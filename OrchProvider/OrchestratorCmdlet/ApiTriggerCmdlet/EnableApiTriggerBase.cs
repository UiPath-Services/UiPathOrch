using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableApiTriggerCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // This cannot be shared because it only enumerates disabled API triggers
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
            var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.ApiTriggers.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var trigger in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(t => wp.IsMatch(t.Name))
                    .ExcludeByWildcards(t => t?.Name, wpName))
                {
                    string tooltip = trigger.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} ApiTrigger";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var triggers = drive.ApiTriggers.Get(folder);

                foreach (var trigger in triggers
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(trigger.GetPSPath(), action))
                    {
                        try
                        {
                            drive.OrchAPISession.EnableHttpTriggers(folder.Id ?? 0, [trigger.Id!], Enable.Value);
                            drive.ApiTriggers.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}ApiTriggerError";
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, drive));
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
    //        df => df.drive.GetHttpTriggers(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var triggers = result.GetResult(cancelHandler.Token);
    //            if (triggers is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var trigger in triggers
    //                .Where(t => !t.Enabled.GetValueOrDefault())
    //                .FilterByWildcards(t => t.Name!, wpName)
    //                .OrderBy(t => t.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(trigger.GetPSPath(), "Enable ApiTrigger"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.EnableHttpTriggers(folder.Id ?? 0, new string[] { trigger.Id! }, true);
    //                        drive._dicHttpTriggers?.TryRemove(folder.Id ?? 0, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableApiTriggerError", ErrorCategory.InvalidOperation, trigger));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetApiTriggerError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
