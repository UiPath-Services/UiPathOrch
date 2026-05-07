using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableWebhookCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    // This only enumerates disabled Webhooks, so it cannot be shared.
    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude already-selected Name values from the candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Webhooks.Get());

            foreach (var result in results)
            {
                foreach (var webhook in result
                    .Where(e => Enable.Value
                        ? !e.Enabled.GetValueOrDefault()
                        : e.Enabled.GetValueOrDefault())
                    .Where(e => wp.IsMatch(e.Name))
                    .ExcludeByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name!))
                {
                    string tiphelp = TipHelp(webhook);
                    yield return new CompletionResult(PathTools.EscapePSText(webhook.Name), webhook.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }
    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();
        //var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} Webhook";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var webhooks = drive.Webhooks.Get();

                foreach (var webhook in webhooks
                    .Where(e => Enable.Value
                        ? !e.Enabled.GetValueOrDefault()
                        : e.Enabled.GetValueOrDefault())
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(webhook.GetPSPath(), action))
                    {
                        try
                        {
                            var newWebhook = new Webhook()
                            {
                                Enabled = Enable.Value
                            };
                            drive!.OrchAPISession.PatchWebhook(webhook.Id ?? 0, newWebhook);
                            webhook.Enabled = Enable.Value;
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}WebhookError";
                            WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, webhook));
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
