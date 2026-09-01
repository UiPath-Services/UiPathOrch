using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Regression tests for three ways Compare-Orch* misreported a correct MSI-to-Automation-Suite
// migration (reported 2026-08-31): the Differences column vanishing on export, and two fields
// that the destination server -- not the caller -- owns being compared as if they were drift.

public class PropertyDifferenceListTests
{
    private static PropertyDifferenceList List(params PropertyDifference[] diffs)
    {
        var l = new PropertyDifferenceList();
        l.AddRange(diffs);
        return l;
    }

    // Export-Csv converts each property with ToString(); a bare List<T> answered with its type
    // name, so the exported Differences column carried none of the diff.
    [Fact]
    public void ToString_RendersTheDiffsNotTheTypeName()
    {
        var l = List(
            new PropertyDifference { Property = "RetryAbandonedItems", ReferenceValue = null, DifferenceValue = "False" },
            new PropertyDifference { Property = "JobPriority", ReferenceValue = "Low", DifferenceValue = null });

        Assert.Equal("RetryAbandonedItems: (null) => 'False'; JobPriority: 'Low' => (null)", l.ToString());
        Assert.DoesNotContain("System.Collections", l.ToString());
    }

    [Fact]
    public void ToString_SingleDiffHasNoSeparator()
    {
        var l = List(new PropertyDifference { Property = "Description", ReferenceValue = "a", DifferenceValue = "b" });
        Assert.Equal("Description: 'a' => 'b'", l.ToString());
    }

    [Fact]
    public void ToString_EmptyIsEmpty() => Assert.Equal("", List().ToString());

    // The diff engine has to hand back the type that renders, or nothing above changes.
    [Fact]
    public void DiffProperties_ReturnsTheRenderingList()
    {
        var diffs = EntityComparison.DiffProperties(
            new QueueDefinition { Description = "a" },
            new QueueDefinition { Description = "b" },
            [("Description", q => q.Description)],
            null);

        Assert.IsType<PropertyDifferenceList>(diffs);
        Assert.Equal("Description: 'a' => 'b'", diffs.ToString());
    }
}

public class ComparisonCsvRowTests
{
    [Fact]
    public void RowMatchesTheHeaderCount()
        => Assert.Equal(CompareOrchCmdlet.CsvHeaders.Length, CompareOrchCmdlet.CsvRow(new OrchComparison()).Length);

    // The columns -ExportCsv writes are the first six `Compare-Orch* | Export-Csv` produced, in
    // the same order, so a script built on that pipeline keeps reading the same file.
    [Fact]
    public void HeadersAreTheReportColumns()
        => Assert.Equal(
            ["SideIndicator", "Name", "DifferenceName", "Path", "DifferencePath", "Differences"],
            CompareOrchCmdlet.CsvHeaders);

    [Fact]
    public void DifferencesColumnCarriesTheDiffText()
    {
        var diffs = new PropertyDifferenceList
        {
            new() { Property = "RetryAbandonedItems", ReferenceValue = null, DifferenceValue = "False" },
        };
        var row = CompareOrchCmdlet.CsvRow(new OrchComparison
        {
            SideIndicator = "<>",
            Name = "QueueHelloWorld",
            DifferenceName = "QueueHelloWorld",
            Path = @"OrchMSI:\FolderTestMigrate1\QueueHelloWorld",
            DifferencePath = @"OrchAS:\FolderTestMigrate1\QueueHelloWorld",
            Differences = diffs,
        });

        Assert.Equal(@"OrchMSI:\FolderTestMigrate1\QueueHelloWorld", row[3]);
        Assert.Equal("RetryAbandonedItems: (null) => 'False'", row[5]);
    }

    // A "<=" / "=>" row has no Differences at all; the column must be empty, not "null".
    [Fact]
    public void OneSidedRowLeavesTheDifferencesColumnEmpty()
        => Assert.Equal("", CompareOrchCmdlet.CsvRow(new OrchComparison { SideIndicator = "<=" })[5]);

    // A diff whose text contains a comma has to be quoted or it would split into extra columns.
    [Fact]
    public void CommaInADiffIsQuoted()
    {
        var diffs = new PropertyDifferenceList
        {
            new() { Property = "Tags", ReferenceValue = "a=1;b=2", DifferenceValue = "x,y" },
        };
        var col = CompareOrchCmdlet.CsvRow(new OrchComparison { Differences = diffs })[5];
        Assert.StartsWith("\"", col, StringComparison.Ordinal);
        Assert.EndsWith("\"", col, StringComparison.Ordinal);
    }

    // Names are written as they read, not wildcard-escaped: this report has no import partner.
    [Fact]
    public void NameIsNotWildcardEscaped()
        => Assert.Equal("Queue[1]", CompareOrchCmdlet.CsvRow(new OrchComparison { Name = "Queue[1]" })[1]);
}

public class EffectiveJobPriorityTests
{
    [Fact]
    public void ExplicitNameWins()
        => Assert.Equal("Low", EntityComparison.EffectiveJobPriority("Low", 65));

    [Fact]
    public void BothAbsentStaysNull()
        => Assert.Null(EntityComparison.EffectiveJobPriority(null, null));

