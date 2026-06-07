using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for the second wave of Compare-Orch* cmdlets (Trigger, Bucket, Machine,
// CredentialStore, Calendar, Webhook): the genuinely-new logic (date/event normalizers) and
// each cmdlet's comparable-property set. Mode dispatch / "<=" / "=>" enumeration is the shared
// FolderCompare / TenantCompare engine, validated live.

public class NormalizeDatesTests
{
    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CompareCalendarCmdlet.NormalizeDates(null));
        Assert.Null(CompareCalendarCmdlet.NormalizeDates([]));
    }

    [Fact]
    public void IsOrderIndependent()
    {
        var a = CompareCalendarCmdlet.NormalizeDates([new DateTime(2026, 1, 2), new DateTime(2026, 3, 4)]);
        var b = CompareCalendarCmdlet.NormalizeDates([new DateTime(2026, 3, 4), new DateTime(2026, 1, 2)]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DistinguishesDifferentDateSets()
    {
        var a = CompareCalendarCmdlet.NormalizeDates([new DateTime(2026, 1, 2)]);
        var b = CompareCalendarCmdlet.NormalizeDates([new DateTime(2026, 1, 3)]);
        Assert.NotEqual(a, b);
    }
}

public class NormalizeEventsTests
{
    [Fact]
    public void NullOrEmpty_ReturnsNull()
    {
        Assert.Null(CompareWebhookCmdlet.NormalizeEvents(null));
        Assert.Null(CompareWebhookCmdlet.NormalizeEvents([]));
    }

    [Fact]
    public void IsOrderIndependent()
    {
        var a = CompareWebhookCmdlet.NormalizeEvents([new() { EventType = "job.completed" }, new() { EventType = "job.faulted" }]);
        var b = CompareWebhookCmdlet.NormalizeEvents([new() { EventType = "job.faulted" }, new() { EventType = "job.completed" }]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DistinguishesDifferentEventSets()
    {
        var a = CompareWebhookCmdlet.NormalizeEvents([new() { EventType = "job.completed" }]);
        var b = CompareWebhookCmdlet.NormalizeEvents([new() { EventType = "job.completed" }, new() { EventType = "job.faulted" }]);
        Assert.NotEqual(a, b);
    }
}

public class CompareFamily2MetadataTests
{
    [Fact]
    public void TriggerValidPropertyNames_IncludeScheduleFields()
    {
        foreach (var name in new[] { "StartProcessCron", "Enabled", "ReleaseName", "TimeZoneId", "Tags" })
            Assert.Contains(name, CompareTriggerCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void BucketValidPropertyNames_IncludeStorageFields()
    {
        foreach (var name in new[] { "StorageProvider", "StorageContainer", "Options", "Tags" })
            Assert.Contains(name, CompareBucketCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void MachineValidPropertyNames_IncludeSlots()
    {
        foreach (var name in new[] { "Type", "UnattendedSlots", "HeadlessSlots", "TargetFramework" })
            Assert.Contains(name, CompareMachineCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void CredentialStoreValidPropertyNames_IncludeTypeAndConfig()
    {
        foreach (var name in new[] { "Type", "ProxyType", "HostName", "AdditionalConfiguration" })
            Assert.Contains(name, CompareCredentialStoreCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void CalendarValidPropertyNames_IncludeTimeZoneAndDates()
    {
        Assert.Contains("TimeZoneId", CompareCalendarCmdlet.ValidPropertyNames);
        Assert.Contains("ExcludedDates", CompareCalendarCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void WebhookValidPropertyNames_IncludeUrlAndEvents()
    {
        foreach (var name in new[] { "Url", "Enabled", "SubscribeToAllEvents", "Events" })
            Assert.Contains(name, CompareWebhookCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ValidPropertyNames_AreCaseInsensitive()
    {
        Assert.Contains("startprocesscron", CompareTriggerCmdlet.ValidPropertyNames);
        Assert.Contains("EVENTS", CompareWebhookCmdlet.ValidPropertyNames);
    }
}
