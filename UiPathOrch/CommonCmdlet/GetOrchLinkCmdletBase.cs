using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Get-Orch{Asset,Bucket,Queue}Link trio. Uses
// OrchThreadPool.RunForEachChained: Phase 1 fetches the per-folder entity
// list, fanout produces (drive, folder, entity) tuples, Phase 2 calls
// GetFoldersFor* per entity. Phase 1 + Phase 2 share a single semaphore
// (cap=4) so the total in-flight API call budget stays at 4 across the
// chain, while consumer drain streams in (folder, entity, link) order.
//
// Output ordering: drivesFolders sorted by PSPath, entities OrderBy(name)
// inside each Phase 1 result, links OrderBy(FullyQualifiedName) inside
// each Phase 2 result. ChainedThreadPool yields tasks in flat insertion
// order (= source order × per-source enumeration order), so no
// after-the-fact sort is needed.
//
// Cancellation: ConsoleCancelHandler.Token forwards to OrchTask.GetResult,
// which throws OperationCanceledException as soon as the consumer notices.
// In-flight workers may still complete their current synchronous API call
// (no token plumbed through OrchAPISession), but per-key cache atomicity in
// OrchDriveInfo (the link cache is set only after a full successful response)
// keeps the cache clean either way.
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

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();

        using var pool = OrchThreadPool.RunForEachChained(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => (object)df.folder,
            df => GetEntities(df.drive, df.folder)
                .FilterByNames(e => GetEntityName(e), Name)
                .OrderBy(e => GetEntityName(e))
                .Select(e => (df.drive, df.folder, entity: e)),
            t => t.folder.GetPSPath(),
            t => (object)t.folder,
            t => GetFoldersForEntity(t.drive, t.folder, t.entity),
            cancelHandler.Token);

        var emitted = new HashSet<(long srcFolderId, long entityId, long linkFolderId)>();

        foreach (var task in pool)
        {
            try
            {
                var accessible = task.GetResult(cancelHandler.Token);
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

        // Phase 1 errors (per-folder GetEntities failures) — drained after
        // Phase 2 completes. Same ErrorId as Phase 2 to keep the link
        // cmdlets simple; switch to a separate ErrorId here if callers need
        // to distinguish.
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            WriteError(new ErrorRecord(ex, ErrorId, ErrorCategory.InvalidOperation, ex.Target));
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
