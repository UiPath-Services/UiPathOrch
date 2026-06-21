using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Get-Orch{Asset,Bucket,Queue}Link trio. Runs two sequential
// parallel phases (each via OrchThreadPool.RunForEach, cap=4): Phase 1 lists the entities
// per folder; Phase 2 resolves each entity's accessible folders (GetFoldersFor*). Running
// the phases in sequence — rather than a streaming chain — lets each progress bar show a
// real percentage against a known denominator (folder count, then entity count).
//
// Output ordering: drivesFolders sorted by PSPath, entities OrderBy(name); each pool drains
// in source order, so no after-the-fact sort is needed.
//
// Cancellation: ConsoleCancelHandler.Token forwards to OrchTask.GetResult, which throws
// OperationCanceledException as soon as the consumer notices. In-flight workers may still
// complete their current synchronous API call (no token plumbed through OrchAPISession),
// but per-key cache atomicity in OrchDriveInfo (the link cache is set only after a full
// successful response) keeps the cache clean either way.
//
// Derived classes must:
//   - Decorate the class with [Cmdlet(VerbsCommon.Get, "OrchXxxLink")] and
//     [OutputType(typeof(XxxLink))]
//   - Override Name to attach [ArgumentCompleter(typeof(LinkedXxxNameCompleter))] —
//     the [Parameter]/[SupportsWildcards] attributes must be re-declared on
//     the override (matches the RemoveDriveEntityCmdletBase convention).
//   - Implement the abstract members below.
public abstract class GetOrchLinkCmdletBase<TEntity> : OrchestratorPSCmdlet
    where TEntity : class
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // -ExportCsv on every link cmdlet (Asset/Bucket/Queue Link). Column
    // names match the parameters on the corresponding Add-Orch*Link, so
    // Get | Export-Csv | Import-Csv | Add-Orch*Link round-trips. The
    // three columns Path / Name / Link are common to every link entity
    // by base-class contract — no per-subclass override needed.
    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string[] CsvHeaders = ["Path", "Name", "Link"];

    // Stable identifier emitted on WriteError, e.g. "GetAssetLinkError".
    protected abstract string ErrorId { get; }

    // Default CSV filename per concrete subclass (e.g. ExportedAssetLinks.csv).
    protected abstract string DefaultCsvName { get; }

    protected abstract ICollection<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string? GetEntityName(TEntity? entity);
    protected abstract long GetEntityId(TEntity entity);
    protected abstract AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, TEntity entity);
    protected abstract EntityLink BuildLink(string srcPath, TEntity entity, string linkFolderPath, long srcFolderId, long linkFolderId);

    // SimpleFolder has no GetPSPath() — it's a value type returned by the
    // GetFoldersFor* family. Build the PSPath as drive prefix + FullyQualifiedName,
    // mirroring Folder.GetPSPath()'s output.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth)
            .OrderBy(df => df.folder.GetPSPath())
            .ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();

        // Two sequential phases so each progress bar has a known denominator. Phase 1 lists
        // the link-able entities per folder (parallel, cap=4); Phase 2 resolves each entity's
        // accessible folders.
        using var entityPool = OrchThreadPool.RunForEach(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => GetEntities(df.drive, df.folder)
                .FilterByWildcards(e => GetEntityName(e), wpName)
                .OrderBy(e => GetEntityName(e))
                .Select(e => (df.drive, df.folder, entity: e))
                .ToList());

        var entities = new List<(OrchDriveInfo drive, Folder folder, TEntity entity)>();
        using (var listReporter = new ProgressReporter(this, 1, entityPool.Count, "Listing entities"))
        {
            foreach (var task in entityPool)
            {
                try
                {
                    var found = entityPool.GetResultWithProgress(task, listReporter, cancelHandler.Token);
                    if (found is not null) entities.AddRange(found);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, ErrorId, ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        var emitted = new HashSet<(long srcFolderId, long entityId, long linkFolderId)>();

        using var linkPool = OrchThreadPool.RunForEach(
            entities,
            t => t.folder.GetPSPath(),
            t => t.folder,
            t => GetFoldersForEntity(t.drive, t.folder, t.entity));

        using var linkReporter = new ProgressReporter(this, 1, linkPool.Count, "Getting links");
        foreach (var task in linkPool)
        {
            try
            {
                var accessible = linkPool.GetResultWithProgress(task, linkReporter, cancelHandler.Token);
                if (accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                var (drive, folder, entity) = task.Source;
                long srcId = folder.Id ?? 0;
                string sourcePath = folder.GetPSPath();
                long entityId = GetEntityId(entity);

                foreach (var linkFolder in accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    long linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, entityId, linkId))) continue;

                    string linkFolderPath = LinkFolderPSPath(drive, linkFolder);
                    if (writer is not null)
                    {
                        // These are exactly the three values every BuildLink
                        // packages into the link object (Path = srcPath,
                        // Name = entity.Name == GetEntityName(entity),
                        // Link = linkFolderPath). Emit them directly: no link
                        // allocation, no reflection, and the link DTOs stay
                        // plain POCOs.
                        string[] line = [
                            EscapeCsvValue(sourcePath, true),
                            EscapeCsvValue(GetEntityName(entity), true),
                            EscapeCsvValue(linkFolderPath, true),
                        ];
                        writer.WriteCsvLine(line);
                    }
                    else
                    {
                        WriteObject(BuildLink(sourcePath, entity, linkFolderPath, srcId, linkId));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, ErrorId, ErrorCategory.InvalidOperation, ex.Target));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A non-Orch failure on the main thread here (e.g. IOException
                // from WriteCsvLine, or a BuildLink fault) should surface as a
                // per-item error, not terminate the whole cmdlet mid-enumeration.
                string target = task.Source.Item2.GetPSPath(); // Item2 = folder (see deconstruction above)
                WriteError(new ErrorRecord(new OrchException(target, ex), ErrorId, ErrorCategory.InvalidOperation, target));
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
