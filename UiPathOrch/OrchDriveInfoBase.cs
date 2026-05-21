using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Entities;
using Path = System.IO.Path;

namespace UiPath.PowerShell.Core;

// Base class for the three PSDriveInfo families in UiPathOrch:
//   OrchDriveInfo    -- primary Orchestrator drive (owns the OrchAPISession)
//   OrchDuDriveInfo  -- Document Understanding shadow drive (delegates to parent)
//   OrchTmDriveInfo  -- Test Manager shadow drive (delegates to parent)
//
// Hoists the structural duplication that each class was tracking independently:
// cache registry pair, name-colon helpers, root folder slot, and the generic
// ClearAllCache iteration. OrchDriveInfo overrides ClearAllCache to add the
// Orchestrator-specific extra cleanup (tenant identity reset etc.).
//
// Adding this base also unlocks cache-class consolidation: ctors that took a
// family-specific drive type can now take OrchDriveInfoBase, removing the
// Du* / Tm* duplicates of the universal cache classes (see Phase 2+).
public abstract class OrchDriveInfoBase : PSDriveInfo
{
    protected OrchDriveInfoBase(
        string name,
        ProviderInfo provider,
        string root,
        string description,
        PSCredential? credential,
        string currentLocation)
        : base(name, provider, root, description, credential, currentLocation)
    {
    }

    // Cache instances live on the drive so they can be cleared uniformly by
    // iterating these registries from ClearAllCache. Each cache class registers
    // itself in its constructor via _drive._allTenantCache.Add(this) (or
    // _allFolderCache.Add(this)), which is the structural fix for the pre-1.4.3
    // bug where DuExtractors was missed from a hand-maintained clear loop.
    internal readonly List<ITenantCacheClearable> _allTenantCache = [];
    internal readonly List<IFolderCacheClearable> _allFolderCache = [];

    // Drive-scoped OrchAPISession accessor. OrchDriveInfo owns the actual
    // session; OrchDuDriveInfo / OrchTmDriveInfo delegate to ParentDrive.
    internal abstract OrchAPISession OrchAPISession { get; }

    // Automation Cloud organization identifier (partitionGlobalId, prt_id from
    // JWT). Used by PerOrganization cache classes to key shared org-scoped
    // singletons. OrchDriveInfo lazily initializes from JWT or API fallback;
    // OrchDuDriveInfo / OrchTmDriveInfo delegate to ParentDrive so they share
    // the same org identifier as the underlying Orch drive.
    //
    // Callers that must not trigger auth (e.g., Clear-OrchCache scanning every
    // registered drive) should first gate on `IsAuthenticated` -- reading
    // PartitionGlobalId on an unauthenticated drive would trigger the same
    // PKCE / API fallback path used by data-fetch cmdlets.
    internal abstract string? PartitionGlobalId { get; }

    // Auth-state probe that never triggers a token request. True iff the
    // AuthManager already holds an access token from a prior cmdlet (or from
    // a static AccessToken specified at drive creation that's been promoted
    // into the AuthManager on first use). Safe for cmdlets like
    // Clear-OrchCache that must enumerate registered drives without
    // provoking PKCE on the ones the user hasn't authenticated yet.
    internal bool IsAuthenticated => OrchAPISession.AuthManager.IsAuthenticated;

    private string? _NameColon;
    internal string NameColon
    {
        get
        {
            _NameColon ??= Name + ':';
            return _NameColon;
        }
    }

    private string? _NameColonSeparator;
    internal string NameColonSeparator
    {
        get
        {
            _NameColonSeparator ??= Name + ':' + Path.DirectorySeparatorChar;
            return _NameColonSeparator;
        }
    }

    protected internal Folder? RootFolder;

    // Registry-driven clear for tenant-scoped cache instances (per-tenant +
    // per-organization). Backs `Clear-OrchCache -Path orch1:\` semantics -- the
    // root folder presentation surface is tenant entities, so "clear what's
    // visible at root" is exactly the tenant cache set.
    //
    // OrchDriveInfo overrides to add the Orchestrator-specific tenant-level
    // extras (tenant identity reset, folder dictionaries, PmApiDeprecated flag,
    // SearchPmDirectoryCache) by calling base.ClearTenantCache() first.
    public virtual void ClearTenantCache()
    {
        foreach (var cache in _allTenantCache)
        {
            cache.ClearCache();
        }
    }

    // Registry-driven clear for all folder-scoped cache instances (every
    // cached folder on this drive). Backs `Clear-OrchCache -Path orch1:` and
    // the no-args drive-level clear.
    public virtual void ClearAllFolderCache()
    {
        foreach (var cache in _allFolderCache)
        {
            cache.ClearCache();
        }
    }

    // Drive-level full clear: tenant + all folders. Backs `Clear-OrchCache
    // -Path orch1:` and the no-args drive-level clear. The split into
    // ClearTenantCache + ClearAllFolderCache lets the new Clear-OrchCache
    // cmdlet dispatch by scope without re-implementing the iteration logic.
    public void ClearAllCache()
    {
        ClearTenantCache();
        ClearAllFolderCache();
    }
}
