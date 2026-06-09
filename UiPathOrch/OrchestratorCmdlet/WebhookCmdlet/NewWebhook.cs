using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// New-OrchWebhook -- wraps POST /odata/Webhooks.
//
// Parameter surface mirrors Update-OrchWebhook so the same CSV columns
// emitted by `Get-OrchWebhook -ExportCsv` re-import cleanly into either
// New- (creation) or Update- (modification) cmdlet.
//
// Subscriptions: the cmdlet defaults to SubscribeToAllEvents=true when
// neither -SubscribeToAllEvents nor a specific event list is supplied,
// matching the in-product UI's "create a webhook subscribed to
// everything" default. To subscribe to a curated subset, pass -Events
// (wildcards supported); it is sent directly on the POST body as the
// server expects (Events:[{ "EventType": ... }], verified on yotsuda
// 2026-05-22) and flips SubscribeToAllEvents to false.
[Cmdlet(VerbsCommon.New, "OrchWebhook", SupportsShouldProcess = true)]
[OutputType(typeof(Webhook))]
public class NewWebhookCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Url { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

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

    // Specific event subscriptions. Wildcards are expanded against the live
    // event-type list (-Events task.*,job.completed). A ';'-joined value from
    // a CSV cell is accepted too. Supplying -Events implies
    // SubscribeToAllEvents=false unless that flag is set explicitly.
    // 'new' is intentional: this parameter hides the rarely-used
    // PSCmdlet.Events (PSEventManager) property. The cmdlet never touches the
    // base eventing API, and PowerShell's own framework reaches it through the
    // PSCmdlet type, so the hide is harmless — 'new' just documents it and
    // silences CS0108.
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
        bool eventsBound = MyInvocation.BoundParameters.ContainsKey(nameof(Events));
        bool subscribeAllBound = MyInvocation.BoundParameters.ContainsKey(nameof(SubscribeToAllEvents));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            // -Events doesn't vary by name; resolve once per drive.
            var resolvedEvents = WebhookEventResolver.Resolve(this, drive, Events);

            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = drive.NameColonSeparator + name;

                var newWebhook = new Webhook
                {
                    Name = WildcardPattern.Unescape(name),
                    Url = Url,
                };

                newWebhook.AssignStringIfNotNullOrEmpty(Description, (w, v) => w.Description = v);
                newWebhook.AssignStringIfNotNullOrEmpty(Secret, (w, v) => w.Secret = v);
                newWebhook.AssignBoolIfNotNull(Enabled, (w, v) => w.Enabled = v);
                newWebhook.AssignBoolIfNotNull(AllowInsecureSsl, (w, v) => w.AllowInsecureSsl = v);
                newWebhook.AssignBoolIfNotNull(SubscribeToAllEvents, (w, v) => w.SubscribeToAllEvents = v);

                // Specific event subscriptions, if supplied. Picking events
                // implies SubscribeToAllEvents=false unless the caller set
                // that flag explicitly.
                if (resolvedEvents is not null)
                {
                    newWebhook.Events = resolvedEvents;
                    if (!subscribeAllBound)
                    {
                        newWebhook.SubscribeToAllEvents = false;
                    }
                }

                // Default subscription: when neither flag nor an event list
                // was set, fall back to "all events" so the new webhook
                // actually fires on something.
                newWebhook.SubscribeToAllEvents ??= true;
                // The POST body always carries Events (empty when subscribing
                // to all). Observed on POST /odata/Webhooks (yotsuda).
                newWebhook.Events ??= System.Array.Empty<WebhookEvent>();

                if (ShouldProcess(target, "New Webhook"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateWebhook(newWebhook);
                        drive.Webhooks.ClearCache();
                        if (created is not null)
                        {
                            created.Path = drive.NameColonSeparator;
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewWebhookError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
