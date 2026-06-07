using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare webhooks between two Orchestrator instances. Webhooks are tenant-level, so the
// reference (-Path) and difference (-DifferencePath) are drives, not folders. Matches by Name
// and compares the endpoint, enablement, and subscribed event set (order-independent). The
// signing Secret is sensitive and is not compared.
[Cmdlet(VerbsData.Compare, "OrchWebhook")]
[OutputType(typeof(OrchComparison))]
public class CompareWebhookCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    public string? DifferenceName { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<Webhook, object?> Get)[] Comparators =
    [
        ("Description", w => w.Description),
        ("Url", w => w.Url),
        ("Enabled", w => w.Enabled),
        ("SubscribeToAllEvents", w => w.SubscribeToAllEvents),
        ("AllowInsecureSsl", w => w.AllowInsecureSsl),
        ("Events", w => NormalizeEvents(w.Events)),
    ];

    internal static readonly HashSet<string> ValidPropertyNames =
        new(Comparators.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

    protected override IEnumerable<string> GetTargetDriveNames()
    {
        foreach (var n in base.GetTargetDriveNames()) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("DifferencePath", out var dp))
            foreach (var n in ExtractDriveNamesFromBoundPath(dp)) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("LiteralPath", out var lp))
            foreach (var n in ExtractDriveNamesFromBoundPath(lp)) yield return n;
    }

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        TenantCompare.Run<Webhook>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            IncludeEqual.IsPresent,
            only,
            drive => drive.Webhooks.Get(),
            w => w?.Name,
            Comparators,
            "GetWebhookError",
            WriteObject,
            WriteError);
    }

    // Order-independent normalized form of the subscribed events: sorted EventType set.
    internal static string? NormalizeEvents(WebhookEvent[]? events)
        => events is null || events.Length == 0
            ? null
            : string.Join(";", events.Select(e => e.EventType).Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s, StringComparer.Ordinal));
}
