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
                .OrderBy(e => e.Name)
                .WithProgressBar(this, $"Updating webhooks in {drive.NameColonSeparator}", e => e.Name)
                .WithCancellation(cancelHandler.Token))
            {
                // Build a PATCH payload with only the properties that need updating.
                // Properties left null are excluded from JSON serialization (WhenWritingNull).
                // The change-detection itself is a pure, API-free method (unit-tested per field).
                var patch = new Webhook { Id = webhook.Id };
                bool dirty = ComputeWebhookUpdate(patch, webhook, new WebhookUpdateInputs
                {
                    Description = Description,
                    Url = Url,
                    Secret = Secret,
                    Enabled = Enabled,
                    AllowInsecureSsl = AllowInsecureSsl,
                    SubscribeToAllEvents = SubscribeToAllEvents,
                    ResolvedEvents = resolvedEvents,
                    SubscribeAllBound = subscribeAllBound,
                });

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

    /// <summary>
    /// Pure inputs for <see cref="ComputeWebhookUpdate"/>. The event-type wildcard resolution
    /// (an API round-trip) is done by the cmdlet first and passed in as <see cref="ResolvedEvents"/>,
    /// so change detection is fully testable without a live Orchestrator.
    /// </summary>
    internal sealed class WebhookUpdateInputs
    {
        public string? Description { get; init; }
        public string? Url { get; init; }
        public string? Secret { get; init; }
        public string? Enabled { get; init; }
        public string? AllowInsecureSsl { get; init; }
        public string? SubscribeToAllEvents { get; init; }
        /// <summary>Resolved event set to apply, or null when -Events was not supplied.</summary>
        public WebhookEvent[]? ResolvedEvents { get; init; }
        /// <summary>True when -SubscribeToAllEvents was explicitly bound (suppresses the implied flip).</summary>
        public bool SubscribeAllBound { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> and returns whether anything
    /// actually changed versus <paramref name="source"/> (the current webhook). Only a real
    /// difference flips the result true, so the caller can skip the PATCH when the request is a
    /// no-op. No API access — unit-testable in isolation.
    /// </summary>
    internal static bool ComputeWebhookUpdate(Webhook payload, Webhook source, WebhookUpdateInputs input)
    {
        bool dirty = false;

        dirty |= payload.AssignStringIfNotNull(input.Description, source, w => w.Description, (w, v) => w.Description = v);
        dirty |= payload.AssignStringIfNotNull(input.Url, source, w => w.Url, (w, v) => w.Url = v);
        dirty |= payload.AssignStringIfNotNull(input.Secret, source, w => w.Secret, (w, v) => w.Secret = v);
        dirty |= payload.AssignBoolIfNotNull(input.Enabled, source, w => w.Enabled, (w, v) => w.Enabled = v);
        dirty |= payload.AssignBoolIfNotNull(input.AllowInsecureSsl, source, w => w.AllowInsecureSsl, (w, v) => w.AllowInsecureSsl = v);
        dirty |= payload.AssignBoolIfNotNull(input.SubscribeToAllEvents, source, w => w.SubscribeToAllEvents, (w, v) => w.SubscribeToAllEvents = v);

        // Replace the event set when -Events was supplied, but only when it actually differs from
        // the current one (order-insensitive), so re-sending the same set does not churn the audit
        // log. Picking events implies not-subscribe-to-all unless the flag was set explicitly; that
        // implied flip is diffed too, so a webhook already not subscribed-to-all with an unchanged
        // set stays a no-op.
        if (input.ResolvedEvents is not null)
        {
            if (!OrchStringExtensions.UnorderedEquals(source.Events, input.ResolvedEvents, e => e.EventType ?? ""))
            {
                payload.Events = input.ResolvedEvents;
                dirty = true;
            }
            if (!input.SubscribeAllBound)
            {
                dirty |= payload.AssignBoolIfNotNull("false", source, w => w.SubscribeToAllEvents, (w, v) => w.SubscribeToAllEvents = v);
            }
        }

        return dirty;
    }
}