    [Theory]
    [InlineData(5, "Low")]
    [InlineData(25, "Low")]    // the named scale's "Low"
    [InlineData(30, "Low")]
    [InlineData(31, "Normal")]
    [InlineData(50, "Normal")]
    [InlineData(60, "Normal")]
    [InlineData(61, "High")]
    [InlineData(65, "High")]   // the named scale's "High"
    [InlineData(95, "High")]
    public void FallsBackToTheBucketOfTheSpecificValue(int specific, string expected)
        => Assert.Equal(expected, EntityComparison.EffectiveJobPriority(null, specific));
}

public class CompareTriggerComparatorTests
{
    private static Func<ProcessSchedule, object?> Get(string name)
        => CompareTriggerCmdlet.Comparators.Single(c => c.Name == name).Get;

    private static object? Diff(string name, ProcessSchedule reference, ProcessSchedule difference)
    {
        var diffs = EntityComparison.DiffProperties(reference, difference, [(name, Get(name))], null);
        return diffs.Count == 0 ? null : diffs[0];
    }

    // The exact shape reported: source keeps the name, the copied destination reads it back as
    // null while carrying the same specific value, and the web UI shows the destination as Low.
    [Fact]
    public void JobPriority_CopiedTriggerIsNotDrift()
    {
        var src = new ProcessSchedule { JobPriority = "Low", SpecificPriorityValue = 25 };
        var dst = new ProcessSchedule { JobPriority = null, SpecificPriorityValue = 25 };
        Assert.Null(Diff("JobPriority", src, dst));
    }

    [Fact]
    public void JobPriority_RealDriftIsStillReported()
    {
        var src = new ProcessSchedule { JobPriority = "Low", SpecificPriorityValue = 25 };
        var dst = new ProcessSchedule { JobPriority = null, SpecificPriorityValue = 65 };
        Assert.NotNull(Diff("JobPriority", src, dst));
    }

    // SpecificPriorityValue is carried across unchanged, so it keeps catching the drift on its own.
    [Fact]
    public void SpecificPriorityValue_StillCompared()
    {
        var src = new ProcessSchedule { SpecificPriorityValue = 25 };
        var dst = new ProcessSchedule { SpecificPriorityValue = 65 };
        Assert.NotNull(Diff("SpecificPriorityValue", src, dst));
    }

    // A queue trigger's cron is the destination server's, and it jitters: the reported pair.
    [Fact]
    public void StartProcessCron_QueueTriggerIsNotCompared()
    {
        var src = new ProcessSchedule { QueueDefinitionId = 33, StartProcessCron = "0 0/30 * 1/1 * ? *" };
        var dst = new ProcessSchedule { QueueDefinitionId = 71, StartProcessCron = "33 20/30 * * * ? *" };
        Assert.Null(Diff("StartProcessCron", src, dst));
    }

    [Fact]
    public void StartProcessCron_TimeTriggerIsStillCompared()
    {
        var src = new ProcessSchedule { StartProcessCron = "0 0/30 * 1/1 * ? *" };
        var dst = new ProcessSchedule { StartProcessCron = "33 20/30 * * * ? *" };
        Assert.NotNull(Diff("StartProcessCron", src, dst));
    }

    // When is suppressing the cron hiding something? Only when the two values actually differ --
    // an equal pair's "==" withholds nothing, and warning there would be noise on a verification
    // pass over triggers that all match. This is unit-tested rather than live because the equal
    // case cannot be built against a server: Automation Cloud assigns a queue trigger's cron
    // itself, so two created separately never carry the same one.
    [Fact]
    public void QueueTrigger_SameCron_HidesNothing()
        => Assert.False(CompareTriggerCmdlet.HidesACronDifference(
            new ProcessSchedule { QueueDefinitionId = 33, StartProcessCron = "0 0/30 * 1/1 * ? *" },
            new ProcessSchedule { QueueDefinitionId = 71, StartProcessCron = "0 0/30 * 1/1 * ? *" }));

    [Fact]
    public void QueueTrigger_DifferentCron_HidesADifference()
        => Assert.True(CompareTriggerCmdlet.HidesACronDifference(
            new ProcessSchedule { QueueDefinitionId = 33, StartProcessCron = "0 0/30 * 1/1 * ? *" },
            new ProcessSchedule { QueueDefinitionId = 71, StartProcessCron = "33 20/30 * * * ? *" }));

    // A time trigger's cron is compared for real, so nothing is hidden and nothing is announced.
    [Fact]
    public void TimeTrigger_DifferentCron_HidesNothing()
        => Assert.False(CompareTriggerCmdlet.HidesACronDifference(
            new ProcessSchedule { StartProcessCron = "0 0/30 * 1/1 * ? *" },
            new ProcessSchedule { StartProcessCron = "0 0/45 * 1/1 * ? *" }));

    // One side a queue trigger and the other not: the cron is suppressed on the queue side only,
    // so the pair is still worth naming.
    [Fact]
    public void OneSidedQueueTrigger_HidesADifference()
        => Assert.True(CompareTriggerCmdlet.HidesACronDifference(
            new ProcessSchedule { QueueDefinitionId = 33, StartProcessCron = "a" },
            new ProcessSchedule { StartProcessCron = "b" }));

