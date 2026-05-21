using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public class OrchTmDriveInfo : OrchDriveInfoBase
{
    // Test tenant entities
    // The number indicates the parameter count of the getter method (in OrchAPISession.cs)
    public SingleCachePerTenant<TmServerInfo> TmServerInformation = null!;
    public SingleCachePerTenant<TmConfig> TmConfiguration = null!;
    public TmSingleCachePerTenant1<TmProjectSettings> TmProjectSetting = null!;

    // Test list entities
    public ListCachePerTenant<TmProject> TmProjects = null!;
    public TmListCachePerTenant1<TmTestCase> TmTestCases = null!;
    public TmListCachePerTenant1<TmTestSet> TmTestSets = null!;
    public TmListCachePerTenant1<TmTestExecution> TmTestExecutions = null!;
    public TmListCachePerTenant1<TmRequirement> TmRequirements = null!;
    public TmListCachePerTenant1<TmProjectPermission> TmProjectPermissions = null!;
    // Incremental cache
    public IncrementalCachePerProject<string, TmTestExecutionResult> TmTestExecutionResults = null!;


    // _allTenantCache / _allFolderCache live on OrchDriveInfoBase; cache
    // instances declared above register themselves via the inherited members.

    private OrchDriveInfo? _parentDrive;
    internal OrchDriveInfo ParentDrive
    {
        get => _parentDrive!;
        set
        {
            _parentDrive = value ?? throw new ArgumentNullException(nameof(value));

            #region initialize test entity caches

            // Caches need to be initialized after ParentDrive is set.
            // TmProjects is per-tenant — Path/FullName stamping in the
            // initializer is safe (no cross-drive sharing).
            TmProjects = new(this, () => OrchAPISession.GetTmProjects() ?? [], e =>
            {
                e.Path = NameColonSeparator;
                e.FullName = NameColonSeparator + e.projectPrefix;
            });

            TmServerInformation = new(this, OrchAPISession.GetTmServerInfo, e => e.Path = NameColonSeparator);
            TmConfiguration = new(this, OrchAPISession.GetTmConfiguration, e => e.Path = NameColonSeparator);
            TmProjectSetting = new(this, project => OrchAPISession.GetTmProjectSettings(project.id!),
                (e, project) =>
                {
                    e.Path = NameColonSeparator + e.projectPrefix;
                });

            TmTestCases = new(this, project => OrchAPISession.GetTmTestCases(project.id!), (e, project) => e.Path = project.GetPSPath());
            TmTestSets = new(this, project => OrchAPISession.GetTmTestSets(project.id!), (e, project) => e.Path = project.GetPSPath());
            TmTestExecutions = new(this, project => OrchAPISession.GetTmTestExecutions(project.id!), (e, project) => e.Path = project.GetPSPath());
            TmRequirements = new(this, project => OrchAPISession.GetTmRequirements(project.id!), (e, project) => e.Path = project.GetPSPath());

            TmProjectPermissions = new(this, project => OrchAPISession.GetTmProjectPermission(project.id!),
                (e, project) =>
                {
                    e.Path = project.GetPSPath();
                    e.Project = project.name;
                    e.PathProject = e.Path + '\\' + e.Project;
                });

            // Incremental cache
            TmTestExecutionResults = new(this,
                OrchAPISession.GetTmTestExecutionsResult,
                e => e.id,
                (e, project) => e.Path = project.GetPSPath());

            #endregion
        }
    }

    internal override OrchAPISession OrchAPISession => ParentDrive.OrchAPISession;
    internal override string? PartitionGlobalId => ParentDrive.PartitionGlobalId;

    // NameColon / NameColonSeparator / RootFolder / ClearAllCache live on
    // OrchDriveInfoBase. The base implementation of ClearAllCache iterates
    // _allTenantCache and _allFolderCache, which is all Tm needs.

    // At the time this constructor runs, NameColonSeparator is not yet available
    public OrchTmDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
    }

    // Backward-compat thin wrapper for callers that still spell the legacy
    // method. The universal ListCachePerTenant returns the internal List<T>
    // directly; wrap as ReadOnlyCollection here to keep callers from mutating
    // the cached list.
    public ReadOnlyCollection<TmProject>? GetTmProjects() => TmProjects.Get().AsReadOnly();
}
