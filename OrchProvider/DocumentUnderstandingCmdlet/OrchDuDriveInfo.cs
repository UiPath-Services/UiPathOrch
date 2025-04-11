using System.Collections.Concurrent;
using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public class OrchDuDriveInfo : PSDriveInfo
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

    // OrchDuProvider の Start で初期化する
    internal static SessionState? SessionState;

    public static IEnumerable<OrchDuDriveInfo> EnumAllOrchDrives()
    {
        return SessionState!.Drive.GetAllForProvider("UiPathOrchDu")
            .Cast<OrchDuDriveInfo>()
            .OrderBy(d => d.Name);
    }

    protected internal Folder? RootFolder;

    // このコンストラクタを実行するタイミングでは、NameColonSeparator は利用できない
    public OrchDuDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
    }

    // paths を指定しない場合、カレントドライブのみを返す
    public static List<OrchDuDriveInfo> EnumOrchDuDrives(IEnumerable<string?>? paths = null)
    {
        var drives = new List<OrchDuDriveInfo>();
        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            if (SessionState!.Path.CurrentLocation.Drive is OrchDuDriveInfo orchDrive)
                drives.Add(orchDrive);
        }
        else
        {
            var psPaths = paths.Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var p in psPaths)
            {
                if (p.Drive is OrchDuDriveInfo orchDrive)
                    drives.Add(orchDrive);
            }
        }
        //return drives.DistinctBy(drive => drive.Name).ToList();
        return drives.Distinct().ToList();
    }

    public static OrchDuDriveInfo GetOrchDuDrive(string? path = null)
    {
        var srcDrives = EnumOrchDuDrives([path]);
        if (srcDrives.Count > 1)
        {
            throw new Exception($"'{path}' resolved to multiple containers.");
        }
        if (srcDrives.Count == 0)
        {
            // たぶん先に EnumOrchDrives() が例外を投げているはずなので、ここは実行されないと思う。
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
    }

    public static IEnumerable<PathInfo> ResolveOrchDrivePaths(IEnumerable<string?>? paths = null)
    {
        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = SessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchDuDriveInfo)
            {
                yield return SessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrchDu"))
            {
                yield return pathInfo;
            }
        }
    }

    public static List<(OrchDuDriveInfo drive, DuProject project)> EnumFolders(IEnumerable<string?>? path, bool recurse = false) ///, bool includeRoot = false)
    {
        var paths = ResolveOrchDrivePaths(path);

        List<(OrchDuDriveInfo drive, DuProject project)> ret = [];

        HashSet<string> visited = [];
        foreach (var p in paths)
        {
            OrchDuDriveInfo drive = p.Drive as OrchDuDriveInfo;
            if (drive is null) continue;

            var dicProjects = drive!.GetDuProjects();
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
            string projectName = Path.GetFileName(p.Path);

            foreach (var project in dicProjects)
            {
                if (string.Compare(project.name, projectName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters on UiPathOrchDu drive.");
        }
        return ret;
    }

    public void ClearAllCache()
    {
        ParentDrive._dicTenantId = null;
        ParentDrive._dicTenantKey = null;

        #region DU cache
        _dicDuClassifier = null;
        _dicDuClassifier_Exceptions.ClearCache();

        _dicDuDocumentTypes = null;
        _dicDuDocumentTypes_Exceptions.ClearCache();

        _dicDuProjects = null;
        _dicDuProjects_Exception.ClearCache();

        _dicDuRoles = null;
        _dicDuRoles_Exception.ClearCache();

        _dicDuUsers = null;
        _dicDuUsers_Exceptions.ClearCache();

        #endregion
    }

    #region Document Understanding cache

    // このエンティティは、テナントではなく組織に所属している。
    // あるべきでいえば、ドライブの static なメンバに partitionGlobalId をキーとして保持すべきだ。
    // ドライブごとにキャッシュをもつのは非効率なんだけど、とりあえず良いか。。
    // TODO: static メンバにして、partitionGlobalId をキーにする形に書き直す。
    // でも、書き直すと Path の形式が不自然になってしまうな。。
    internal DuRole[]? _dicDuRoles = null;
    internal readonly ExceptionCachePerTenant _dicDuRoles_Exception = new();
    public DuRole[]? GetDuRoles()
    {
        _dicDuRoles_Exception.ThrowCachedExceptionIfAny();

        if (_dicDuRoles is null)
        {
            lock (_dicDuRoles_Exception)
            {
                if (_dicDuRoles is null)
                {
                    try
                    {
                        var partitionGlobalId = ParentDrive.GetPartitionGlobalId();
                        _dicDuRoles = OrchAPISession.GetDuRoles(partitionGlobalId);
                        if (_dicDuRoles is null)
                        {
                            _dicDuRoles = [];
                        }
                        else
                        {
                            foreach (var role in _dicDuRoles)
                            {
                                role.Path = NameColonSeparator;
                            }
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicDuRoles_Exception.CacheException(ex);
                        throw;
                    }
                }
            }
        }
        return _dicDuRoles;
    }

    internal DuProject[]? _dicDuProjects = null;
    internal readonly ExceptionCachePerTenant _dicDuProjects_Exception = new();
    public DuProject[]? GetDuProjects()
    {
        _dicDuProjects_Exception.ThrowCachedExceptionIfAny();

        if (_dicDuProjects is null)
        {
            lock (this)
            {
                if (_dicDuProjects is null)
                {
                    try
                    {
                        _dicDuProjects = OrchAPISession.GetDuProjects();
                        if (_dicDuProjects is null)
                        {
                            _dicDuProjects = [];
                        }
                        else
                        {
                            foreach (var project in _dicDuProjects)
                            {
                                project.Path = NameColonSeparator;
                                project.FullName = NameColonSeparator + project.name;
                            }
                        }
                        _dicDuProjects = _dicDuProjects.OrderBy(p => p.name).ToArray();
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicDuProjects_Exception.CacheException(ex);
                        throw;
                    }
                }
            }
        }
        return _dicDuProjects;
    }

    internal ConcurrentDictionary<(string partitionGlobalId, string tenantKey, string projectId), DuUser[]>? _dicDuUsers = null;
    internal readonly ExceptionsCachePer<(string partitionGlobalId, string tenantKey, string projectId)> _dicDuUsers_Exceptions = new();
    public DuUser[] GetDuUsers(DuProject? project)
    {
        var partitionGlobalId = ParentDrive.GetPartitionGlobalId();
        var tenantKey = ParentDrive.GetTenantId().key;
        if (partitionGlobalId is null || tenantKey is null || project is null || project.id is null) return [];

        _dicDuUsers_Exceptions.ThrowCachedExceptionIfAny((partitionGlobalId, tenantKey, project.id));

        if (_dicDuUsers is null)
        {
            lock (_dicDuUsers_Exceptions)
            {
                _dicDuUsers ??= [];
            }
        }

        if (!_dicDuUsers.TryGetValue((partitionGlobalId, tenantKey, project.id), out var users))
        {
            try
            {
                users = OrchAPISession.GetDuUsers(partitionGlobalId, tenantKey, project.id);
                if (users is null)
                {
                    return [];
                }
                else
                {
                    string pathProject = project.GetPSPath();
                    foreach (var user in users)
                    {
                        user.Path = pathProject;
                        user.Project = project.name;
                    }
                    _dicDuUsers[(partitionGlobalId, tenantKey, project.id)] = users;

                    // 三嶋さん(KDDI)からのリクエスト Add-DuUser に User Principal Name を指定できるように
                    // するなら、次が必要だと思うが、良い実装が思いつかない。
                    // パフォーマンスを犠牲にするか、あるいは複雑なパラメータを追加するか。。
                    // 自分としては、どちらも受け入れがたいな。。
                    //#region UserName をバルクで問い合わせる
                    //foreach (var groupedUsers in users.GroupBy(u => u.type))
                    //{
                    //    var entityType = groupedUsers.Key switch
                    //    {
                    //        "DirectoryGroup" => "group",
                    //        "DirectoryApplication" => "application",
                    //        _ => "user"
                    //    };

                    //    var dic = ParentDrive.PmBulkResolveByName(entityType, groupedUsers, user => user.Name ?? user.email ?? "");
                    //    foreach (var user in groupedUsers)
                    //    {
                    //        if (!string.IsNullOrEmpty(user.Name) && dic.TryGetValue(user.Name, out var pmUser))
                    //        {
                    //            user.UserName = pmUser?.name;
                    //        }
                    //    }
                    //}
                    //#endregion
                }
            }
            catch (HttpResponseException ex)
            {
                _dicDuUsers_Exceptions.CacheException((partitionGlobalId, tenantKey, project.id), ex);
                throw;
            }
        }
        return users;
    }

    // key: projectId
    internal ConcurrentDictionary<string, DuDocumentType[]>? _dicDuDocumentTypes = null;
    internal readonly ExceptionsCachePer<string> _dicDuDocumentTypes_Exceptions = new();
    public DuDocumentType[]? GetDuDocumentTypes(DuProject project)
    {
        _dicDuDocumentTypes_Exceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicDuDocumentTypes is null)
        {
            lock (this)
            {
                _dicDuDocumentTypes ??= [];
            }
        }

        if (!_dicDuDocumentTypes.TryGetValue(project.id!, out var documentTypes))
        {
            try
            {
                documentTypes = OrchAPISession.GetDuDocumentTypes(project.id);
                if (documentTypes is null)
                {
                    documentTypes = [];
                }
                else
                {
                    string pathProject = project.GetPSPath();
                    foreach (var documentType in documentTypes)
                    {
                        documentType.Path = pathProject;
                        documentType.Project = project.name;
                    }
                    _dicDuDocumentTypes[project.id!] = documentTypes;
                }
            }
            catch (HttpResponseException ex)
            {
                _dicDuDocumentTypes_Exceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return documentTypes;
    }

    // key: projectId
    internal ConcurrentDictionary<string, DuClassifier[]>? _dicDuClassifier = null;
    internal readonly ExceptionsCachePer<string> _dicDuClassifier_Exceptions = new();
    public DuClassifier[]? GetDuClassifiers(DuProject project)
    {
        _dicDuClassifier_Exceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicDuClassifier is null)
        {
            lock (this)
            {
                _dicDuClassifier ??= [];
            }
        }

        if (!_dicDuClassifier.TryGetValue(project.id!, out var classifiers))
        {
            try
            {
                classifiers = OrchAPISession.GetDuClassifiers(project.id);
                if (classifiers is null)
                {
                    classifiers = [];
                }
                else
                {
                    string pathProject = project.GetPSPath();
                    foreach (var classifier in classifiers)
                    {
                        classifier.Path = pathProject;
                        classifier.Project = project.name;
                    }
                    _dicDuClassifier[project.id!] = classifiers;
                }
            }
            catch (HttpResponseException ex)
            {
                _dicDuClassifier_Exceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return classifiers;
    }

    // key: projectId
    internal ConcurrentDictionary<string, DuExtractor[]>? _dicDuExtractors = null;
    internal readonly ExceptionsCachePer<string> _dicDuExtractorsExceptions = new();
    public DuExtractor[]? GetDuExtractors(DuProject project)
    {
        _dicDuExtractorsExceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_dicDuExtractors is null)
        {
            lock (this)
            {
                _dicDuExtractors ??= [];
            }
        }

        if (!_dicDuExtractors.TryGetValue(project.id!, out var extractors))
        {
            try
            {
                extractors = OrchAPISession.GetDuExtractors(project.id);
                if (extractors is null)
                {
                    extractors = [];
                }
                else
                {
                    string pathProject = project.GetPSPath();
                    foreach (var extractor in extractors)
                    {
                        extractor.Path = pathProject;
                        extractor.Project = project.name;
                    }
                    _dicDuExtractors[project.id!] = extractors;
                }
            }
            catch (HttpResponseException ex)
            {
                _dicDuExtractorsExceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return extractors;
    }

    #endregion

}
