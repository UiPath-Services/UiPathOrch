using System.Globalization;
using System.Text.Json;
using System.Threading;
using UiPath.PowerShell.Entities.JsonConverter;
using Xunit;

namespace UnitTests;

// Orchestrator JSON is always invariant (ISO-8601 dates, plain integer strings).
// StringOrIntConverter / DateTimeArrayJsonConverter must parse it the same under
// any host culture; each test runs under de-DE to prove independence.
public class JsonConverterCultureTests
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
    public void StringOrInt_ParsesStringTokenUnderForeignCulture()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new StringOrIntConverter());
        var v = WithCulture("de-DE", () => JsonSerializer.Deserialize<int>("\"1920\"", opts));
        Assert.Equal(1920, v);
    }

    [Fact]
    public void StringOrInt_ParsesNumberToken()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new StringOrIntConverter());
        Assert.Equal(1080, JsonSerializer.Deserialize<int>("1080", opts));
    }

    [Fact]
    public void DateTimeArray_ParsesIsoUnderForeignCulture()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new DateTimeArrayJsonConverter());
        var arr = WithCulture("de-DE", () => JsonSerializer.Deserialize<DateTime[]>("[\"2026-05-30T12:00:00Z\"]", opts));
        Assert.NotNull(arr);
        Assert.Single(arr!);
        Assert.Equal(new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc), arr![0].ToUniversalTime());
    }

    [Fact]
    public void DateTimeArray_WritesInvariantIsoUnderForeignCulture()
    {
        // The Write path (Calendar excluded dates -> API JSON body) previously used a
        // CurrentCulture ToString; fi-FI's '.' time separator would have yielded "12.00.00".
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new DateTimeArrayJsonConverter());
        var dt = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
        var json = WithCulture("fi-FI", () => JsonSerializer.Serialize(new[] { dt }, opts));
        Assert.Equal("[\"2026-05-30T12:00:00.000Z\"]", json);
    }
}
