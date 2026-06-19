using UiPath.OrchAPI;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchAPISession.ApplyQueueRetentionDefaults — the single source of the queue retention
// defaults shared by New-OrchQueue and Copy-OrchQueue (CopyQueues).
//
// Behaviour anchored to live verification against the OData.CreateQueue action endpoint:
//   - Automation Cloud (v20): RetentionPeriod required, Final + Stale honoured.
//   - On-prem 25.10.2 (API v17): Period not required, Final honoured.
// => Delete/30 is a load-bearing default (Cloud requires a Period); Stale defaults apply only
//    >= v19; the "None"->Delete guard (also >= v19) is retained for an older platform that was
//    observed to reject "None" on POST (no longer reproducible) — see ApplyQueueRetentionDefaults.
public class QueueRetentionDefaultsTests
{
    [Fact]
    public void Below_v16_leaves_everything_untouched()
    {
        // < v16 has no queue retention concept; the legacy create path strips it anyway.
        var q = new QueueDefinition { Name = "Q" };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 15.0);
        Assert.Null(q.RetentionAction);
        Assert.Null(q.RetentionPeriod);
        Assert.Null(q.StaleRetentionAction);
        Assert.Null(q.StaleRetentionPeriod);
    }

    [Fact]
    public void v17_defaults_final_to_delete_30_and_skips_stale()
    {
        var q = new QueueDefinition { Name = "Q" };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 17.0);
        Assert.Equal("Delete", q.RetentionAction);
        Assert.Equal(30, q.RetentionPeriod);
        // StaleRetention is a >= v19 concept — untouched here.
        Assert.Null(q.StaleRetentionAction);
        Assert.Null(q.StaleRetentionPeriod);
    }

    [Fact]
    public void v19_defaults_both_final_and_stale()
    {
        var q = new QueueDefinition { Name = "Q" };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 19.0);
        Assert.Equal("Delete", q.RetentionAction);
        Assert.Equal(30, q.RetentionPeriod);
        Assert.Equal("Delete", q.StaleRetentionAction);
        Assert.Equal(180, q.StaleRetentionPeriod);
    }

    [Fact]
    public void Zero_period_is_treated_as_unset_and_defaulted()
    {
        var q = new QueueDefinition { Name = "Q", RetentionPeriod = 0, StaleRetentionPeriod = 0 };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 19.0);
        Assert.Equal(30, q.RetentionPeriod);
        Assert.Equal(180, q.StaleRetentionPeriod);
    }

    [Fact]
    public void Existing_values_are_preserved()
    {
        var q = new QueueDefinition
        {
            Name = "Q",
            RetentionAction = "Archive",
            RetentionPeriod = 99,
            StaleRetentionAction = "Archive",
            StaleRetentionPeriod = 77,
        };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 20.0);
        Assert.Equal("Archive", q.RetentionAction);
        Assert.Equal(99, q.RetentionPeriod);
        Assert.Equal("Archive", q.StaleRetentionAction);
        Assert.Equal(77, q.StaleRetentionPeriod);
    }

    [Fact]
    public void Below_v19_passes_None_through_uncoerced()
    {
        // The None->Delete guard is gated >= v19 (Cloud / Automation Suite). On-prem (<= v17)
        // never reaches it, so an explicit "None" survives.
        var q = new QueueDefinition { Name = "Q", RetentionAction = "None", RetentionPeriod = 45 };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 17.0);
        Assert.Equal("None", q.RetentionAction);
    }

    [Fact]
    public void v19_coerces_None_to_Delete_for_final_and_stale()
    {
        // Retained guard: an older Orchestrator was observed to reject "None" on POST. See the
        // note on ApplyQueueRetentionDefaults.
        var q = new QueueDefinition
        {
            Name = "Q",
            RetentionAction = "None",
            RetentionPeriod = 45,
            StaleRetentionAction = "None",
            StaleRetentionPeriod = 45,
        };
        OrchAPISession.ApplyQueueRetentionDefaults(q, 19.0);
        Assert.Equal("Delete", q.RetentionAction);
        Assert.Equal("Delete", q.StaleRetentionAction);
        // periods are user-supplied non-zero -> preserved
        Assert.Equal(45, q.RetentionPeriod);
        Assert.Equal(45, q.StaleRetentionPeriod);
    }
}
