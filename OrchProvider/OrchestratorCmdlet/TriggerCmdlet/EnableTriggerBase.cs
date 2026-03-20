using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableTriggerCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected Names from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetTriggers(df.folder));

            foreach (var result in results)
            {
                foreach (var trigger in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(t => wp.IsMatch(t.Name))
                    .ExcludeByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name))
                {
                    string tiphelp = TipHelp(trigger);
                    yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} Trigger";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var schedules = drive.GetTriggers(folder);

                foreach (var trigger in schedules
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        :  t.Enabled.GetValueOrDefault())
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(trigger.GetPSPath(), action))
                    {
                        try
                        {
                            drive.OrchAPISession.EnableProcessSchedule(folder.Id ?? 0, [trigger.Id ?? 0], Enable.Value);
                            drive._dicTriggers?.TryRemove(folder.Id ?? 0, out _);
                            drive._dicTriggers_Exceptions.ClearCache();
                            drive._dicTriggersDetailed?.TryRemove(folder.Id ?? 0, out _);
                            drive._dicTriggersDetailed_Exceptions.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}TriggerError";
                            WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, trigger));
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
                continue;
            }
        }
    }

    // Multi-threaded version
    // Rewritten as single-threaded because it could be slower when HTTP calls are capped
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetProcessSchedule(df.folder)
    //    );

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var trigger in entities
    //                .Where(t => !t.Enabled.GetValueOrDefault())
    //                .FilterByWildcards(t => t.Name!, wpName)
    //                .OrderBy(t => t.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(trigger.GetPSPath(), "Enable Trigger"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.EnableProcessSchedule(folder.Id ?? 0, [trigger.Id ?? 0], true);
    //                        drive._dicProcessSchedule?.TryRemove(folder.Id ?? 0, out List<ProcessSchedule>? _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableTriggerError", ErrorCategory.InvalidOperation, trigger));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetTriggerError", ErrorCategory.InvalidOperation, ex.Target));
    //            continue;
    //        }
    //    }
    //}
}
