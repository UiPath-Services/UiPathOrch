using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    // Forbidden になってしまうので一旦ボツ。
    [Cmdlet(VerbsCommon.Get, "TmRole")]
    [OutputType(typeof(Entities.TmRole))]
    class GetTmRoleCommand : OrchestratorPSCmdlet
    {
        //[Parameter(Position = 0)]
        //[ArgumentCompleter(typeof(TmProjectNameCompleter))]
        //[SupportsWildcards]
        //public string[]? Name { get; set; }

        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TmDriveCompleter<Positional.Path>))]
        public string[]? Path { get; set; }

        private class TmProjectNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drives = OrchTmDriveInfo.EnumOrchDrives(paramPath);

                // パラメータで選択済みの Name は、候補から除外する
                var wpname = CreateWPListFromParameter(commandAst, "name", Positional.Path.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetTmProjects());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(e => wp.IsMatch(e?.name))
                        .ExcludeByWildcards(e => e?.name!, wpname)
                        .OrderBy(e => e?.name))
                    {
                        string tooltip = e.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(e.name), e.name, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchTmDriveInfo.EnumOrchDrives(Path);
            //var wpProjectName = Name.ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                WriteObject(drive.OrchAPISession.GetTmRoles(), true);
            }
        }
    }
}
