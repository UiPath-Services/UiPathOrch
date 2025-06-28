using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id_Version;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchPackage", SupportsShouldProcess = true)]
public class RemovePackageCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(VersionsCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PathCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

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
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // パラメータで選択済みの Id は、候補から除外する
            var wpId = CreateWPListFromParameter(commandAst, "Id", TPositional.Parameters, wordToComplete);

            // パラメータで選択された Version のみ対象とする
            var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

            foreach (var result in results)
            {
                foreach (var package in result
                    .Where(m => wp.IsMatch(m.Id))
                    .ExcludeByWildcards(p => p?.Id, wpId)
                    .FilterByWildcards(p => p?.Version, wpVersion)
                    .OrderBy(m => m.Id))
                {
                    string tiphelp = TipHelp(package);
                    yield return new CompletionResult(PathTools.EscapePSText(package.Id), package.Id, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class VersionsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");
            var paramDepth = GetParameterValue(commandAst, "Depth");

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // パラメータで選択された Id のみ対象とする
            var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

            // パラメータで選択済みの Version は、候補から除外する
            var wpVersion = CreateWPListFromParameter(commandAst, "Version", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                var packages = drive.GetPackages(folder).FilterByWildcards(p => p?.Id, wpId);
                return ParallelResults3.GroupBy(packages, package =>
                    drive.GetPackageVersions(folder, package.Id!));
            });

            foreach (var result in results)
            {
                foreach (var package in result)
                {
                    foreach (var version in package
                        .Where(v => wp.IsMatch(v.Version!))
                        .ExcludeByWildcards(v => v?.Version, wpVersion))
                        //.OrderBy(v => v.Version!, VersionComparer.Instance))
                    {
                        string tiphelp = TipHelp(version);
                        yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                    }
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
            var drives = SessionState.EnumAllOrchDrives()
                .Where(d => d.OrchAPISession.AuthManager.IsAuthenticated);

            var feedFolders = SessionState.EnumPackageFeedFolders(drives.SelectMany(d => new[] { $"{d.Name}:{System.IO.Path.DirectorySeparatorChar}", $"{d.Name}:{System.IO.Path.DirectorySeparatorChar}*" }))
                .Select(df => df.folder.GetPSPath());

            // パラメータで選択済みの Path は、候補から除外する
            var wpPath = CreateWPListFromParameter(commandAst, "Path", TPositional.Parameters, wordToComplete);

            // パラメータで選択済みの Destination も、候補から除外する
            var wpDestination = CreateWPListFromOtherParameters(commandAst, "Destination", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var path in feedFolders
                .Where(path => wp.IsMatch(path))
                .ExcludeByWildcards(path => path, wpPath)
                .ExcludeByWildcards(path => path, wpDestination))
            {
                yield return new CompletionResult(PathTools.EscapePSText(path));
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(Path, Recurse.IsPresent);

        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var packages = drive.GetPackages(folder);

                foreach (var package in packages
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!);

                        foreach (var version in versions
                            .FilterByWildcards(v => v?.Version, wpVersion))
                            //.OrderBy(v => v.Version!, VersionComparer.Instance))
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            string target = $"{version.GetPSPath()}:{version.Version}";
                            if (ShouldProcess(target, "Remove Package"))
                            {
                                try
                                {
                                    string feedId = drive.FolderFeedId.Get(folder);
                                    drive.OrchAPISession.RemovePackage(version.Id!, version.Version!, feedId);
                                    drive._dicPackages?.TryRemove(feedId ?? "", out _);
                                    if (drive._dicPackageVersions?.TryGetValue(feedId ?? "", out var packageVersionsByPackageId) ?? false)
                                    {
                                        packageVersionsByPackageId.TryRemove(version.Id!, out _);
                                    }
                                    drive._dicPackageVersions?.TryRemove(feedId ?? "", out _);
                                }
                                catch (Exception ex)
                                {
                                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemovePackageError", ErrorCategory.InvalidOperation, version);
                                    WriteError(errorRecord);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
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

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Path, Recurse.IsPresent);

    //    var wpId = Id!.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();
    //    var wpVersion = Version?.Select(ver => new WildcardPattern(ver, WildcardOptions.IgnoreCase)).ToList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df =>
    //        {
    //            var packages = df.drive.GetPackages(df.folder)
    //                .FilterByWildcards(p => p.Id!, wpId)
    //                .OrderBy(p => p.Id!.ToLower());

    //            return OrchThreadPool.RunForEach(packages,
    //                package => package.GetPSPath(),
    //                package => package,
    //                package => df.drive.GetPackageVersions(df.folder, package.Id!));
    //        });

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            using var threads = result.GetResult(cancelHandler.Token);

    //            foreach (var thread in threads!)
    //            {
    //                try
    //                {
    //                    var versions = thread.GetResult(cancelHandler.Token);
    //                    var (drive, folder) = result.Source;

    //                    foreach (var version in versions!
    //                        .FilterByWildcards(v => v.Version!, wpVersion)
    //                        .OrderBy(v => v.Version!, VersionComparer.Instance))
    //                    {
    //                        cancelHandler.Token.ThrowIfCancellationRequested();

    //                        string target = $"{version.GetPSPath()}:{version.Version}";
    //                        if (ShouldProcess(target, "Remove Package"))
    //                        {
    //                            try
    //                            {
    //                                string feedId = drive.GetFolderFeedId(folder);
    //                                drive.OrchAPISession.RemovePackage(version.Id!, version.Version!, feedId);
    //                                drive._dicPackages?.TryRemove(feedId ?? "", out _);
    //                                if (drive._dicPackageVersions?.TryGetValue(feedId ?? "", out var packageVersionsByPackageId) ?? false)
    //                                {
    //                                    packageVersionsByPackageId.TryRemove(version.Id!, out var _);
    //                                }
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemovePackageError", ErrorCategory.InvalidOperation, version);
    //                                WriteError(errorRecord);
    //                            }
    //                        }
    //                    }
    //                }
    //                catch (OrchException ex)
    //                {
    //                    WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
