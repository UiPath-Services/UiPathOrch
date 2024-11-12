using System;
using System.Globalization;
using System.Management.Automation;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Positional = UiPath.PowerShell.Positional.Source_Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Import, "OrchPackage", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.BulkItemDtoOfString))]
    public class ImportPackageCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[]? Source { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter SourceRecurse { get; set; }

        private static bool PackageExists(OrchDriveInfo drive, Folder folder, string fullPath)
        {
            try
            {
                var (id, version) = ExtractPackageIdVersionFromFilePath(fullPath);
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                {
                    var dstExistingVersions = drive.GetPackageVersions(folder, id);
                    if (dstExistingVersions != null)
                    {
                        return dstExistingVersions.Any(v => v.Version == version);
                    }
                }
            }
            catch { } // この例外は握りつぶす

            return false;
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Path);
            if (SourceRecurse.IsPresent && drivesFolders.Any(df => df.folder != df.drive.RootFolder))
            {
                throw new Exception("The -SourceRecurse parameter can only be specified for the tenant's root folder.");
            }

            var pkgFilePaths = OrchDriveInfo.ExpandLocalPath(Source, "*.nupkg", SourceRecurse.IsPresent, SourceRecurse.IsPresent ? 1 : 0)
                .OrderByFileNameVersion();

            var tasks = drivesFolders
                .SelectMany(df => pkgFilePaths, (df, pkgFilePath) =>
                    (df.drive, df.folder, pkgFilePath.FullPath, pkgFilePath.RelativePath))
                .ToList();

            int totalNum = tasks.Count;

            string msg = "Importing Packages";
            using var reporter = new ProgressReporter(this, 1, totalNum, msg, msg);

            int index = 0;
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var task in tasks)
            {
                var (drive, folder, fullPath, relativePath) = task;

                HashSet<string> ignoredFolders = new();
                Entities.Folder targetFolder = null;
                if (relativePath == "." || string.IsNullOrEmpty(relativePath))
                {
                    targetFolder = folder;
                }
                else
                {
                    if (ignoredFolders.Contains(System.IO.Path.GetDirectoryName(fullPath)!))
                    {
                        continue;
                    }

                    targetFolder = drive!.GetFolder(relativePath);
                    if (targetFolder == null)
                    {
                        WriteWarning($"Folder {relativePath} does not exist on {drive.NameColonSeparator}. Ignored.");
                        ignoredFolders.Add(System.IO.Path.GetDirectoryName(fullPath)!);
                        continue;
                    }
                    if (targetFolder.FeedType != "FolderHierarchy")
                    {
                        WriteWarning($"Folder {drive.NameColonSeparator}{relativePath} exists, but its FeedType is {targetFolder.FeedType}. Ignored.");
                        ignoredFolders.Add(System.IO.Path.GetDirectoryName(fullPath)!);
                        continue;
                    }
                }

                cancelHandler.Token.ThrowIfCancellationRequested();

                string target = targetFolder.GetPSPath();
                string target2 = target + System.IO.Path.GetFileName(fullPath);
                if (ShouldProcess(target, $"Import Package {fullPath}"))
                {
                    reporter.WriteProgress(++index, $"{index:D}/{totalNum}");
                    try
                    {
                        // targetFolder に同名のパッケージがあれば、警告を表示してコピーをスキップする
                        if (PackageExists(drive, targetFolder, fullPath))
                        {
                            WriteError(new ErrorRecord(new InvalidOperationException($"\"{fullPath}\": Package already exists in {targetFolder.GetPSPath()}. Skipping the import."), "ImportPackageError", ErrorCategory.WriteError, targetFolder));
                            continue;
                        }

                        string? feedId = drive.FolderFeedId.Get(targetFolder);
                        var result = drive.OrchAPISession.UploadPackage(feedId!, fullPath);
                        if (result != null)
                        {
                            result.Path = target;
                            WriteObject(result);
                        }
                        drive._dicPackages?.TryRemove(feedId ?? "", out var _);
                        drive._dicPackageVersions?.TryRemove(feedId ?? "", out var _);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target2, ex), "ImportPackageError", ErrorCategory.InvalidOperation, target2));
                    }
                }
            }
        }
    }
}
