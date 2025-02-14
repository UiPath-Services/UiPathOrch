using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public class OrchTmDriveInfo : PSDriveInfo
{
    internal OrchDriveInfo? _parentDrive;
    internal OrchDriveInfo ParentDrive => _parentDrive!;

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

    // OrchTmProvider の Start で初期化する
    internal static SessionState? SessionState;

    public static IEnumerable<OrchTmDriveInfo> EnumAllOrchDrives()
    {
        return SessionState!.Drive.GetAllForProvider("UiPathOrchTm")
            .Cast<OrchTmDriveInfo>()
            .OrderBy(d => d.Name);
    }

    protected internal Folder? RootFolder;

    // このコンストラクタを実行するタイミングでは、NameColonSeparator は利用できない
    public OrchTmDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
//            _parentDrive = parent;
    }

    // paths を指定しない場合、カレントドライブのみを返す
    public static List<OrchTmDriveInfo> EnumOrchTmDrives(IEnumerable<string?>? paths = null)
    {
        var drives = new List<OrchTmDriveInfo>();
        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            if (SessionState!.Path.CurrentLocation.Drive is OrchTmDriveInfo orchDrive)
                drives.Add(orchDrive);
        }
        else
        {
            var psPaths = paths.Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var p in psPaths)
            {
                if (p.Drive is OrchTmDriveInfo orchDrive)
                    drives.Add(orchDrive);
            }
        }
        //return drives.DistinctBy(drive => drive.Name).ToList();
        return drives.Distinct().ToList();
    }

    public static IEnumerable<PathInfo> ResolveOrchDrivePaths(IEnumerable<string?>? paths = null)
    {
        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = SessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchTmDriveInfo)
            {
                yield return SessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrchTm"))
            {
                yield return pathInfo;
            }
        }
    }

    public static List<(OrchTmDriveInfo drive, TmProject project)> EnumFolders(IEnumerable<string?>? path, bool recurse = false) ///, bool includeRoot = false)
    {
        var paths = ResolveOrchDrivePaths(path);

        List<(OrchTmDriveInfo drive, TmProject project)> ret = [];

        HashSet<string> visited = [];
        foreach (var p in paths)
        {
            OrchTmDriveInfo drive = p.Drive as OrchTmDriveInfo;
            if (drive is null) continue;

            var dicProjects = drive!.GetTmProjects();
            if (dicProjects is null) continue;

            //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            //if (folder is null) continue;

            // Recurse が指定されていて、かつルートフォルダであれば、すべてのプロジェクトを返せばOK
            if (recurse && p.Path.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                foreach (var project in dicProjects)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
                continue;
            }

            // dicFolders にはルートフォルダーが含まれないため、ルートだけ先にここで探して追加する
            //if (includeRoot)
            //{
            //    ret.Add((drive!, null));
            //}

            // p からプロジェクト名を取り出す
            string projectPrefix = Path.GetFileName(p.Path);

            foreach (var project in dicProjects)
            {
                if (string.Compare(project.projectPrefix, projectPrefix, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters on UiPathOrchTm drive.");
        }
        return ret;
    }

    public void ClearAllCache()
    {
        _dicTmProjects = null;
        _dicTmProjectsException.ClearCache();

        _dicTmTestCases = null;
        _dicTmTestCasesExceptions.ClearCache();

        _dicTmServerInfo = null;
        _dicTmServerInfoException.ClearCache();

        _dicTmProjectSettings = null;
        _dicTmProjectSettingsException.ClearCache();

        _dicTmTestSets = null;
        _dicTmTestSetsExceptions.ClearCache();

        _dicTmConfig = null;
        _dicTmConfigException.ClearCache();

        _dicTmProjectPermission = null;
        _dicTmProjectPermissionException.ClearCache();
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

    internal TmServerInfo? _dicTmServerInfo = null;
    internal readonly ExceptionCachePerTenant _dicTmServerInfoException = new();
    public TmServerInfo? GetTmServerInfo()
    {
        _dicTmServerInfoException.ThrowCachedExceptionIfAny();

        if (_dicTmServerInfo is null)
        {
            lock (this)
            {
                try
                {
                    _dicTmServerInfo = OrchAPISession.GetTmServerInfo();
                    if (_dicTmServerInfo is not null)
                    {
                        _dicTmServerInfo.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicTmServerInfoException.CacheException(ex);
                    throw;
                }
            }
        }
        return _dicTmServerInfo;
    }

    internal TmConfig? _dicTmConfig = null;
    internal readonly ExceptionCachePerTenant _dicTmConfigException = new();
    public TmConfig? GetTmConfiguration()
    {
        _dicTmConfigException.ThrowCachedExceptionIfAny();

        if (_dicTmConfig is null)
        {
            lock (this)
            {
                try
                {
                    _dicTmConfig = OrchAPISession.GetTmConfiguration();
                    if (_dicTmConfig is not null)
                    {
                        _dicTmConfig.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicTmConfigException.CacheException(ex);
                    throw;
                }
            }
        }
        return _dicTmConfig;
    }

    // key: projectId
    internal ConcurrentDictionary<string, TmProjectSettings>? _dicTmProjectSettings = null;
    internal readonly ExceptionCachePerTenant _dicTmProjectSettingsException = new();
    public TmProjectSettings? GetTmProjectSettings(TmProject project)
    {
        _dicTmProjectSettingsException.ThrowCachedExceptionIfAny();

        if (_dicTmProjectSettings is null)
        {
            lock (this)
            {
                _dicTmProjectSettings ??= [];
            }
        }

        if (!_dicTmProjectSettings.TryGetValue(project.id!, out var tmProjectSettings))
        {

            try
            {
                tmProjectSettings = OrchAPISession.GetTmProjectSettings(project.id!);
                if (tmProjectSettings is not null)
                {
                    tmProjectSettings.Path = NameColonSeparator + tmProjectSettings.projectPrefix;
                    _dicTmProjectSettings[project.id ?? ""] = tmProjectSettings;
                }
            }
            catch (HttpResponseException ex)
            {
                _dicTmProjectSettingsException.CacheException(ex);
                throw;
            }
        }
        return tmProjectSettings;
    }

    // key: projectId
    internal ConcurrentDictionary<string, List<TmProjectPermission>>? _dicTmProjectPermission = null;
    internal readonly ExceptionCachePerTenant _dicTmProjectPermissionException = new();
    public ReadOnlyCollection<TmProjectPermission>? GetTmProjectPermission(TmProject project)
    {
        _dicTmProjectPermissionException.ThrowCachedExceptionIfAny();

        if (_dicTmProjectPermission is null)
        {
            lock (this)
            {
                _dicTmProjectPermission ??= [];
            }
        }

        if (!_dicTmProjectPermission.TryGetValue(project.id!, out var tmProjectPermissions))
        {
            try
            {
                tmProjectPermissions = OrchAPISession.GetTmProjectPermission(project.id!).ToList();
                string pathProject = project.GetPSPath();
                foreach (var permission in tmProjectPermissions)
                {
                    permission.Path = NameColonSeparator;
                    permission.Project = project.name;
                    permission.PathProject = pathProject;
                }
                _dicTmProjectPermission[project.id ?? ""] = tmProjectPermissions;
            }
            catch (HttpResponseException ex)
            {
                _dicTmProjectPermissionException.CacheException(ex);
                throw;
            }
        }
        return tmProjectPermissions.AsReadOnly();
    }

    // key: projectId
    internal ConcurrentDictionary<string, List<TmRequirement>>? _dicTmRequirements = null;
    internal readonly ExceptionsCachePer<string> _dicTmRequirementExceptions = new();
    public ReadOnlyCollection<TmRequirement> GetTmRequirements(TmProject project)
    {
        _dicTmRequirementExceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicTmRequirements is null)
        {
            lock (this)
            {
                _dicTmRequirements ??= [];
            }
        }

        if (!_dicTmRequirements.TryGetValue(project.id!, out var tmRequirements))
        {

            try
            {
                tmRequirements = OrchAPISession.GetTmRequirements(project.id!).ToList();
                string path = NameColonSeparator + project.projectPrefix;
                foreach (var requirement in tmRequirements)
                {
                    requirement.Path = path;
                }
                _dicTmRequirements[project.id ?? ""] = tmRequirements;
            }
            catch (HttpResponseException ex)
            {
                _dicTmRequirementExceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return tmRequirements.AsReadOnly();
    }

    // key: projectId
    internal ConcurrentDictionary<string, List<TmTestCase>>? _dicTmTestCases = null;
    internal readonly ExceptionsCachePer<string> _dicTmTestCasesExceptions = new();
    public ReadOnlyCollection<TmTestCase> GetTmTestCases(TmProject project)
    {
        _dicTmTestCasesExceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicTmTestCases is null)
        {
            lock (this)
            {
                _dicTmTestCases ??= [];
            }
        }

        if (!_dicTmTestCases.TryGetValue(project.id!, out var tmTestCases))
        {

            try
            {
                tmTestCases = OrchAPISession.GetTmTestCases(project.id!).ToList();
                string path = NameColonSeparator + project.projectPrefix;
                foreach (var testCase in tmTestCases)
                {
                    testCase.Path = path;
                }
                _dicTmTestCases[project.id ?? ""] = tmTestCases;
            }
            catch (HttpResponseException ex)
            {
                _dicTmTestCasesExceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return tmTestCases.AsReadOnly();
    }

    // key: projectId
    internal ConcurrentDictionary<string, List<TmTestSet>>? _dicTmTestSets = null;
    internal readonly ExceptionsCachePer<string> _dicTmTestSetsExceptions = new();
    public ReadOnlyCollection<TmTestSet> GetTmTestSets(TmProject project)
    {
        _dicTmTestSetsExceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicTmTestSets is null)
        {
            lock (this)
            {
                _dicTmTestSets ??= [];
            }
        }

        if (!_dicTmTestSets.TryGetValue(project.id!, out var tmTestSets))
        {

            try
            {
                tmTestSets = OrchAPISession.GetTmTestSets(project.id!).ToList();
                string path = NameColonSeparator + project.projectPrefix;
                foreach (var testSet in tmTestSets)
                {
                    testSet.Path = path;
                }
                _dicTmTestSets[project.id ?? ""] = tmTestSets;
            }
            catch (HttpResponseException ex)
            {
                _dicTmTestSetsExceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return tmTestSets.AsReadOnly();
    }
}
