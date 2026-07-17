using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// The retention diff (shared by Update-OrchProcess and Update-OrchQueue) and the entry-point
// reassignment decision used to live inline in the cmdlets, so the bucket-name/feed resolution
// they need kept them out of the pure Compute* cores. The resolution now stays in the cmdlet and
// the DECISIONS are pure: OrchStringExtensions.ComputeRetentionUpdate and
// UpdateProcessCmdlet.ShouldReassignEntryPoint, both exercised here in both directions. The tests
// also pin the pre-existing quirk that an empty-string bucket clear flips the dirty flag but the
// PUT fill-default restores the current id, so the clear is effectively inert (preserved verbatim).
public class RetentionAndEntryPointDirtyTests
{
    private static (bool dirty, string action, int period, long? bucketId) Run(
        OrchStringExtensions.RetentionUpdateInput input,
        string? curAction, int? curPeriod, long? curBucketId)
    {
        bool dirty = OrchStringExtensions.ComputeRetentionUpdate(
            input, curAction, curPeriod, curBucketId,
            out string action, out int period, out long? bucketId);
        return (dirty, action, period, bucketId);
    }

    // ----- nothing / action / period -----

    [Fact]
    public void NothingSpecified_IsNoOp()
        => Assert.False(Run(new(), "Delete", 30, 7).dirty);

    [Fact]
    public void Action_SameValue_IsNoOp()
        => Assert.False(Run(new() { Action = "Delete" }, "Delete", 30, 7).dirty);

    [Fact]
    public void Action_DifferentValue_Writes_AndFillsTheRestFromCurrent()
    {
        var r = Run(new() { Action = "Archive" }, "Delete", 30, 7);
        Assert.True(r.dirty);
        Assert.Equal("Archive", r.action);
        Assert.Equal(30, r.period);       // filled from current
        Assert.Equal(7, r.bucketId);      // filled from current
    }

    [Fact]
    public void Period_SameValue_IsNoOp()
        => Assert.False(Run(new() { Period = 30 }, "Delete", 30, 7).dirty);

    [Fact]
    public void Period_DifferentValue_Writes()
    {
        var r = Run(new() { Period = 60 }, "Delete", 30, 7);
        Assert.True(r.dirty);
        Assert.Equal(60, r.period);
    }

    [Fact]
    public void Period_Zero_IsTreatedAsUnspecified_NoOp()
        => Assert.False(Run(new() { Period = 0 }, "Delete", 30, 7).dirty);

    // ----- bucket (resolved id) -----

    [Fact]
    public void Bucket_SameResolvedId_IsNoOp()
        => Assert.False(Run(new() { ResolvedBucketId = 7 }, "Delete", 30, 7).dirty);

    [Fact]
    public void Bucket_DifferentResolvedId_Writes()
    {
        var r = Run(new() { ResolvedBucketId = 8 }, "Delete", 30, 7);
        Assert.True(r.dirty);
        Assert.Equal(8, r.bucketId);
    }

    [Fact]
    public void Bucket_NotSpecified_LeavesTheCurrentId()
    {
        // Not the bucket that's dirty — action is — so the payload carries the current bucket id.
        var r = Run(new() { Action = "Archive" }, "Delete", 30, 7);
        Assert.Equal(7, r.bucketId);
    }

    // ----- empty-string clear: dirty flips, but fill-default restores the current id (inert) -----

    [Fact]
    public void Bucket_EmptyClearWhenSet_FlipsDirtyButKeepsCurrentId()
    {
        var r = Run(new() { BucketCleared = true }, "Delete", 30, 7);
        Assert.True(r.dirty);        // the clear flips dirty...
        Assert.Equal(7, r.bucketId); // ...but the id sent is still the current one (clear is inert)
    }

    [Fact]
    public void Bucket_EmptyClearWhenAlreadyNull_IsNoOp()
        => Assert.False(Run(new() { BucketCleared = true }, "Delete", 30, null).dirty);

    // ----- PUT fill-defaults from a fully-null current -----

    [Fact]
    public void FillDefaults_FromNullCurrent_UsesActionDeleteAndPeriod30()
    {
        var r = Run(new() { Action = "Archive" }, null, null, null);
        Assert.True(r.dirty);
        Assert.Equal("Archive", r.action);
        Assert.Equal(30, r.period);      // default when current is null
        Assert.Null(r.bucketId);
    }

    // ----- ShouldReassignEntryPoint -----

    [Fact]
    public void EntryPoint_SameId_IsNoOp()
        => Assert.False(UpdateProcessCmdlet.ShouldReassignEntryPoint(5, 5));

    [Fact]
    public void EntryPoint_DifferentId_Reassigns()
        => Assert.True(UpdateProcessCmdlet.ShouldReassignEntryPoint(5, 8));

    [Fact]
    public void EntryPoint_ClearFromSet_Reassigns()
        => Assert.True(UpdateProcessCmdlet.ShouldReassignEntryPoint(5, null));

    [Fact]
    public void EntryPoint_NullToValue_Reassigns()
        => Assert.True(UpdateProcessCmdlet.ShouldReassignEntryPoint(null, 5));

    [Fact]
    public void EntryPoint_BothNull_IsNoOp()
        => Assert.False(UpdateProcessCmdlet.ShouldReassignEntryPoint(null, null));
}
