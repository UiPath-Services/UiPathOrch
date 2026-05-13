using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchWebhook")]
[OutputType(typeof(Entities.Webhook))]
public class GetWebhookCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
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

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.Webhooks.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var webhooks = result.GetResult(cancelHandler.Token);
                if (webhooks is null) continue;

                WriteObject(webhooks
                    .FilterByWildcards(c => c?.Name, wpName)
                    .OrderBy(c => c.Name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetWebhookError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
