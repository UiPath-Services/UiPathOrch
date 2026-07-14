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
    // PASSIVE: returns the cached value (null if the drive has never authed).
    // Safe to read from cleanup paths (Clear-OrchCache, Import-OrchConfig's
    // drive teardown, etc.) without provoking PKCE on drives the user hasn't
    // touched yet. Callers that need to force the lookup (data-fetch paths)
    // must call <see cref="GetPartitionGlobalId"/> instead.
    internal abstract string? PartitionGlobalId { get; }

    // ACTIVE: returns the partition id, lazily fetching it from the JWT
    // (cheap) or the Users API (triggers auth) on the first call. Use from
    // data-fetch paths where the caller is already committed to issuing
    // API calls; never from cache cleanup, where the regression that drove
    // this split lives -- ClearCache on an unauthed drive was firing PKCE
    // because PartitionGlobalId was doing the fetch.
    internal abstract string? GetPartitionGlobalId();

    // Resolves the parent Orchestrator drive of a shadow (DU / TM) drive by name: this drive's
    // name minus its 2-char suffix, so "Orch1Du" -> "Orch1".
    //
    // It is deliberately LAZY, and the shadow drives call it from their ParentDrive getter.
    // Each provider mounts its own default drives from the config file, and the ORDER in which
    // PowerShell initializes the providers is not ours to control: the DU provider can come up
    // before UiPathOrch, in which case the parent does not exist yet at the moment the DU drive is
    // created. Linking eagerly there would leave the drive permanently parentless (and unusable),
    // or -- worse -- make mounting it fail outright, depending on which provider happened to load
    // first. Resolving on first USE instead is correct under either order, because by the time any
    // cmdlet touches the drive every provider has finished initializing.
    //
    // Returns null when no such drive is mounted; the caller decides how to report that.
    private protected OrchDriveInfo? ResolveParentDriveByName(string driveSuffix)
    {
        if (Name.Length <= driveSuffix.Length ||
            !Name.EndsWith(driveSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string parentName = Name[..^driveSuffix.Length];

        // Drive.Get throws (rather than returning null) on an unknown name.
        try { return OrchDriveInfo.SessionState?.Drive.Get(parentName) as OrchDriveInfo; }
        catch { return null; }
    }

    // The exception a shadow drive raises when its parent cannot be resolved on first use. The
    // drive is a view onto the parent -- auth, base URLs, partition and every cache come from it --
    // so there is nothing useful the drive can do without one. Two distinct causes, and the message
    // says which: the name doesn't follow the convention at all, or it does and the parent isn't
    // mounted. (Naming a drive off-convention must NOT produce a made-up parent name in the text.)
    private protected InvalidOperationException NoParentDriveException(string driveSuffix)
    {
        bool named = Name.Length > driveSuffix.Length &&
                     Name.EndsWith(driveSuffix, StringComparison.OrdinalIgnoreCase);

        string detail = named
            ? $"'{Name[..^driveSuffix.Length]}:' is not mounted. Run Import-OrchConfig, or mount it first."
            : $"'{Name}:' does not follow that convention — name it '<orchestrator drive>{driveSuffix}'.";

        return new InvalidOperationException(
            $"'{Name}:' has no parent UiPathOrch drive. A {driveSuffix} drive is a view onto an Orchestrator " +
            $"drive and is named after it with the '{driveSuffix}' suffix, so {detail}");
    }

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