    // How many notices, and how they are counted. None of this can be pinned live: a queue
    // trigger's cron is assigned by the server and cannot be set at create or update, so whether
    // two of them differ depends on when each was created.
    private static ProcessSchedule Qt(string path, string name, string cron)
        => new() { Path = path, Name = name, QueueDefinitionId = 7, StartProcessCron = cron };

    [Fact]
    public void SameNameInMirroredFolders_CountsAsTwo()
    {
        // The migration shape: one trigger name repeated in every folder. Keyed on the name this
        // reported "1 queue trigger" and pointed at one folder (measured against that build).
        var acc = new List<string>();
        CompareTriggerCmdlet.AddCronDiff(acc, Qt(@"Src:\A", "T", "a"), Qt(@"Dst:\A", "T", "b"));
        CompareTriggerCmdlet.AddCronDiff(acc, Qt(@"Src:\A\Sub", "T", "a"), Qt(@"Dst:\A\Sub", "T", "b"));

        // Composed with Path.Combine, not spelled out: the key comes from GetPSPath, whose
        // separator follows the host, so hard-coding "\" fails the Linux and macOS legs of CI.
        Assert.Equal(
            [System.IO.Path.Combine(@"Src:\A", "T"), System.IO.Path.Combine(@"Src:\A\Sub", "T")],
            acc);
    }

    // ProcessRecord runs once per piped item, so the same pair can arrive twice.
    [Fact]
    public void TheSameTriggerTwice_CountsOnce()
    {
        var acc = new List<string>();
        CompareTriggerCmdlet.AddCronDiff(acc, Qt(@"Src:\A", "T", "a"), Qt(@"Dst:\A", "T", "b"));
        CompareTriggerCmdlet.AddCronDiff(acc, Qt(@"Src:\A", "T", "a"), Qt(@"Dst:\A", "T", "b"));

        Assert.Single(acc);
    }

    [Fact]
    public void EqualCrons_AreNotAccumulated()
    {
        var acc = new List<string>();
        CompareTriggerCmdlet.AddCronDiff(acc, Qt(@"Src:\A", "T", "same"), Qt(@"Dst:\A", "T", "same"));
        Assert.Empty(acc);
    }

    [Fact]
    public void Notice_NamesEveryTriggerUpToFive()
    {
        var five = Enumerable.Range(1, 5).Select(i => $@"Src:\F{i}\T").ToList();
        var s = CompareTriggerCmdlet.ComposeCronNotice(five);

        Assert.Contains("differs on 5 queue trigger(s)", s, StringComparison.Ordinal);
        Assert.All(five, p => Assert.Contains(p, s, StringComparison.Ordinal));
        Assert.DoesNotContain("more", s, StringComparison.Ordinal);
    }

    // Beyond five it stays one line: the count carries the scale, the names carry a starting point.
    [Fact]
    public void Notice_CountsTheRestInsteadOfListingThem()
    {
        var eight = Enumerable.Range(1, 8).Select(i => $@"Src:\F{i}\T").ToList();
        var s = CompareTriggerCmdlet.ComposeCronNotice(eight);

        Assert.Contains("differs on 8 queue trigger(s)", s, StringComparison.Ordinal);
        Assert.Contains("and 3 more", s, StringComparison.Ordinal);
        Assert.DoesNotContain(@"Src:\F6\T", s, StringComparison.Ordinal);
    }

    // Zero is the "no queue" value the copy path itself tests for; it must not read as a queue trigger.
    [Fact]
    public void IsQueueTrigger_TreatsNullAndZeroAsTimeTrigger()
    {
        Assert.False(CompareTriggerCmdlet.IsQueueTrigger(new ProcessSchedule()));
        Assert.False(CompareTriggerCmdlet.IsQueueTrigger(new ProcessSchedule { QueueDefinitionId = 0 }));
        Assert.True(CompareTriggerCmdlet.IsQueueTrigger(new ProcessSchedule { QueueDefinitionId = 33 }));
    }
}

public class CompareProcessComparatorTests
{
    private static Func<Release, object?> Get(string name)
        => CompareProcessCmdlet.Comparators.Single(c => c.Name == name).Get;

    // Copy-Item nulls a Release's JobPriority on the same condition it nulls a trigger's, so
    // Compare-OrchProcess had the identical false positive.
    [Fact]
    public void JobPriority_CopiedProcessIsNotDrift()
    {
        var get = Get("JobPriority");
        Assert.Equal(
            get(new Release { JobPriority = "Low", SpecificPriorityValue = 25 }),
            get(new Release { JobPriority = null, SpecificPriorityValue = 25 }));
    }

    [Fact]
    public void JobPriority_RealDriftIsStillReported()
    {
        var get = Get("JobPriority");
        Assert.NotEqual(
            get(new Release { JobPriority = "Low", SpecificPriorityValue = 25 }),
            get(new Release { JobPriority = null, SpecificPriorityValue = 65 }));
    }
}
