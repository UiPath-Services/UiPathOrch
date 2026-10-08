using UiPath.OrchAPI;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchAPISession.StripStartProcessFieldsForApiVersion, which Start-OrchJob runs before the
// StartJobs POST and whose returned names it reports in a warning.
public class StartProcessStripTests
{
    private static StartProcess Full() => new()
    {
        ReleaseKey = "key",
        Strategy = "ModernJobsCount",
        SpecificPriorityValue = 65,
        StopStrategy = "SoftStop",
        StopProcessExpression = "3600",
        KillProcessExpression = "600",
        AlertPendingExpression = "pending",
        AlertRunningExpression = "running",
    };

    [Fact]
    public void Strip_v20_keeps_everything()
    {
        var sp = Full();
        var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 20.0);
        Assert.Empty(dropped);
        Assert.Equal(65, sp.SpecificPriorityValue);
        Assert.Null(sp.JobPriority);
        Assert.Equal("pending", sp.AlertPendingExpression);
        Assert.Equal("running", sp.AlertRunningExpression);
    }

    [Fact]
    public void Strip_v15_drops_alerts_and_names_them()
    {
        var sp = Full();
        var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 15.0);
        Assert.Equal(["AlertPendingExpression", "AlertRunningExpression"], dropped);
        Assert.Null(sp.AlertPendingExpression);
        Assert.Null(sp.AlertRunningExpression);
        // stop / kill are not version-gated
        Assert.Equal("SoftStop", sp.StopStrategy);
        Assert.Equal("3600", sp.StopProcessExpression);
        Assert.Equal("600", sp.KillProcessExpression);
        // 15 is at the SpecificPriorityValue floor (14), so the value stays
        Assert.Equal(65, sp.SpecificPriorityValue);
    }

    [Fact]
    public void Strip_v13_converts_priority_to_bucket_without_reporting_it()
    {
        var sp = Full();
        var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 13.0);
        Assert.DoesNotContain("SpecificPriorityValue", dropped);
        Assert.Null(sp.SpecificPriorityValue);
        Assert.Equal("High", sp.JobPriority);
    }

    [Fact]
    public void Strip_reports_only_fields_that_were_given()
    {
        var sp = new StartProcess { ReleaseKey = "key", AlertRunningExpression = "running" };
        var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 15.0);
        Assert.Equal(["AlertRunningExpression"], dropped);
    }

    [Fact]
    public void Strip_is_idempotent()
    {
        var sp = Full();
        OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 13.0);
        var second = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, 13.0);
        Assert.Empty(second);
        Assert.Equal("High", sp.JobPriority);
    }

    [Fact]
    public void Strip_unknown_version_strips_nothing()
    {
        var sp = Full();
        var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(sp, null);
        Assert.Empty(dropped);
        Assert.Equal("pending", sp.AlertPendingExpression);
        Assert.Equal(65, sp.SpecificPriorityValue);
    }
}
