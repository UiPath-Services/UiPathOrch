using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "DuClassifier")]
    [OutputType(typeof(Entities.DuClassifier))]
    public class GetDuClassifierCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(ClassifierNameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        private class ClassifierNameCompleter : OrchArgumentCompleter
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
                var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

                // パラメータで選択済みの ClassifierName は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuClassifiers(dp.project));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var classifier in entities!
                        .Where(e => wp.IsMatch(e?.name))
                        .ExcludeByWildcards(e => e?.name!, wpName)
                        .OrderBy(e => e?.name))
                    {
                        string tooltip = classifier.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(classifier.name), classifier.name, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
            var wpClassifierName = Name.ConvertToWildcardPatternList();

            // 同期バージョン
            //foreach (var driveProject in drivesProjects)
            //{
            //    var (drive, project) = driveProject;

            //    WriteObject(drive.GetDuClassifiers(project)?
            //        .FilterByWildcards(u => u.name!, wpClassifierName)
            //        .OrderBy(e => e.name),
            //        true);
            //}

            // 非同期バージョン
            using var results = OrchThreadPool.RunForEach(drivesProjects,
                dp => dp.project.GetPSPath(),
                dp => dp.project,
                dp => dp.drive.GetDuClassifiers(dp.project));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities
                        .FilterByWildcards(u => u?.name, wpClassifierName)
                        .OrderBy(e => e.name),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetDuClassifierError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
