using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public class OrchDuDriveInfo : OrchDriveInfoBase
{
    // _allTenantCache / _allFolderCache live on OrchDriveInfoBase; cache
    // instances declared below register themselves via the inherited members.

    // Org-scoped: shared across all drives in the same org (partitionGlobalId
    // keyed in the cache). Entities are stored bare; cmdlets ShallowClone()
    // per emit and stamp drive-local Path / Project — same pattern as PM*
    // (post-1.4.2 per-org cache path isolation).
    public ListCachePerOrganization<DuRole> DuRoles = null!;
    public KeyedListCachePerOrganization<(string TenantKey, string ProjectId), DuUser> DuUsers = null!;

    // Per-tenant: scoped to this single drive. For uniformity with the
    // org-scoped DU caches above, cmdlets ShallowClone() per emit and stamp
    // here too — same call-site code for every Du entity.
    public ListCachePerTenant<DuProject> DuProjects = null!;
    public DuListCachePerProject<DuDocumentType> DuDocumentTypes = null!;
    public DuListCachePerProject<DuClassifier> DuClassifiers = null!;
    public DuListCachePerProject<DuExtractor> DuExtractors = null!;

    private OrchDriveInfo? _parentDrive;
    internal OrchDriveInfo ParentDrive
    {
        get => _parentDrive!;
        set
        {
            _parentDrive = value ?? throw new ArgumentNullException(nameof(value));

            // Caches need ParentDrive (for OrchAPISession and partitionGlobalId).
            // Initialize after the field is set; pass `this` so the cache classes
            // register into _allTenantCache for uniform ClearAllCache.

            // Org-scoped: no initializer — Path is set by the cmdlet on the
            // per-emit ShallowClone() copy (per-org caches are shared across
            // drives, so drive-local Path on the cached singleton would be
            // raced/wrong, matching the PM* 1.4.2 fix rationale).
            DuRoles = new(this, partitionGlobalId =>
                OrchAPISession.GetDuRoles(partitionGlobalId) ?? []);

            DuUsers = new(this, (partitionGlobalId, key) =>
                OrchAPISession.GetDuUsers(partitionGlobalId, key.TenantKey, key.ProjectId) ?? []);

            // Per-tenant: Path/FullName stamped in the initializer, because
            // DuProject is consumed AS INPUT by other cmdlets (e.g.,
            // GetDuDocumentType receives the project via EnumDuFolders and
            // calls project.GetPSPath() on it). If we left Path unstamped
            // here and relied on clone+stamp at the OrchDuProvider emit
            // site only, the project handed to downstream cmdlets would
            // have Path = null and PathProject would degrade to just the
            // project name. Per-tenant scope means there's no cross-drive
            // sharing, so init-time stamping is safe (unlike DuRoles /
            // DuUsers which are per-org and must clone+stamp on emit).
            DuProjects = new(this,
                () => (OrchAPISession.GetDuProjects() ?? []).OrderBy(p => p.name),
                p => { p.Path = NameColonSeparator; p.FullName = NameColonSeparator + p.name; });

            DuDocumentTypes = new(this, project =>
                OrchAPISession.GetDuDocumentTypes(project.id!) ?? []);

            DuClassifiers = new(this, project =>
                OrchAPISession.GetDuClassifiers(project.id!) ?? []);

            DuExtractors = new(this, project =>
                OrchAPISession.GetDuExtractors(project.id!) ?? []);
        }
    }

    internal override OrchAPISession OrchAPISession => ParentDrive.OrchAPISession;
    internal override string? PartitionGlobalId => ParentDrive.PartitionGlobalId;

    // NameColon / NameColonSeparator / RootFolder / ClearAllCache live on
    // OrchDriveInfoBase. The base implementation of ClearAllCache iterates
    // _allTenantCache and _allFolderCache, which is all DU needs.

    // At the time this constructor runs, NameColonSeparator is not yet available
    public OrchDuDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
    }

    // Wrappers preserve the legacy array return type so existing call sites
    // (completers, internal Add-DuUser / Remove-DuRoleFromDuUser logic) keep
    // working without changes. The returned entities are RAW (no drive-local
    // Path / Project stamped) — emitting cmdlets must ShallowClone() and stamp
    // per-emit. Internal callers that only read name/id don't need to clone.

    public DuRole[] GetDuRoles()
        => DuRoles.Get().ToArray();

    public DuProject[] GetDuProjects()
        => DuProjects.Get().ToArray();

    public DuUser[] GetDuUsers(DuProject? project)
    {
        var tenantKey = ParentDrive.GetTenantId().key;
        if (project?.id is null || tenantKey is null) return [];
        return DuUsers.Get((tenantKey, project.id)).ToArray();
    }

    public DuDocumentType[] GetDuDocumentTypes(DuProject project)
        => DuDocumentTypes.Get(project).ToArray();

    public DuClassifier[] GetDuClassifiers(DuProject project)
        => DuClassifiers.Get(project).ToArray();

    public DuExtractor[] GetDuExtractors(DuProject project)
        => DuExtractors.Get(project).ToArray();
}
