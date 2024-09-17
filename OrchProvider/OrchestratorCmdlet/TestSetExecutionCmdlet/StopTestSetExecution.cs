using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Stop, "OrchTestSetExecution", SupportsShouldProcess = true)]
    public class StopTestExecutionCommand : OrchestratorPSCmdlet
    {
        private static readonly string[] stoppableStatus = ["Pending", "Running"];

        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? Id { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        private class IdCompleter : OrchArgumentCompleter
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
                var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath);

                // パラメータで選択済みの Id は、候補から除外する
                var paramId = GetParameterValues(commandAst, "Id", Positional.Id.Parameters, wordToComplete).Select(id => long.Parse(id));

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df =>
                {
                    return df.drive.GetTestSetExecutions(df.folder, "&$filter=(((Status%20eq%20%270%27)%20or%20(Status%20eq%20%271%27)))");
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var te in entities!
                        .Where(te => stoppableStatus.Contains(te.Status))
                        .Where(te => wp.IsMatch(te.Id.ToString()))
                        .ExcludeByStructValues(te => te.Id ?? 0, paramId))
                    {
                        string tiphelp = $"{te.Id}  {te.Name!}";
                        if (!string.IsNullOrEmpty(te?.TestSet?.Description))
                            tiphelp += $" ({te?.TestSet?.Description})  ";
                        tiphelp += $"  StartTime: {te!.StartTime}  Status: {te.Status}";
                        yield return new CompletionResult((te!.Id ?? 0).ToString(), (te.Id ?? 0).ToString(), CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                foreach (var id in Id!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = System.IO.Path.Combine(folder.GetPSPath(), id.ToString());
                    if (ShouldProcess(target, "Stop TestSetExecution"))
                    {
                        try
                        {
                            drive.OrchAPISession.CancelTestSetExecutions(folder.Id ?? 0, id);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "StopTestSetError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
        }
    }
}
