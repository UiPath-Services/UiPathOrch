using UiPath.OrchAPI;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.PowerShell.Entities;
using Job = UiPath.PowerShell.Entities.Job;
using UiPath.PowerShell.Commands;
using System.Net.Sockets;
using Microsoft.Management.Infrastructure.Options;
using System.ComponentModel;
using License = UiPath.PowerShell.Entities.License;

namespace UiPath.PowerShell.Core
{
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
                _NameColon ??= Name + Path.VolumeSeparatorChar;
                return _NameColon;
            }
        }
        internal string NameColonSeparator
        {
            get
            {
                _NameColonSeparator ??= Name + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar;
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
            base(driveName, provider, driveName + ":\\", description, null, root)
        {
        }

        // paths を指定しない場合、カレントドライブのみを返す
        public static List<OrchDuDriveInfo> EnumOrchDrives(IEnumerable<string?>? paths = null)
        {
            var drives = new List<OrchDuDriveInfo>();
            if (paths == null || !paths.Any() || paths.All(p => p == null))
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

        public static IEnumerable<PathInfo> ResolveOrchDrivePaths(IEnumerable<string?>? paths = null)
        {
            if (paths == null || !paths.Any() || paths.All(p => p == null))
            {
                PathInfo pathInfo = SessionState!.Path.CurrentLocation;
                if (pathInfo.Drive is OrchDuDriveInfo)
                {
                    yield return SessionState!.Path.CurrentLocation;
                }
            }
            else
            {
                var psPaths = paths.Where(p => p != null).Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
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
                if (drive == null) continue;

                var dicProjects = drive!.GetDuProjects();
                if (dicProjects == null) continue;

                //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
                //if (folder == null) continue;

                // Recurse が指定されていて、かつルートフォルダであれば、すべてのプロジェクトを返せばOK
                if (recurse && p.Path.EndsWith('\\'))
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

            if (ret == null || ret.Count == 0)
            {
                throw new Exception("Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters on UiPathOrchDu drive.");
            }
            return ret;
        }

        public void ClearAllCache()
        {
            #region DU cache
            _dicDuProjects = null;
            _dicDuDocumentTypes = null;
            _dicDuClassifier = null;

            _dicDuProjectsException.ClearCache();
            _dicDuDocumentTypeExceptions.ClearCache();
            _dicDuClassifierExceptions.ClearCache();
            #endregion
        }

        #region Document Understanding cache
        internal DuProject[]? _dicDuProjects = null;
        internal readonly ExceptionCachePerTenant _dicDuProjectsException = new();
        public DuProject[]? GetDuProjects()
        {
            _dicDuProjectsException.ThrowCachedExceptionIfAny();

            if (_dicDuProjects == null)
            {
                lock (this)
                {
                    if (_dicDuProjects == null)
                    {
                        try
                        {
                            _dicDuProjects = OrchAPISession.GetDuProjects();
                            if (_dicDuProjects == null)
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
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicDuProjectsException.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicDuProjects;
        }

        // key: projectId
        internal Dictionary<string, DuDocumentType[]>? _dicDuDocumentTypes = null;
        internal readonly ExceptionsCachePer<string> _dicDuDocumentTypeExceptions = new();
        public DuDocumentType[]? GetDuDocumentTypes(DuProject project)
        {
            _dicDuDocumentTypeExceptions.ThrowCachedExceptionIfAny(project.id!);

            if (_dicDuDocumentTypes == null)
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
                    if (documentTypes == null)
                    {
                        documentTypes = [];
                    }
                    else
                    {
                        string pathProject = project.GetPSPath();
                        foreach (var documentType in documentTypes)
                        {
                            documentType.Path = NameColonSeparator;
                            documentType.PathProject = pathProject;
                            documentType.Project = project.name;
                        }
                        _dicDuDocumentTypes[project.id!] = documentTypes;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicDuDocumentTypeExceptions.CacheException(project.id!, ex);
                    throw;
                }
            }
            return documentTypes;
        }

        // key: projectId
        internal Dictionary<string, DuClassifier[]>? _dicDuClassifier = null;
        internal readonly ExceptionsCachePer<string> _dicDuClassifierExceptions = new();
        public DuClassifier[]? GetDuClassifiers(DuProject project)
        {
            _dicDuClassifierExceptions.ThrowCachedExceptionIfAny(project.id!);

            if (_dicDuClassifier == null)
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
                    if (classifiers == null)
                    {
                        classifiers = [];
                    }
                    else
                    {
                        string pathProject = project.GetPSPath();
                        foreach (var classifier in classifiers)
                        {
                            classifier.Path = NameColonSeparator;
                            classifier.PathProject = pathProject;
                            classifier.Project = project.name;
                        }
                        _dicDuClassifier[project.id!] = classifiers;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicDuClassifierExceptions.CacheException(project.id!, ex);
                    throw;
                }
            }
            return classifiers;
        }

        // key: projectId
        internal Dictionary<string, DuExtractor[]>? _dicDuExtractors = null;
        internal readonly ExceptionsCachePer<string> _dicDuExtractorsExceptions = new();
        public DuExtractor[]? GetDuExtractors(DuProject project)
        {
            _dicDuExtractorsExceptions.ThrowCachedExceptionIfAny(project.id!);

            if (_dicDuExtractors == null)
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
                    if (extractors == null)
                    {
                        extractors = [];
                    }
                    else
                    {
                        string pathProject = project.GetPSPath();
                        foreach (var extractor in extractors)
                        {
                            extractor.Path = NameColonSeparator;
                            extractor.PathProject = pathProject;
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
}
