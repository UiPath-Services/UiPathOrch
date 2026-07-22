using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.OrchAPI.OrchAPISession;

namespace UnitTests;

// The pre-signed write-URI verb guard shared by WriteBucketItem / WriteBucketItemFromStream.
//
// The verb is chosen by the BUCKET'S STORAGE PROVIDER, so which branch a user lands on depends
// on their tenant's configuration, not on anything they typed -- which is exactly why the
// rejection has to say what it received. Extracted as a pure static (same treatment as
// ParseTokens / IsTokenApplied / ComputeTokenExpiry) so every branch is testable without a live
// bucket or an HTTP round trip.
public class BucketWriteVerbGuardTests
{
    private static BlobFileAccess Access(string? verb) =>
        new() { Verb = verb, Uri = "https://example.blob.core.windows.net/c/o?sig=x" };

    [Theory]
    [InlineData("PUT")]
    [InlineData("put")]   // ordinal-ignore-case, so provider casing does not matter
    [InlineData("Put")]
    public void Put_in_any_casing_is_accepted(string verb)
    {
        // No exception == accepted; the guard returns void.
        EnsurePutWriteUri(Access(verb));
    }

    [Theory]
    [InlineData("POST")]  // e.g. an S3 POST-policy form upload
    [InlineData("GET")]
    [InlineData("PATCH")]
    public void A_non_put_verb_is_rejected_and_named_in_the_message(string verb)
    {
        var ex = Assert.Throws<NotSupportedException>(() => EnsurePutWriteUri(Access(verb)));

        // The received verb must appear verbatim -- without it a bug report has nothing to
        // reproduce from, which is the whole failing of the bare NotImplementedException this
        // guard replaced.
        Assert.Contains(verb, ex.Message);
        Assert.Contains("PUT", ex.Message);
    }

    // Verb is `string?` on BlobFileAccess and the previous code dereferenced it with `!`, so a
    // provider omitting it produced a NullReferenceException instead of a diagnosable error.
    [Fact]
    public void A_missing_verb_is_rejected_rather_than_dereferenced()
    {
        var ex = Assert.Throws<NotSupportedException>(() => EnsurePutWriteUri(Access(null)));

        Assert.Contains("(none)", ex.Message);
    }

    [Fact]
    public void An_empty_verb_is_rejected()
    {
        Assert.Throws<NotSupportedException>(() => EnsurePutWriteUri(Access("")));
    }

    // NotSupportedException, not NotImplementedException: the input is outside the supported
    // range, it is not an unfinished stub. NotImplementedException would also read to the user
    // as "the module is half-built" for what is really a storage-provider capability boundary.
    [Fact]
    public void The_rejection_is_not_a_NotImplementedException()
    {
        var ex = Record.Exception(() => EnsurePutWriteUri(Access("POST")));

        Assert.NotNull(ex);
        Assert.IsNotType<NotImplementedException>(ex);
    }
}
