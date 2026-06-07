using System.Globalization;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare calendars between two Orchestrator instances. Calendars are tenant-level, so the
// reference (-Path) and difference (-DifferencePath) are drives, not folders. Matches by Name
// and compares the time zone and the set of excluded dates (order-independent).
[Cmdlet(VerbsData.Compare, "OrchCalendar")]
[OutputType(typeof(OrchComparison))]
public class CompareCalendarCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    public string? DifferenceName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<ExtendedCalendar, object?> Get)[] Comparators =
    [
        ("TimeZoneId", c => c.TimeZoneId),
        ("ExcludedDates", c => NormalizeDates(c.ExcludedDates)),
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

        TenantCompare.Run<ExtendedCalendar>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            IncludeEqual.IsPresent,
            only,
            // The list accessor (drive.Calendars.Get()) returns only Name/Id -- TimeZoneId and
            // ExcludedDates are populated only by the per-calendar detail fetch, so enrich each
            // calendar with CalendarsDetailed before comparing.
            drive => drive.Calendars.Get()
                .Where(c => c?.Id is not null)
                .Select(c => drive.CalendarsDetailed.Get(c!.Id!.Value))
                .OfType<ExtendedCalendar>(),
            c => c?.Name,
            Comparators,
            "GetCalendarError",
            WriteObject,
            WriteError);
    }

    // Order-independent normalized form of the excluded dates: sorted yyyy-MM-dd set.
    internal static string? NormalizeDates(DateTime[]? dates)
        => dates is null || dates.Length == 0
            ? null
            : string.Join(";", dates.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).OrderBy(s => s, StringComparer.Ordinal));
}
