using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using Xunit;

namespace UnitTests;

// Regression reproductions for bugs fixed after v1.9.1. Every test here is written ONLY against APIs
// that already exist in v1.9.1 (AddTimeRange, DateTimeArrayJsonConverter, MemberConverter), so the
// file compiles on the v1.9.1 source too -- where each test FAILS (reproduces the bug) -- and on HEAD,
// where each PASSES (fix confirmed). (Fixes that live behind new APIs, the provider, the pipeline
// binder, or live HTTP are not unit-reproducible and are covered by their own HEAD-only tests.)
public class RegressionRepro192Tests
{
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

    // Culture class (ed004545): OData $filter timestamps were built with an interpolated custom format
    // whose ':' is the locale TimeSeparator. Under fi-FI ('.') v1.9.1 emits "...12.00.00..." -> the
    // server rejects the filter; HEAD routes through the invariant ToODataUtc.
    [Fact]
    public void AddTimeRange_UsesInvariantTimeSeparator()
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

    // Same culture class via the Calendar excluded-dates JSON body (DateTimeArrayJsonConverter.Write):
    // v1.9.1 used ToString without InvariantCulture, so fi-FI corrupts the written dates.
    [Fact]
    public void DateTimeArrayConverterWrite_UsesInvariantTimeSeparator()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new DateTimeArrayJsonConverter());
        var dt = new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
        var json = WithCulture("fi-FI", () => JsonSerializer.Serialize(new[] { dt }, opts));
        Assert.Equal("[\"2026-05-30T12:00:00.000Z\"]", json);
    }

    // MemberConverter robustness (da4687a6): v1.9.1 hard-indexed objectType and threw on an unknown /
    // missing value, aborting the whole Get-PmGroup read. HEAD reads it with TryGetProperty and skips
    // an unrecognized member (returns null) instead of throwing.
    [Theory]
    [InlineData("{\"objectType\":\"BrandNewKind\",\"id\":\"x\"}")] // unknown objectType
    [InlineData("{\"id\":\"x\"}")]                                  // objectType absent
    public void MemberConverter_DoesNotThrowOnUnknownOrMissingObjectType(string json)
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new MemberConverter());
        var ex = Record.Exception(() => JsonSerializer.Deserialize<PmGroupMember>(json, opts));
        Assert.Null(ex);
    }
}
