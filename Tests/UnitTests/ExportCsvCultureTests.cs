using System.Globalization;
using System.Threading;
using UiPath.PowerShell.Commands;
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
}
