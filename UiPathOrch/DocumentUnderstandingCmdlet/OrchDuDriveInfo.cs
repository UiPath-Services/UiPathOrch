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

    protected internal Folder? RootFolder;

    // At the time this constructor runs, NameColonSeparator is not yet available
    public OrchDuDriveInfo(ProviderInfo provider, string driveName, string description, string root) :
        base(driveName, provider, driveName + ':' + Path.DirectorySeparatorChar, description, null, root)
    {
    }

    public void ClearAllCache()
    {
        //ParentDrive._tenantId = null;
        //ParentDrive._tenantKey = null;

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

    // This entity belongs to the organization, not the tenant.
    // Ideally, it should be stored in a static member of the drive keyed by partitionGlobalId.
    // Having a cache per drive is inefficient, but it's fine for now..
    // TODO: Rewrite to use a static member keyed by partitionGlobalId.
    // However, rewriting it that way would make the Path format look unnatural..
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
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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

                    // Feature request from Mishima-san (KDDI): allow specifying User Principal Name in Add-DuUser.
                    // The code below would be needed for that, but I can't think of a good implementation.
                    // Either sacrifice performance, or add complex parameters..
                    // Personally, neither option is acceptable..
                    //#region Bulk-query UserName
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _dicDuExtractorsExceptions.CacheException(project.id!, ex);
                throw;
            }
        }
        return extractors;
    }

    #endregion

}
