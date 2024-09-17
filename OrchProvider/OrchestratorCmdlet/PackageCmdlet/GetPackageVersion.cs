using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id_Version;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPackageVersion")]
    [OutputType(typeof(Entities.Package))]
    public class GetPackageVersionCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public string[]? Id { get; set; }

        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(VersionCompleter))]
        public string[]? Version { get; set; }

        [Parameter]
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
                var paramDepth = GetParameterValue(commandAst, "Depth");
                uint.TryParse(paramDepth, out uint depth);

                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(paramPath, recurse);
                int totalFolderCount = drivesFolders.Count;

                // パラメータで選択済みの Id は、候補から除外する
                var wpId = CreateWPListFromParameter(commandAst, "Id", Positional.Id_Version.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetPackages(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(m => wp.IsMatch(m.Id))
                        .ExcludeByWildcards(p => p?.Id, wpId)
                        .OrderBy(l => l.Id))
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
                var paramId = GetFakeBoundParameters(fakeBoundParameters, "Id");
                var wpId = paramId.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みの Version は、候補から除外する
                var wpVersion = CreateWPListFromParameter(commandAst, "Version", Positional.Id_Version.Parameters, wordToComplete);

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
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var package in entities!)
                    {
                        if (!package.TryGetValue(out var versions)) continue;

                        foreach (var version in versions!
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

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Path, Recurse.IsPresent);
            var wpId = Id?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();
            var wpVersion = Version?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df =>
                {
                    var packages = df.drive.GetPackages(df.folder)
                        .FilterByWildcards(p => p?.Id, wpId)
                        .OrderBy(p => p.Id!.ToLower());

                    return OrchThreadPool.RunForEach(packages,
                        package => package.GetPSPath(),
                        package => package,
                        package => df.drive.GetPackageVersions(df.folder, package.Id!));
                });

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    using var threads = result.GetResult(cancelHandler.Token);

                    foreach (var thread in threads!)
                    {
                        try
                        {
                            var versions = thread.GetResult(cancelHandler.Token);
                            WriteObject(versions!
                                .FilterByWildcards(v => v?.Version, wpVersion),
                                //.OrderBy(v => v.Version!, VersionComparer.Instance),
                                true);
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
        }
    }
}
