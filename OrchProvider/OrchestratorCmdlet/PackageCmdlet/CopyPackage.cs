using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id_Version_Destination;
using System.Globalization;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchPackage", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.BulkItemDtoOfString))]
    public class CopyPackageCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageIdCompleter<Positional.Id_Version_Destination>))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(PackageVersionCompleter<Positional.Id_Version_Destination>))]
        public string[]? Version { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(PackageFeedFolderCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(PathCompleter))]
        public string? Path { get; set; }

        internal class PackageFeedFolderCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters, true);

                // パラメータで選択済みの Destination は、候補から除外する
                var selectedDestination = GetParameterValues(commandAst, parameterName, Positional.Id_Version_Destination.Parameters, wordToComplete)
                    .SelectMany(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p))
                    .Select(p => WildcardPattern.Unescape(p.Path.TrimEnd('\\')))
                    .ToList();

                #region 指定されたパスを解決する
                if (wordToComplete != "\\" && wordToComplete != "/" && !wordToComplete.EndsWith(':') &&
                    (!string.IsNullOrEmpty(wordToComplete) || wordToComplete.EndsWith('\\') || wordToComplete.EndsWith('/')))
                {
                    wordToComplete += '*';
                }
                var paths = SessionState?.Path.GetResolvedPSPathFromPSPath(wordToComplete);
                #endregion

                foreach (var p in paths ?? [])
                {
                    var drive = p.Drive as OrchDriveInfo;
                    if (drive == null) continue;

                    string p2 = OrchDriveInfo.PSPathToOrchPath(p.Path);
                    if (string.IsNullOrEmpty(p2))
                    {
                        yield return new CompletionResult(drive.NameColonSeparator);
                        continue;
                    }

                    var folder = drive.GetFolder(p2);

                    // コピー元のフォルダーは外す。ただしルートフォルダは候補に含める
                    if (folder!.Id != null && drivesFolders.Contains((drive!, folder!))) continue;

                    // 選択済みのフォルダーは外す。
                    if (selectedDestination.Contains(p.Path)) continue;

                    if (folder?.ParentId == null && folder?.FeedType == "FolderHierarchy")
                    {
                        string candidate = p.Path
                            .Replace("`", "``")
                            .Replace("'", "''")
                            .Replace("*", "`*")
                            .Replace("?", "`?")
                            .Replace("[", "`[")
                            .Replace("]", "`]");
                        if (candidate != p.Path || candidate.Contains(' '))
                            yield return new CompletionResult($"'{candidate}'", p.Path, CompletionResultType.ProviderContainer, p.Path);
                        else
                            yield return new CompletionResult(p.Path);
                    }
                }
            }
        }

        private class PathCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                if (wordToComplete != "\\" && wordToComplete != "/" && !wordToComplete.EndsWith(':') &&
                    (!string.IsNullOrEmpty(wordToComplete) || wordToComplete.EndsWith('\\') || wordToComplete.EndsWith('/')))
                {
                    wordToComplete += '*';
                }
                var paths = SessionState?.Path.GetResolvedPSPathFromPSPath(wordToComplete);

                foreach (var p in paths ?? [])
                {
                    var drive = p.Drive as OrchDriveInfo;
                    if (drive == null) continue;

                    string p2 = OrchDriveInfo.PSPathToOrchPath(p.Path);
                    if (string.IsNullOrEmpty(p2))
                    {
                        yield return new CompletionResult(drive.NameColonSeparator);
                        continue;
                    }

                    var folder = drive.GetFolder(p2);

                    if (folder?.ParentId == null && folder?.FeedType == "FolderHierarchy")
                    {
                        string candidate = p.Path
                            .Replace("`", "``")
                            .Replace("'", "''")
                            .Replace("*", "`*")
                            .Replace("?", "`?")
                            .Replace("[", "`[")
                            .Replace("]", "`]");
                        if (candidate != p.Path || candidate.Contains(' '))
                            yield return new CompletionResult($"'{candidate}'", p.Path, CompletionResultType.ProviderContainer, p.Path);
                        else
                            yield return new CompletionResult(p.Path);
                    }
                }
            }
        }

        private static bool PackageExists(OrchDriveInfo drive, Folder folder, Package version)
        {
            try
            {
                // Key でも検索できるんだけど、キャッシュが壊れちゃう。
                var dstExistingVersions = drive.GetPackageVersions(folder, version.Id!);
                if (dstExistingVersions != null)
                {
                    return dstExistingVersions.Any(v => v.Version == version.Version);
                }
            }
            catch { } // この例外は握りつぶす

            return false;
        }

        protected override void ProcessRecord()
        {
            var wpId = Id?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            Version = Version.Split1stValueByUnescapedCommas()?.ToArray();

            var wpVersion = Version?.Select(v => new WildcardPattern(v, WildcardOptions.IgnoreCase)).ToList();

            var srcDrivesFolders = OrchDriveInfo.EnumPackageFeedFolders([Path!]);
            var dstDrivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Destination);

            if (srcDrivesFolders.Count > 1)
            {
                throw new Exception($"-Path '{Path}' resolved to multiple containers.");
            }

            if (srcDrivesFolders.Count == 0)
            {
                // TODO: 例外をスローすべきか？
                // throw new Exception($"Path '{Path}' does not exist.");
                return;
            }

            var srcDrive = srcDrivesFolders[0].drive;
            var srcFolder = srcDrivesFolders[0].folder;
            var srcFeedId = srcDrive.GetFolderFeedId(srcFolder);

            var srcPackages = srcDrive.GetPackages(srcFolder)
                .FilterByWildcards(p => p?.Id, wpId)
                .OrderBy(p => p.Id)
                .ToList();

            string msg = "Copying packages";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var srcPackage in srcPackages)
            {
                var srcVersions = srcDrive.GetPackageVersions(srcFolder, srcPackage.Id!)
                    .FilterByWildcards(p => p?.Version, wpVersion)
                    //.OrderBy(p => p.Version!, VersionComparer.Instance)
                    .ToList();

                int index = 0;
                reporter.TotalNum = dstDrivesFolders.Count * srcVersions.Count;
                foreach (var srcVersion in srcVersions)
                {
                    string key = $"{srcDrive.NameColonSeparator}{srcVersion.Id}:{srcVersion.Version}";

                    foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (srcDrive == dstDrive && srcFolder == dstFolder) continue;

                        var target = $"Item: {srcVersion.GetPSPath()}:{srcVersion.Version} Destination: {dstFolder.GetPSPath()}";

                        reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {key}.nupkg to {dstFolder.GetPSPath()}");

                        if (ShouldProcess(target, $"Copy Package"))
                        {
                            // dstFolder に同名のパッケージがあれば、警告を表示してコピーをスキップする
                            if (PackageExists(dstDrive, dstFolder, srcVersion))
                            {
                                WriteError(new ErrorRecord(new InvalidOperationException($"\"{srcVersion.GetPSPath()}:{srcVersion.Version}\": Package already exists in {dstFolder.GetPSPath()}. Skipping the copy."), "CopyPackageError", ErrorCategory.WriteError, dstFolder));
                                continue;
                            }
  
                            string fileName;
                            byte[] fileContent;
                            try
                            {
                                (fileName, fileContent) = srcDrive.OrchAPISession.DownloadPackage(srcFeedId!, srcVersion.Id!, srcVersion.Version!);
                            }
                            catch (Exception ex)
                            {
                                target = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
                                WriteError(new ErrorRecord(new OrchException(srcVersion.GetPSPath(), ex), "DownloadPackageError", ErrorCategory.InvalidOperation, srcVersion));
                                continue;
                            }

                            try
                            {
                                string dstFeedId = dstDrive.GetFolderFeedId(dstFolder);
                                var copiedPackage = dstDrive.OrchAPISession.UploadPackage(dstFeedId, fileName!, fileContent!);
                                if (copiedPackage != null)
                                {
                                    //copiedPackage.Path = dstFolder.GetPSPath();
                                    //WriteObject(copiedPackage);
                                    dstDrive._dicPackages?.TryRemove(dstFeedId ?? "", out var _);
                                    dstDrive._dicPackageVersions?.TryRemove(dstFeedId ?? "", out var _);

                                    // コピー先フィードが個人用ワークスペースなら、プロセスのキャッシュもクリアする
                                    if (dstFolder.FolderType == "Personal")
                                    {
                                        dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out var _);
                                        dstDrive._dicReleaseList?.TryRemove(dstFolder.Id ?? 0, out var _);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                target = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
                                WriteError(new ErrorRecord(new OrchException(target, ex), "UploadPackageError", ErrorCategory.InvalidOperation, target));
                            }
                        }
                    }
                }
            }
        }
    }
}
