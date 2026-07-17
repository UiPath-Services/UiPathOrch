using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Exhaustive per-field change-detection coverage for Update-OrchBucket's pure core
// (UpdateBucketCmdlet.ComputeBucketUpdate): every field that can land in the PUT payload is
// asserted in both directions — the current value is a no-op, a different value writes.
public class ComputeBucketUpdate_EveryFieldTests
{
    private static Tag[] T(params (string n, string? v)[] items) =>
        items.Select(i => new Tag { Name = i.n, Value = i.v }).ToArray();

    private static Bucket Baseline() => new()
    {
        Id = 1,
        Name = "bucket1",
        Description = "desc",
        StorageProvider = "FileSystem",
        StorageParameters = "param",
        StorageContainer = "container",
        ExternalName = "ext",
        Options = "a,b",
        CredentialStoreId = 7,
        Tags = T(("env", "prod"), ("tier", "1")),
    };

    private static void AssertField(UpdateBucketCmdlet.BucketUpdateInputs unchanged, UpdateBucketCmdlet.BucketUpdateInputs changed)
    {
        var s1 = Baseline();
        Assert.False(UpdateBucketCmdlet.ComputeBucketUpdate(OrchCollectionExtensions.DeepCopy(s1), s1, unchanged),
            "expected NO write when the value equals the current one");
        var s2 = Baseline();
        Assert.True(UpdateBucketCmdlet.ComputeBucketUpdate(OrchCollectionExtensions.DeepCopy(s2), s2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact] public void NewName() => AssertField(new() { NewName = "bucket1" }, new() { NewName = "bucket2" });
    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "changed" });
    [Fact] public void StorageProvider() => AssertField(new() { StorageProvider = "FileSystem" }, new() { StorageProvider = "Azure" });
    [Fact] public void StorageParameters() => AssertField(new() { StorageParameters = "param" }, new() { StorageParameters = "changed" });
    [Fact] public void StorageContainer() => AssertField(new() { StorageContainer = "container" }, new() { StorageContainer = "changed" });
    [Fact] public void ExternalName() => AssertField(new() { ExternalName = "ext" }, new() { ExternalName = "changed" });

    [Fact]
    public void Options() => AssertField(
        new() { Options = new[] { "a", "b" } },   // joins to "a,b" == current
        new() { Options = new[] { "a", "c" } });  // joins to "a,c"

    [Fact]
    public void CredentialStore() => AssertField(
        new() { CredentialStoreResolved = true, ResolvedCredentialStoreId = 7 },   // same id
        new() { CredentialStoreResolved = true, ResolvedCredentialStoreId = 8 });  // different id

    [Fact]
    public void Tags() => AssertField(
        new() { Tags = new[] { "tier=1", "env=prod" } },   // same set, different order
        new() { Tags = new[] { "env=dev" } });

    [Fact]
    public void Password_AlwaysWritesWhenSupplied_NoOpWhenBlank()
    {
        var s1 = Baseline();
        Assert.True(UpdateBucketCmdlet.ComputeBucketUpdate(OrchCollectionExtensions.DeepCopy(s1), s1,
            new UpdateBucketCmdlet.BucketUpdateInputs { Password = "s3cr3t" }));

        // A blank password must be a no-op AND must not be written onto the payload (never write a
        // blank credential), matching Update-OrchWebhook's -Secret rule.
        var s2 = Baseline();
        var payload = OrchCollectionExtensions.DeepCopy(s2);
        Assert.False(UpdateBucketCmdlet.ComputeBucketUpdate(payload, s2,
            new UpdateBucketCmdlet.BucketUpdateInputs { Password = "" }));
        Assert.Null(payload.Password);
    }

    [Fact]
    public void CredentialStore_SpecifiedButUnresolved_IsNoOp()
    {
        // Name given but did not resolve (found == null): the block must not fire.
        var s = Baseline();
        Assert.False(UpdateBucketCmdlet.ComputeBucketUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateBucketCmdlet.BucketUpdateInputs { CredentialStoreResolved = false, ResolvedCredentialStoreId = null }));
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var s = Baseline();
        Assert.False(UpdateBucketCmdlet.ComputeBucketUpdate(OrchCollectionExtensions.DeepCopy(s), s,
            new UpdateBucketCmdlet.BucketUpdateInputs()));
    }
}
