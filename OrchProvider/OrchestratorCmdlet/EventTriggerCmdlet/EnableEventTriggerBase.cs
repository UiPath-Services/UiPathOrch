using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableEventTriggerCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // 無効となっている Event trigger だけを列挙するので、これは共通にできない
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

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.EventTriggers.Get(df.folder));

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

        string action = $"{(Enable.Value ? "Enable" : "Disable")} EventTrigger";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var triggers = drive.EventTriggers.Get(folder);

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
                            drive.OrchAPISession.EnableEventTriggers(folder.Id ?? 0, trigger.Id!, Enable.Value);
                            drive.EventTriggers.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}EventTriggerError";
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
}
