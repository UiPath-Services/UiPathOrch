using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base class for Remove-* cmdlets that delete folder-scoped entities by Name wildcard.
// Derived classes:
//   - Add [Cmdlet(VerbsCommon.Remove, "OrchXxx", SupportsShouldProcess = true)]
//   - Override Name to attach [ArgumentCompleter(typeof(XxxNameCompleter))]
//   - Implement EntityNoun, GetEntities, Remove, GetName, GetPSPath
//   - Optionally override PreFilter, ExcludePersonalWorkspace, ErrorCategory
public abstract class RemoveFolderEntityCmdletBase<TEntity> : RemoveEntityCmdletBase<TEntity>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
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

    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract void Remove(OrchDriveInfo drive, Folder folder, TEntity entity);

    protected virtual bool ExcludePersonalWorkspace => false;

    // The folders — other than the one being removed from — that this entity is also linked into,
    // or null when the entity type has no folder sharing. Default: null, so no lookup and no extra
    // API call for the entity types that can't be shared.
    //
    // Why the removal needs to know: deleting an entity that is linked into several folders only
    // removes it FROM THAT FOLDER. The entity — and its value — survive in the remaining ones, with
    // the same id, and the local view then shows nothing, so "Remove-OrchAsset X" reads as a delete
    // while X is still live elsewhere. Measured on Cloud for assets, queues and buckets, and in both
    // directions (removing from the folder the entity was created in, and from a folder it was
    // linked into). Overriders should return null rather than an empty list when nothing is shared.
    protected virtual List<SimpleFolder>? GetOtherLinkFolders(OrchDriveInfo drive, Folder folder, TEntity entity) => null;

    // Shared shape of the three link lookups: everything the GetFoldersFor* call reports, minus the
    // folder the entity is being removed from.
    protected static List<SimpleFolder>? OtherFolders(AccessibleFoldersDto? accessible, Folder folder)
    {
        var others = accessible?.AccessibleFolders?.Where(f => f.Id != folder.Id).ToList();
        return others is { Count: > 0 } ? others : null;
    }

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = ExcludePersonalWorkspace
            ? SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth)
            : SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var preFilter = PreFilter;

        var stillShared = new StillSharedNotice(this, EntityNoun);
        var getPSPath = GetPSPath;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                IEnumerable<TEntity> entities = GetEntities(drive, folder);
                if (preFilter is not null) entities = preFilter(entities);

                RemoveMatching(entities, wpName, folder.GetPSPath(),
                    entity =>
                    {
                        // Looked up BEFORE the delete: afterwards this folder no longer holds the
                        // entity, so the sharing can't be read from here any more.
                        var others = OtherLinkFoldersSafely(drive, folder, entity);
                        Remove(drive, folder, entity);
                        stillShared.Note(drive, getPSPath(entity), others, whatIf: false);
                    },
                    cancelHandler.Token,
                    entity => stillShared.Note(drive, getPSPath(entity), OtherLinkFoldersSafely(drive, folder, entity), whatIf: true));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), $"Get{EntityNoun}Error", ErrorCategory, folder));
            }
        }

        stillShared.WriteSummary();
    }

    // A warning must never be the reason a delete fails, and the GetFoldersFor* family 404s below
    // API v12 (same gate as the Copy link path — an unknown version is not treated as too old).
    private List<SimpleFolder>? OtherLinkFoldersSafely(OrchDriveInfo drive, Folder folder, TEntity entity)
    {
        if (drive.OrchAPISession.ApiVersion < 12) return null;
        try
        {
            return GetOtherLinkFolders(drive, folder, entity);
        }
        catch
        {
            return null;
        }
    }

    // Reports the entities whose removal only unshared them, throttled like the copy warnings:
    // the first few in full, then one summary line, so removing a folder full of shared entities
    // doesn't flood the warning stream.
    private sealed class StillSharedNotice
    {
        private const int Threshold = 5;
        private const int MaxNamedFolders = 3;

        private readonly RemoveFolderEntityCmdletBase<TEntity> _host;
        private readonly string _noun;
        private int _count;

        public StillSharedNotice(RemoveFolderEntityCmdletBase<TEntity> host, string noun)
        {
            _host = host;
            _noun = noun.ToLowerInvariant();
        }

        public void Note(OrchDriveInfo drive, string entityPSPath, List<SimpleFolder>? others, bool whatIf)
        {
            if (others is not { Count: > 0 }) return;

            _count++;
            if (_count > Threshold) return;

            string where = Describe(drive, others);
            string plural = others.Count == 1 ? "that folder" : "those folders";
            _host.WriteWarning(whatIf
                ? $"'{entityPSPath}' would be removed from this folder only: the {_noun} is also linked into {where}, where it would still exist."
                : $"'{entityPSPath}' was removed from this folder only: the {_noun} is also linked into {where}, where it still exists. Remove it from {plural} too to delete the {_noun} itself.");
        }

        public void WriteSummary()
        {
            if (_count <= Threshold) return;
            _host.WriteWarning($"{_count} {_noun}s were removed from their folder only because they are linked into other folders, where they still exist; {_count - Threshold} further warning(s) were suppressed. Get-Orch{_host.EntityNoun}Link shows the remaining sharing.");
        }

        // SimpleFolder has no GetPSPath() — mirror GetOrchLinkCmdletBase's construction.
        private static string Describe(OrchDriveInfo drive, List<SimpleFolder> folders)
        {
            var paths = folders
                .Select(f => drive.NameColonSeparator + (f.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return paths.Count <= MaxNamedFolders
                ? string.Join(", ", paths)
                : $"{string.Join(", ", paths.Take(MaxNamedFolders))} (+{paths.Count - MaxNamedFolders} more)";
        }
    }
}
