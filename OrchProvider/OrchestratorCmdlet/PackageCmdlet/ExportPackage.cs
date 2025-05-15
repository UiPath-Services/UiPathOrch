using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchPackage", SupportsShouldProcess = true)]
public class ExportPackageCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(IdCompleter))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(VersionCompleter))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(paramPath, recurse);

            // パラメータで選択済みの Id は、候補から除外する
            var wpId = CreateWPListFromParameter(commandAst, "Id", TPositional.Parameters, wordToComplete);

            // パラメータで選択された Version のみ対象とする
            var paramVersion = GetParameterValues(commandAst, "Version", TPositional.Parameters).ToList();
            var wpVersion = paramVersion.Select(ver => new WildcardPattern(ver, WildcardOptions.IgnoreCase)).ToList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetPackages(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var e in result.Result
                    .Where(m => wp.IsMatch(m.Id))
                    .ExcludeByWildcards(p => p?.Id, wpId)
                    .FilterByWildcards(p => p?.Version, wpVersion)
                    .OrderBy(m => m.Id))
                {
                    string tiphelp = TipHelp(e);
                    yield return new CompletionResult(PathTools.EscapePSText(e.Id), e.Id, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class VersionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(paramPath, recurse);

            // パラメータで選択された Id のみ対象とする
            var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

            // パラメータで選択済みの Version は、候補から除外する
            var wpVersion = CreateWPListFromParameter(commandAst, "Version", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                var packages = drive.GetPackages(folder)
                    .FilterByWildcards(p => p?.Id, wpId);
                return ParallelResults.ForEach(packages, package =>
                    drive.GetPackageVersions(folder, package.Id!));
            });

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var package in result.Result)
                {
                    if (package.Result is null) continue;

                    foreach (var version in package.Result
                        .Where(v => wp.IsMatch(v.Version))
                        .ExcludeByWildcards(v => v?.Version, wpVersion))
                        //.OrderBy(v => v.Version!, VersionComparer.Instance))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(version.Version));
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Path, Recurse.IsPresent);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        if (Destination is null)
        {
            Destination = SessionState.Path.CurrentFileSystemLocation.Path;
        }
        
        // PSDrive のパスを、実際のファイルシステムのパスに変換
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        if (!Directory.Exists(Destination))
        {
            throw new DirectoryNotFoundException($"A directory '{Destination}' does not exist.");
        }

        // c: のようなパスを、現在のパスを考慮して完全パスに変換しておく
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        // 最初にすべてまとめて非同期に API call するバージョン
        // ちゃんと動いているけど、API call の数が多すぎてしまうかも。
#if false
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var packages = df.drive.GetPackages(df.folder)
                    .FilterByWildcards(p => p.Id!, wpId)
                    .OrderBy(p => p.Id!.ToLower());

                return OrchThreadPool.RunForEach(packages,
                    package => package.GetPSPath(),
                    package => package,
                    package => df.drive.GetPackageVersions(df.folder, package.Id!)
                        .FilterByWildcards(v => v.Version!, wpVersion)
                        .OrderBy(v => v.Version!, VersionComparer.Instance));
            });

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export Package", "Export Package");
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult(cancelHandler.Token);
                var (drive, folder) = result.Source;

                string feedFolder = folder.GetPackageFeedFolder();
                string feedId = drive.GetFolderFeedId(folder);
                string folderDisplayName = OrchDriveInfo.MakeValidFolderName(feedFolder);

                string destination;
                if (string.IsNullOrEmpty(feedFolder))
                {
                    destination = Destination;
                }
                else
                {
                    destination = System.IO.Path.Combine(Destination, folderDisplayName);
                }

                foreach (var thread in threads!)
                {
                    try
                    {
                        var versions = thread.GetResult(cancelHandler.Token);

                        reporter.TotalNum = versions!.Count();
                        int index = 0;
                        foreach (var version in versions!)
                        {
                            string target;
                            if (string.IsNullOrEmpty(folderDisplayName))
                            {
                                target = drive.NameColonSeparator + version.Id + ':' + version.Version;
                            }
                            else
                            {
                                target = drive.NameColonSeparator + folderDisplayName + System.IO.Path.DirectorySeparatorChar + version.Id + ':' + version.Version;
                            }

                            reporter.WriteProgress(++index, $"{version.GetPSPath()}:{version.Version}");
                            if (ShouldProcess(target, "Export Package"))
                            {
                                try
                                {
                                    Directory.CreateDirectory(destination);
                                    var (fileName, fileContent) = drive.OrchAPISession.DownloadPackage(feedId!, version.Id!, version.Version!);
                                    string filePath = System.IO.Path.Combine(destination, fileName!);
                                    File.WriteAllBytes(filePath, fileContent);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "ExportPackageError", ErrorCategory.InvalidOperation, target));
                                }
                            }
                        }
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
#endif

        // 最初の GetPackage() だけ非同期に実行するバージョン
        // GetPackageVersion() は、ダウンロードの直前に呼び出す。
        // フォルダフィードが多い場合は、処理開始までに時間がかかるかな？
