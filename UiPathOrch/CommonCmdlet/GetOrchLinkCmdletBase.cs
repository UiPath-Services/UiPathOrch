using System.Management.Automation;
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
public abstract class GetOrchLinkCmdletBase<TEntity, TLink> : OrchestratorPSCmdlet
    where TEntity : class
    where TLink : class
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Stable identifier emitted on WriteError, e.g. "GetAssetLinkError".
    protected abstract string ErrorId { get; }

    protected abstract ICollection<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string? GetEntityName(TEntity? entity);
    protected abstract long GetEntityId(TEntity entity);
    protected abstract AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, TEntity entity);
    protected abstract TLink BuildLink(string srcPath, TEntity entity, string linkFolderPath, long srcFolderId, long linkFolderId);

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
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth)
            .OrderBy(df => df.folder.GetPSPath())
            .ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        using var pool = OrchThreadPool.RunForEachChained(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => (object)df.folder,
            df => GetEntities(df.drive, df.folder)
                .FilterByWildcards(e => GetEntityName(e), wpName)
                .OrderBy(e => GetEntityName(e))
                .Select(e => (df.drive, df.folder, entity: e)),
            t => t.folder.GetPSPath(),
            t => (object)t.folder,
            t => GetFoldersForEntity(t.drive, t.folder, t.entity));

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

                    WriteObject(BuildLink(sourcePath, entity, LinkFolderPSPath(drive, linkFolder), srcId, linkId));
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, ErrorId, ErrorCategory.InvalidOperation, ex.Target));
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
    }
}
