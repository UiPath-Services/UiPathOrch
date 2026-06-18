using UiPath.OrchAPI;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the version-strip helpers extracted from OrchAPISession's CreateQueue / EditQueue /
// PutQueueDefinition / PostRelease / PatchRelease.
//
// The strips MUTATE THE PASSED DTO IN PLACE — the module-wide contract is that callers hand them
// a freshly-built or DeepCopy'd object (see CopyRole/CopyUser/UpdateQueue). These tests assert
// correctness: fields absent from the target ApiVersion's DTO are nulled (sending them triggers
// strict-deserialization HTTP 400).
public class QueueReleaseStripTests
{
    private static QueueDefinition FullQueue() => new()
    {
        Id = 42,
        Name = "Q",
        RetryAbandonedItems = true,
        StaleRetentionAction = "Delete",
        StaleRetentionPeriod = 30,
        StaleRetentionBucketId = 7,
        StaleRetentionBucketName = "bucket",
    };

    [Fact]
    public void StripQueue_v15_nulls_stale_retention_and_retry_abandoned()
    {
        var q = FullQueue();
        OrchAPISession.StripQueueFieldsForApiVersion(q, 15.0);
        Assert.Null(q.StaleRetentionAction);
        Assert.Null(q.StaleRetentionPeriod);
        Assert.Null(q.StaleRetentionBucketId);
        Assert.Null(q.StaleRetentionBucketName);
        Assert.Null(q.RetryAbandonedItems);
        Assert.Equal("Q", q.Name); // unrelated fields preserved
    }

    [Fact]
    public void StripQueue_v18_keeps_retry_abandoned_but_nulls_stale_retention()
    {
        var q = FullQueue();
        OrchAPISession.StripQueueFieldsForApiVersion(q, 18.0);
        Assert.True(q.RetryAbandonedItems);   // RetryAbandonedItems exists at >= 18
        Assert.Null(q.StaleRetentionAction);  // StaleRetention still absent below 19
    }

    [Fact]
    public void StripQueue_v19_keeps_everything()
    {
        var q = FullQueue();
        OrchAPISession.StripQueueFieldsForApiVersion(q, 19.0);
        Assert.True(q.RetryAbandonedItems);
        Assert.Equal("Delete", q.StaleRetentionAction);
        Assert.Equal(30, q.StaleRetentionPeriod);
    }

    [Fact]
    public void StripQueue_unknown_version_strips_nothing()
    {
        // null ApiVersion => Below(...) is false for every floor => no stripping (legacy path).
        var q = FullQueue();
        OrchAPISession.StripQueueFieldsForApiVersion(q, null);
        Assert.Equal("Delete", q.StaleRetentionAction);
        Assert.True(q.RetryAbandonedItems);
    }

    private static Release FullRelease() => new()
    {
        Id = 99,
        Name = "R",
        EnvironmentVariables = "env",
        MinRequiredRobotVersion = "1.0",
        FolderKey = "fk",
        StaleRetentionAction = "Delete",
        StaleRetentionPeriod = 30,
        StaleRetentionBucketId = 7,
        HiddenForAttendedUser = true,
        EntryPointPath = "Main.xaml",
        RemoteControlAccess = "Allow",
        AutomationHubIdeaUrl = "url",
        RobotSize = "Small",
        VideoRecordingSettings = new VideoRecordingSettings { VideoRecordingType = "All" },
        ProcessSettings = new ProcessSettings { AutopilotForRobots = new AutopilotForRobotsSettings() },
    };

    [Fact]
    public void StripRelease_v15_nulls_v16_v17_v19_fields()
    {
        var r = FullRelease();
        OrchAPISession.StripReleaseFieldsForApiVersion(r, 15.0);
        // v19 fields
        Assert.Null(r.EnvironmentVariables);
        Assert.Null(r.MinRequiredRobotVersion);
        Assert.Null(r.FolderKey);
        Assert.Null(r.StaleRetentionAction);
        Assert.Null(r.ProcessSettings!.AutopilotForRobots);
        // v17 fields
        Assert.Null(r.HiddenForAttendedUser);
        Assert.Null(r.EntryPointPath);
        // v16 fields
        Assert.Null(r.RemoteControlAccess);
        Assert.Null(r.VideoRecordingSettings);
        Assert.Null(r.AutomationHubIdeaUrl);
        Assert.Null(r.RobotSize);
    }

    [Fact]
    public void StripRelease_v19_keeps_everything()
    {
        var r = FullRelease();
        OrchAPISession.StripReleaseFieldsForApiVersion(r, 19.0);
        Assert.Equal("env", r.EnvironmentVariables);
        Assert.Equal("Delete", r.StaleRetentionAction);
        Assert.NotNull(r.ProcessSettings!.AutopilotForRobots);
    }
}