#if false
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.GetPackages(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export Package", "Export Package");
        foreach (var result in results)
        {
            try
            {
                var packages = result.GetResult(cancelHandler.Token);
                var (drive, folder) = result.Source;

                string feedFolder = folder.GetPackageFeedFolder();
                string folderDisplayName = OrchDriveInfo.MakeValidFolderName(feedFolder);

                string feedId = drive.GetFolderFeedId(folder);

                string destination;
                if (string.IsNullOrEmpty(feedFolder))
                {
                    destination = Destination;
                }
                else
                {
                    destination = System.IO.Path.Combine(Destination, folderDisplayName);
                }

                foreach (var package in packages!
                    .FilterByWildcards(p => p.Id!, wpId)
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v.Version!, wpVersion)
                            .OrderBy(v => v.Version!, VersionComparer.Instance).ToList();

                        int index = 0;
                        reporter.TotalNum = versions!.Count;
                        foreach (var version in versions!)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            string target = version.GetPSPath() + ':' + version.Version;
                            reporter.WriteProgress(++index, $"{index:D}/{versions.Count} {target}");
                            if (ShouldProcess(target, "Export Package"))
                            {
                                try
                                {
                                    Directory.CreateDirectory(destination);
                                    var (fileName, fileContent) = drive.OrchAPISession.DownloadPackage(feedId!, version.Id!, version.Version!);
                                    string filePath = System.IO.Path.Combine(destination, fileName!);
                                    File.WriteAllBytes(filePath, fileContent);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "ExportPackageError", ErrorCategory.InvalidOperation, version));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(package.GetPSPath(), ex), "GetPackageVersionError", ErrorCategory.InvalidOperation, package));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
#endif

        // 完全にシングルスレッドで処理するバージョン
        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export packages");
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var packages = drive.GetPackages(folder);

                string feedFolder = folder.GetPackageFeedFolder();
                string folderDisplayName = OrchDriveInfo.MakeValidFolderName(feedFolder);

                string feedId = drive.FolderFeedId.Get(folder);

                string destination;
                if (string.IsNullOrEmpty(feedFolder))
                {
                    destination = Destination;
                }
                else
                {
                    destination = System.IO.Path.Combine(Destination, folderDisplayName);
                }

                foreach (var package in packages!
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            //.OrderBy(v => v.Version!, VersionComparer.Instance)
                            .ToList();

                        int index = 0;
                        reporter.TotalNum = versions!.Count;
                        foreach (var version in versions!)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            string target = version.GetPSPath() + ':' + version.Version;
                            reporter.WriteProgress(++index, target);
                            if (ShouldProcess(target, "Export Package"))
                            {
                                try
                                {
                                    Directory.CreateDirectory(destination);
                                    var (fileName, fileContent) = drive.OrchAPISession.DownloadPackage(feedId!, version.Id!, version.Version!);
                                    string filePath = System.IO.Path.Combine(destination, fileName!);
                                    File.WriteAllBytes(filePath, fileContent);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "ExportPackageError", ErrorCategory.InvalidOperation, version));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(package.GetPSPath(), ex), "GetPackageVersionError", ErrorCategory.InvalidOperation, package));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
