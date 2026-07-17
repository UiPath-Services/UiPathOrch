using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Per-field change detection for Update-OrchQueue's main PUT payload (retention is a separate
// API and handled outside the pure core). For every field that can flip the dirty flag, assert
// both directions: the current value is a no-op, a different value writes.
public class ComputeQueueUpdate_EveryFieldTests
{
    private static Tag[] T(params (string n, string? v)[] items) =>
        items.Select(i => new Tag { Name = i.n, Value = i.v }).ToArray();

    private static QueueDefinition Baseline() => new()
    {
        Id = 1,
        Name = "Q1",
        Description = "desc",
        AcceptAutomaticallyRetry = true,
        RetryAbandonedItems = true,
        MaxNumberOfRetries = 3,
        SpecificDataJsonSchema = "{a}",
        OutputDataJsonSchema = "{b}",
        AnalyticsDataJsonSchema = "{c}",
        SlaInMinutes = 60,
        RiskSlaInMinutes = 30,
        ReleaseId = 100,
        Tags = T(("env", "prod")),
    };

    private static void AssertField(UpdateQueueCmdlet.QueueUpdateInputs unchanged, UpdateQueueCmdlet.QueueUpdateInputs changed)
    {
        var d1 = Baseline();
        Assert.False(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d1), d1, unchanged),
            "expected NO write when the value equals the current one");
        var d2 = Baseline();
        Assert.True(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d2), d2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var d = Baseline();
        Assert.False(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d), d, new UpdateQueueCmdlet.QueueUpdateInputs()));
    }

    [Fact] public void NewName() => AssertField(new() { NewName = "Q1" }, new() { NewName = "Q2" });
    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "other" });
    [Fact] public void AcceptAutomaticallyRetry() => AssertField(new() { AcceptAutomaticallyRetry = "true" }, new() { AcceptAutomaticallyRetry = "false" });
    [Fact] public void RetryAbandonedItems() => AssertField(new() { RetryAbandonedItems = "true" }, new() { RetryAbandonedItems = "false" });
    [Fact] public void MaxNumberOfRetries() => AssertField(new() { MaxNumberOfRetries = 3 }, new() { MaxNumberOfRetries = 5 });
    [Fact] public void SpecificDataJsonSchema() => AssertField(new() { SpecificDataJsonSchema = "{a}" }, new() { SpecificDataJsonSchema = "{z}" });
    [Fact] public void OutputDataJsonSchema() => AssertField(new() { OutputDataJsonSchema = "{b}" }, new() { OutputDataJsonSchema = "{z}" });
    [Fact] public void AnalyticsDataJsonSchema() => AssertField(new() { AnalyticsDataJsonSchema = "{c}" }, new() { AnalyticsDataJsonSchema = "{z}" });
    [Fact] public void SlaInMinutes() => AssertField(new() { SlaInMinutes = 60 }, new() { SlaInMinutes = 90 });
    [Fact] public void RiskSlaInMinutes() => AssertField(new() { RiskSlaInMinutes = 30 }, new() { RiskSlaInMinutes = 45 });

    [Fact]
    public void Release_SameId_IsNoOp_DifferentId_Writes() => AssertField(
        new() { ReleaseSpecified = true, ReleaseResolved = true, ResolvedReleaseId = 100 },
        new() { ReleaseSpecified = true, ReleaseResolved = true, ResolvedReleaseId = 200 });

    [Fact]
    public void Release_SpecifiedButUnresolved_IsNoOp()
    {
        var d = Baseline();
        Assert.False(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d), d,
            new UpdateQueueCmdlet.QueueUpdateInputs { ReleaseSpecified = true, ReleaseResolved = false, ResolvedReleaseId = 999 }));
    }

    [Fact]
    public void Tags_SameSetDifferentOrder_IsNoOp()
    {
        var d = Baseline();
        d.Tags = T(("env", "prod"), ("tier", "1"));
        Assert.False(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d), d,
            new UpdateQueueCmdlet.QueueUpdateInputs { Tags = new[] { "tier=1", "env=prod" } }));
    }

    [Fact]
    public void Tags_DifferentSet_Writes()
    {
        var d = Baseline();
        Assert.True(UpdateQueueCmdlet.ComputeQueueUpdate(OrchCollectionExtensions.DeepCopy(d), d,
            new UpdateQueueCmdlet.QueueUpdateInputs { Tags = new[] { "env=dev" } }));
    }
}
