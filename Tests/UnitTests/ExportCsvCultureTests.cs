using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// EscapeCsvValue(DateTime?)/(bool?) must emit culture-independent text so exported
// CSVs round-trip across hosts with different locales. Each test runs under a
// foreign culture (de-DE / tr-TR) to prove the output doesn't follow the host.
public class ExportCsvCultureTests
{
    // Run on a dedicated thread so the culture mutation is isolated and never
    // bleeds into other tests xUnit runs in parallel on the thread pool.
    private static T WithCulture<T>(string culture, Func<T> f)
    {
        T result = default!;
        Exception? captured = null;
        var t = new Thread(() =>
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            try { result = f(); }
            catch (Exception ex) { captured = ex; }
        });
        t.Start();
        t.Join();
        if (captured is not null) throw captured;
        return result;
    }

    [Fact]
    public void DateTime_Utc_IsInvariantIsoWithZ()
    {
        var dt = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
        var s = WithCulture("de-DE", () => OrchestratorPSCmdlet.EscapeCsvValue((DateTime?)dt));
        Assert.Equal("2026-05-30T12:00:00Z", s);
    }

    [Fact]
    public void DateTime_Local_IsInvariantIso_NotLocaleFormat()
    {
        var dt = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Local);
        var s = WithCulture("de-DE", () => OrchestratorPSCmdlet.EscapeCsvValue((DateTime?)dt));
        // Invariant ISO; a German-locale ToString() would yield "30.05.2026 12:00:00".
        Assert.Equal("2026-05-30T12:00:00", s);
    }

    [Fact]
    public void DateTime_DateOnly_Unspecified_IsInvariantIso()
    {
        // The shape Get-OrchCalendarDate -ExportCsv emits: an excluded date is midnight with
        // Kind=Unspecified. It must export as invariant ISO (no Z) so it round-trips across locales.
        // The export previously used the current-culture ToShortDateString() and corrupted / swapped
        // day-month cross-host; it now routes through this EscapeCsvValue(DateTime?) overload.
        var dt = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Unspecified);
        var s = WithCulture("de-DE", () => OrchestratorPSCmdlet.EscapeCsvValue((DateTime?)dt));
        Assert.Equal("2026-06-09T00:00:00", s);
    }

    [Fact]
    public void DateTime_Null_IsEmpty()
    {
        Assert.Equal("", OrchestratorPSCmdlet.EscapeCsvValue((DateTime?)null));
    }

    [Fact]
    public void Bool_IsInvariantUpper()
    {
        // tr-TR is the classic ToUpper() trap; "True"/"False" have no 'i' so the
        // result must be the plain ASCII upper either way.
        Assert.Equal("TRUE", WithCulture("tr-TR", () => OrchestratorPSCmdlet.EscapeCsvValue((bool?)true)));
        Assert.Equal("FALSE", WithCulture("tr-TR", () => OrchestratorPSCmdlet.EscapeCsvValue((bool?)false)));
    }

    [Fact]
    public void ToODataUtc_IsInvariantIso_NotLocaleTimeSeparator()
    {
        // Every OData $filter timestamp (Get-OrchJob/Log/AuditLog/Alert/TestSet/TestCaseExecution and
        // AddTimeRange) routes through ToODataUtc and must use ':' regardless of host locale. fi-FI
        // uses '.' as its time separator, so a CurrentCulture format would yield "12.00.00".
        var dt = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal("2026-05-30T12:00:00.000Z", WithCulture("fi-FI", () => dt.ToODataUtc()));
    }

    [Fact]
    public void AddTimeRange_EmitsInvariantTimestamps()
    {
        var after = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
        var filter = WithCulture("fi-FI", () =>
        {
            var f = new List<string>();
            f.AddTimeRange("CreationTime", after, null);
            return f;
        });
        Assert.Equal("(CreationTime ge 2026-05-30T12:00:00.000Z)", filter[0]);
    }
}
