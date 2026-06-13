using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Stop, "OrchTestSetExecution", SupportsShouldProcess = true)]
public class StopTestExecutionCmdlet : OrchestratorPSCmdlet
{
    private static readonly string[] stoppableStatus = ["Pending", "Running"];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath);

            // Exclude Ids already selected by parameter from candidates
            var paramId = GetSelfExclusionValues(commandAst, "Id", wordToComplete).Select(id => long.Parse(id));

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                return df.drive.TestSetExecutions.Fetch(df.folder, "&$filter=(((Status eq '0') or (Status eq '1')))");
            });

            foreach (var result in results)
            {
                foreach (var te in result
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
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var id in Id!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), id.ToString());
                if (ShouldProcess(target, "Stop TestSetExecution"))
                {
                    try
                    {
                        drive.OrchAPISession.CancelTestSetExecutions(folder.Id ?? 0, id);
                        // Surgically drop just this execution's now-stale cache entry (its status moved
                        // to Cancelling/Cancelled) so a later Get-OrchTestSetExecution re-fetches it
                        // fresh, without clearing the rest of the folder's accumulated cache.
                        drive.TestSetExecutions.GetCache(folder)?.TryRemove(id, out _);
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
