using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchWebhook", SupportsShouldProcess = true)]
public class UpdateWebhookCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Url { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Secret { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? AllowInsecureSsl { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? SubscribeToAllEvents { get; set; }

    // Replace the event subscription set. Wildcards expand against the live
    // event-type list (-Events task.*,job.completed); a ';'-joined CSV cell
    // is accepted too. Supplying -Events implies SubscribeToAllEvents=false
    // unless that flag is set explicitly.
    // 'new' is intentional — hides the unused PSCmdlet.Events property.
    // See New-OrchWebhook for the full rationale.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(WebhookEventTypeNameCompleter))]
    [WebhookEventArgumentTransformation]
    public new string[]? Events { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();
        bool subscribeAllBound = MyInvocation.BoundParameters.ContainsKey(nameof(SubscribeToAllEvents));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            // -Events doesn't vary by matched webhook; resolve once per drive.
            var resolvedEvents = WebhookEventResolver.Resolve(this, drive, Events);

            ICollection<Webhook>? webhooks;
            try
            {
                webhooks = drive.Webhooks.Get();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var webhook in webhooks
                .FilterByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name).WithCancellation(cancelHandler.Token))
            {
                // Build a PATCH payload with only the properties that need updating.
                // Properties left null are excluded from JSON serialization (WhenWritingNull).
                var patch = new Webhook { Id = webhook.Id };
                bool dirty = false;

                dirty |= patch.AssignStringIfNotNull(Description, webhook, w => w.Description, (w, v) => w.Description = v);
                dirty |= patch.AssignStringIfNotNull(Url, webhook, w => w.Url, (w, v) => w.Url = v);
                dirty |= patch.AssignStringIfNotNull(Secret, webhook, w => w.Secret, (w, v) => w.Secret = v);
                dirty |= patch.AssignBoolIfNotNull(Enabled, webhook, w => w.Enabled, (w, v) => w.Enabled = v);
                dirty |= patch.AssignBoolIfNotNull(AllowInsecureSsl, webhook, w => w.AllowInsecureSsl, (w, v) => w.AllowInsecureSsl = v);
                dirty |= patch.AssignBoolIfNotNull(SubscribeToAllEvents, webhook, w => w.SubscribeToAllEvents, (w, v) => w.SubscribeToAllEvents = v);

                // Replace the event set when -Events was supplied. The server
                // PATCH replaces Events wholesale, so we always send the
                // resolved set; picking events implies not-subscribe-to-all
                // unless the flag was set explicitly.
                if (resolvedEvents is not null)
                {
                    patch.Events = resolvedEvents;
                    dirty = true;
                    if (!subscribeAllBound)
                    {
                        patch.SubscribeToAllEvents = false;
                    }
                }

                if (!dirty) continue;

                string target = webhook.GetPSPath();
                if (ShouldProcess(target, "Update Webhook"))
                {
                    try
                    {
                        drive.OrchAPISession.PatchWebhook(webhook.Id ?? 0, patch);
                        drive.Webhooks.ClearCache();
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateWebhookError", ErrorCategory.InvalidOperation, webhook));
                    }
                }
            }
        }
    }
}
