using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public class OrchTmDriveInfo : PSDriveInfo
{
    // テストのテナントエンティティ
    // 数字は getter (OrchAPISession.cs にあるメソッド) の引数の数を示す
    public TestSingleCachePerTenant0<TmServerInfo>      TmServerInformation = null!;
    public TestSingleCachePerTenant0<TmConfig>          TmConfiguration     = null!;
    public TestSingleCachePerTenant1<TmProjectSettings> TmProjectSetting    = null!;

    // テストのリストエンティティ
    //public TestListCachePerTenant0<TmProject>           TmProjects           = null!;
    public TestListCachePerTenant1<TmTestCase>          TmTestCases          = null!;
    public TestListCachePerTenant1<TmTestSet>           TmTestSets           = null!;
    public TestListCachePerTenant1<TmTestExecution>     TmTestExecutions     = null!;
    public TestListCachePerTenant1<TmRequirement>       TmRequirements       = null!;
    public TestListCachePerTenant1<TmProjectPermission> TmProjectPermissions = null!;

    // これらはドライブごとに保持する必要があるため、 Cache クラスの static メンバにはできない
    internal readonly List<ITenantCacheClearable> _allTenantCache = [];
    internal readonly List<IFolderCacheClearable> _allFolderCache = [];

    private OrchDriveInfo? _parentDrive;
    internal OrchDriveInfo ParentDrive
    {
        get => _parentDrive!;
        set
        {
            _parentDrive = value ?? throw new ArgumentNullException(nameof(value));

            #region initialize test entity caches

            // ParentDrive を設定した後に、キャッシュを初期化する必要がある
            //TmProjects = new(this, OrchAPISession.GetTmProjects, e =>
            //{
            //    e.Path = NameColonSeparator;
            //    e.FullName = NameColonSeparator + e.projectPrefix;
            //});

            TmServerInformation = new(this, OrchAPISession.GetTmServerInfo,    e => e.Path = NameColonSeparator);
            TmConfiguration     = new(this, OrchAPISession.GetTmConfiguration, e => e.Path = NameColonSeparator);
            TmProjectSetting    = new(this, project => OrchAPISession.GetTmProjectSettings(project.id!),
                (e, project) =>
                {
                    e.Path = NameColonSeparator + e.projectPrefix;
                });

            TmTestCases = new(this, project => OrchAPISession.GetTmTestCases(project.id!), (e, project) => e.Path = project.GetPSPath());
            TmTestSets  = new(this, project => OrchAPISession.GetTmTestSets(project.id!),  (e, project) => e.Path = project.GetPSPath());
            TmTestExecutions = new(this, project => OrchAPISession.GetTmTestExecutions(project.id!), (e, project) => e.Path = project.GetPSPath());
            TmRequirements = new(this, project => OrchAPISession.GetTmRequirements(project.id!), (e, project) => e.Path = project.GetPSPath());

            TmProjectPermissions = new(this, project => OrchAPISession.GetTmProjectPermission(project.id!),
                (e, project) =>
                {
                    e.Path = project.GetPSPath();
                    e.Project = project.name;
                    e.PathProject = e.Path + '\\' + e.Project;
                });

            #endregion
        }
    }

    internal OrchAPISession OrchAPISession => ParentDrive.OrchAPISession;

    private string? _NameColon = null;
    private string? _NameColonSeparator = null;

    internal string NameColon
    {
        get
        {
            _NameColon ??= Name + ':';
            return _NameColon;
        }
    }
    internal string NameColonSeparator
    {
        get
        {
            _NameColonSeparator ??= Name + ':' + Path.DirectorySeparatorChar;
            return _NameColonSeparator;
        }
    }

    protected internal Folder? RootFolder;

    // このコンストラクタを実行するタイミングでは、NameColonSeparator は利用できない
    public OrchTmDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
        //            _parentDrive = parent;
    }

    public void ClearAllCache()
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

    internal List<TmProject>? _dicTmProjects = null;
    internal readonly ExceptionCachePerTenant _dicTmProjectsException = new();
    public ReadOnlyCollection<TmProject>? GetTmProjects()
    {
        _dicTmProjectsException.ThrowCachedExceptionIfAny();

        if (_dicTmProjects is null)
        {
            lock (this)
            {
                if (_dicTmProjects is null)
                {
                    try
                    {
                        _dicTmProjects = OrchAPISession.GetTmProjects()?.ToList();
                        if (_dicTmProjects is null)
                        {
                            _dicTmProjects = [];
                        }
                        else
                        {
                            foreach (var project in _dicTmProjects)
                            {
                                project.Path = NameColonSeparator;
                                project.FullName = NameColonSeparator + project.projectPrefix;
                            }
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicTmProjectsException.CacheException(ex);
                        throw;
                    }
                }
            }
        }
        return _dicTmProjects?.AsReadOnly();
    }
}
