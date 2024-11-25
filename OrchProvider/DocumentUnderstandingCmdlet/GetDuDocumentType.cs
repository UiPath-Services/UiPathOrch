using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "DuDocumentType")]
    [OutputType(typeof(Entities.DuDocumentType))]
    public class GetDuDocumentTypeCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DocumentTypeNameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        private class DocumentTypeNameCompleter : OrchArgumentCompleter
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

                // パラメータで選択済みの DocumentTypeName は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuDocumentTypes(dp.project));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var valueType in entities!
                        .Where(e => wp.IsMatch(e?.name))
                        .ExcludeByWildcards(e => e?.name!, wpName)
                        .OrderBy(e => e?.name))
                    {
                        string tooltip = valueType.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(valueType.name), valueType.name, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
            var wpName = Name.ConvertToWildcardPatternList();

            // 同期バージョン
            //foreach (var driveProject in drivesProjects)
            //{
            //    var (drive, project) = driveProject;

            //    WriteObject(drive.GetDuDocumentTypes(project)?
            //        .FilterByWildcards(u => u.name!, wpDocumentTypeName)
            //        .OrderBy(e => e.name),
            //        true);
            //}

            // 非同期バージョン
            using var results = OrchThreadPool.RunForEach(drivesProjects,
                dp => dp.project.GetPSPath(),
                dp => dp.project,
                dp => dp.drive.GetDuDocumentTypes(dp.project));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities= result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities!
                        .FilterByWildcards(u => u?.name, wpName)
                        .OrderBy(e => e.name),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetDuDocumentTypeError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
