using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableTestSetScheduleCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // This completer cannot be shared because it only shows disabled schedules
    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
            var depth = ResolveDepth(fakeBoundParameters);

            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // Exclude Names already selected by parameter from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.TestSetSchedules.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var entity in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(e => wp.IsMatch(e.Name!))
                    .ExcludeByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    string tiphelp = TipHelp(entity);
                    yield return new CompletionResult(PathTools.EscapePSText(entity.Name), entity.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} TestSetSchedule";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var schedules = drive.TestSetSchedules.Get(folder);

                foreach (var schedule in schedules
                    .FilterByWildcards(ts => ts?.Name, wpName)
                    .OrderBy(ts => ts.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var target = schedule.GetPSPath();

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, Enable.Value, [schedule.Id ?? 0]);
                            drive.TestSetSchedules.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}TestSetScheduleError";
                            WriteError(new ErrorRecord(new OrchException(target, ex), errorId, ErrorCategory.InvalidOperation, schedule));
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
