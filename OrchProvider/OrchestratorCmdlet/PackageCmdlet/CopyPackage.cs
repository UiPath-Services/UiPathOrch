using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchPackage", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.BulkItemDtoOfString))]
    public class CopyPackageCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageIdCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageVersionCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Version { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageFeedFolderCompleter))]
        [SupportsWildcards]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PathCompleter))]
        [SupportsWildcards]
        public string? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

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
                var selectedDestination = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete)
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

        private class NoCorrespondDestinatoinFolderException : Exception {}

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

        private bool DownloadPackage(OrchDriveInfo srcDrive, string? srcFeedId, Package srcVersion, out string? fileName, out byte[]? fileContent)
        {
            try
            {
                (fileName, fileContent) = srcDrive.OrchAPISession.DownloadPackage(srcFeedId!, srcVersion.Id!, srcVersion.Version!);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcVersion.GetPSPath(), ex), "DownloadPackageError", ErrorCategory.InvalidOperation, srcVersion));
                fileName = null;
                fileContent = null;
                return false;
            }
            return true;
        }

        private bool UploadPackage(Package srcVersion, OrchDriveInfo dstDrive, Folder dstFolder, string? fileName, byte[]? fileContent)
        {
            if (string.IsNullOrEmpty(fileName) || fileContent == null) return false;
            try
            {
                string dstFeedId = dstDrive.FolderFeedId.Get(dstFolder);
                var copiedPackage = dstDrive.OrchAPISession.UploadPackage(dstFeedId, fileName!, fileContent!);
                if (copiedPackage != null)
                {
                    dstDrive._dicPackages?.TryRemove(dstFeedId ?? "", out var _);
                    dstDrive._dicPackageVersions?.TryRemove(dstFeedId ?? "", out var _);

                    // コピー先フィードが個人用ワークスペースなら、プロセスのキャッシュもクリアする
                    if (dstFolder.FolderType == "Personal")
                    {
                        dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out var _);
                        //dstDrive._dicReleaseList?.TryRemove(dstFolder.Id ?? 0, out var _);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                string target = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
                WriteError(new ErrorRecord(new OrchException(target, ex), "UploadPackageError", ErrorCategory.InvalidOperation, target));
            }

            return false;
        }

        protected override void ProcessRecord()
        {
            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            Id = Id.Split1stValueByUnescapedCommas()?.ToArray();
            Version = Version.Split1stValueByUnescapedCommas()?.ToArray();
            Destination = Destination.Split1stValueByUnescapedCommas()?.ToArray();

            var wpId = Id.ConvertToWildcardPatternList();
            var wpVersion = Version.ConvertToWildcardPatternList();

            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFeedFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumPackageFeedFolders([srcRootFolder.GetPSPath()], Recurse.IsPresent);
            var dstDrivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Destination);

            if (srcRootFolder != srcDrive.RootFolder && Recurse.IsPresent)
            {
                throw new Exception("The -Recurse can only be used when the source folder is the root folder.");
            }

            string msg1 = "Processing folders...";
            int index1 = 0;
            using var reporterMain = new ProgressReporter(this, 1, srcDrivesFolders.Count, msg1, msg1);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                //reporterMain.WriteProgress(++indexMain, $"{indexMain:D}/{srcDrivesFolders.Count} {srcFolder.GetPSPath()}");
                if (srcDrivesFolders.Count > 1) reporterMain.WriteProgress(++index1, $"{index1:D}/{srcDrivesFolders.Count}");
                try
                {
                    var srcPackages = srcDrive.GetPackages(srcFolder)
                        .FilterByWildcards(p => p?.Id, wpId)
                        .OrderBy(p => p.Id)
                        .ToList();

                    var srcFeedId = srcDrive.FolderFeedId.Get(srcFolder);

                    string msg2 = "Processing packages...";
                    int index2 = 0;
                    using var reporter2 = new ProgressReporter(this, 2, srcPackages.Count, msg2, msg2);
                    foreach (var srcPackage in srcPackages)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (reporter2.TotalNum > 1) reporter2.WriteProgress(++index2, $"{index2:D}/{reporter2.TotalNum}");

                        var srcVersions = srcDrive.GetPackageVersions(srcFolder, srcPackage.Id!)
                            .FilterByWildcards(p => p?.Version, wpVersion)
                            //.OrderBy(p => p.Version!, VersionComparer.Instance)
                            .ToList();

                        string msg3 = "Copying versions...   ";
                        int index3 = 0;
                        using var reporter3 = new ProgressReporter(this, 3, srcVersions.Count * dstDrivesFolders.Count, msg3, msg3);
                        foreach (var srcVersion in srcVersions)
                        {
                            string fileName = null;
                            byte[] fileContent = null;

                            foreach (var (dstDrive, dstRootFolder) in dstDrivesFolders)
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();

                                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, true);
                                // 同名のフォルダがない場合は、コピー処理をスキップする
                                if (dstFolder == null) throw new NoCorrespondDestinatoinFolderException();

                                if (srcDrive == dstDrive && srcFolder == dstFolder) continue;

                                // 同名のフォルダはあるが、フォルダフィードがない場合には、コピー処理をスキップする
                                // （テナントフィードにコピーすることはしない）
                                if (dstFolder != dstDrive.RootFolder && dstFolder.FeedType != "FolderHierarchy")
                                {
                                    WriteError(new ErrorRecord(
                                        new OrchException(srcFolder.GetPSPath(), $"Skipping folder '{dstFolder.GetPSPath()}' as its FeedType is not 'FolderHierarchy'."),
                                        "CopyFolderEntityToRootFolderError",
                                        ErrorCategory.InvalidOperation,
                                        dstDrive));
                                    throw new NoCorrespondDestinatoinFolderException();
                                }

                                // dstFolder に同名のパッケージがあれば、警告を表示してコピーをスキップする
                                if (PackageExists(dstDrive, dstFolder, srcVersion))
                                {
                                    WriteError(new ErrorRecord(new InvalidOperationException($"\"{srcVersion.GetPSPath()}:{srcVersion.Version}\": Package already exists in {dstFolder.GetPSPath()}. Skipping the copy."), "CopyPackageError", ErrorCategory.WriteError, dstFolder));
                                    continue;
                                }

                                string key = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
                                string target = $"Item: {key} Destination: {dstFolder.GetPSPath()}";

                                if (ShouldProcess(target, $"Copy Package"))
                                {
                                    // 進捗は、実際にコピーするときにだけ表示された方が良い
                                    reporter3.WriteProgress(++index3, $"{index3:D}/{reporter3.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

                                    if (fileName == null)
                                    {
                                        if (!DownloadPackage(srcDrive, srcFeedId, srcVersion, out fileName, out fileContent)) break;
                                    }
                                    UploadPackage(srcVersion, dstDrive, dstFolder, fileName, fileContent);
                                }
                            }
                        }
                    }
                }
                catch (NoCorrespondDestinatoinFolderException)
                {
                    // この例外は、コピー先フォルダーがない場合に処理をスキップするときにスローされる。
                    // 警告はコンソールに出力済みなので、ここでは何もしない
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // だいぶ雑だな。。とりあえずいいか、、
                    WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "CopyFolderMachineError", ErrorCategory.InvalidOperation, srcDrive));
                }
            }
        }
    }
}
