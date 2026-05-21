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
// family-specific drive type can now take OrchPSDriveInfoBase, removing the
// Du* / Tm* duplicates of the universal cache classes (see Phase 2+).
public abstract class OrchPSDriveInfoBase : PSDriveInfo
{
    protected OrchPSDriveInfoBase(
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
    internal abstract string? PartitionGlobalId { get; }

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

    // Default registry-driven clear. OrchDriveInfo overrides to add the
    // Orchestrator-specific cleanup (tenant identity reset, folder dictionaries,
    // PmApiDeprecated flag) by calling base.ClearAllCache() first.
    public virtual void ClearAllCache()
    {
        foreach (var cache in _allTenantCache)
        {
            cache.ClearCache();
        }
        foreach (var cache in _allFolderCache)
        {
            cache.ClearCache();
        }
    }
}
