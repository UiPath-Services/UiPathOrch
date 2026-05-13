using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchWebhook", SupportsShouldProcess = true)]
public class RemoveWebhookCmdlet : RemoveDriveEntityCmdletBase<Webhook>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Webhook";
    protected override Func<Webhook?, string?> GetName => w => w?.Name;
    protected override Func<Webhook, string> GetPSPath => w => w.GetPSPath();

    protected override IEnumerable<Webhook> GetEntities(OrchDriveInfo drive)
        => drive.Webhooks.Get();

    protected override void Remove(OrchDriveInfo drive, Webhook webhook)
    {
        drive.OrchAPISession.RemoveWebhooks(webhook.Id ?? 0);
        drive.Webhooks.ClearCache();
    }
}
