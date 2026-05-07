using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Test, "OrchWebhook", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.WebhookPingResult))]
public class TestWebhookCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            try
            {
                var webhooks = drive.Webhooks.Get();

                foreach (var webhook in webhooks
                    .FilterByWildcards(wh => wh?.Name, wpName)
                    .OrderBy(wh => wh.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(webhook.GetPSPath(), "Send Ping"))
                    {
                        try
                        {
                            var result = drive.OrchAPISession.PingWebhook(webhook.Id ?? 0);
                            if (result is not null)
                            {
                                result.Path = drive.NameColonSeparator;
                                WriteObject(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "TestWebhookError", ErrorCategory.InvalidOperation, webhook));
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
