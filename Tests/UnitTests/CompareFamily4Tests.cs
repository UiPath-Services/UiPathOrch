using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Unit tests for the fourth wave of Compare-Orch* cmdlets (TestSet, TestDataQueue,
// TestSetSchedule, ActionCatalog): each cmdlet's comparable-property set. These are
// folder-scoped FolderCompare drop-ins with no new normalizers (tags reuse the shared
// EntityComparison.NormalizeTags, covered elsewhere); mode dispatch is the shared engine.

public class CompareFamily4MetadataTests
{
    [Fact]
    public void TestSetValidPropertyNames()
    {
        foreach (var name in new[] { "Description", "SourceType", "Enabled", "EnableCoverage", "TestCaseCount" })
            Assert.Contains(name, CompareTestSetCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void TestDataQueueValidPropertyNames()
    {
        Assert.Contains("Description", CompareTestDataQueueCmdlet.ValidPropertyNames);
        Assert.Contains("ContentJsonSchema", CompareTestDataQueueCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void TestSetScheduleValidPropertyNames()
    {
        foreach (var name in new[] { "Enabled", "TestSetName", "CronExpression", "TimeZoneId", "CalendarName" })
            Assert.Contains(name, CompareTestSetScheduleCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ActionCatalogValidPropertyNames()
    {
        foreach (var name in new[] { "Description", "Encrypted", "RetentionAction", "RetentionPeriod", "RetentionBucketName", "Tags" })
            Assert.Contains(name, CompareActionCatalogCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ValidPropertyNames_AreCaseInsensitive()
    {
        Assert.Contains("contentjsonschema", CompareTestDataQueueCmdlet.ValidPropertyNames);
        Assert.Contains("CRONEXPRESSION", CompareTestSetScheduleCmdlet.ValidPropertyNames);
    }
}
