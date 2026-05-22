using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shared by New-/Update-OrchWebhook to turn the user's -Events patterns into
// the WebhookEvent[] the server expects ({ "EventType": "task.created" }).
//
// -Events is string[] with wildcard support, so "-Events task.*,job.completed"
// expands against the live event-type list (Get-OrchWebhookEventType /
// /odata/Webhooks/...GetEventTypes). A single CSV cell that joins several
// values with ';' is accepted too, so Get-OrchWebhook -ExportCsv round-trips.
// Matching is done against the server list (not a hard-coded enum) so new
// event types are picked up automatically; an EventType wire value equals the
// WebhookEventType.Name (verified on yotsuda 2026-05-22).
internal static class WebhookEventResolver
{
    // Returns null when no patterns were supplied (caller keeps its existing
    // SubscribeToAllEvents behaviour); otherwise the deduplicated set of
    // matching events, in the server's listing order. Patterns that match
    // nothing produce a warning but don't fail the call.
    internal static WebhookEvent[]? Resolve(OrchestratorPSCmdlet cmdlet, OrchDriveInfo drive, string[]? events)
    {
        if (events is null || events.Length == 0) return null;

        // Flatten: split each element on ';' so multiple -Events args and a
        // single ';'-joined CSV cell both yield one pattern per value.
        var rawPatterns = events
            .SelectMany(e => (e ?? string.Empty)
                .Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
            .ToList();

        var patterns = rawPatterns
            .Select(p => new WildcardPattern(PathTools.UnescapePSText(p), WildcardOptions.IgnoreCase))
            .ToList();

        var validNames = drive.WebhookEventTypes.Get()
            .Select(t => t.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        // Warn for any pattern that matched no known event type.
        for (int i = 0; i < patterns.Count; i++)
        {
            if (!validNames.Any(n => patterns[i].IsMatch(n!)))
            {
                cmdlet.WriteWarning(
                    $"No webhook event type matches '{rawPatterns[i]}'. " +
                    "Use Get-OrchWebhookEventType to list valid values.");
            }
        }

        return validNames
            .Where(n => patterns.Any(p => p.IsMatch(n!)))
            .Select(n => new WebhookEvent { EventType = n })
            .ToArray();
    }
}
