using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// OrchAPISession.CapErrorBody bounds the response-body text that becomes an
// exception message, so a large non-JSON error payload can't flood
// Start-Transcript / CI logs (the full body stays on HttpResponseException.Response).
public class ErrorBodyCapTests
{
    [Fact]
    public void ShortBody_Unchanged()
    {
        Assert.Equal("Bad Request: name is required", OrchAPISession.CapErrorBody("Bad Request: name is required"));
    }

    [Fact]
    public void EmptyOrNull_Unchanged()
    {
        Assert.Equal("", OrchAPISession.CapErrorBody(""));
        Assert.Null(OrchAPISession.CapErrorBody(null!));
    }

    [Fact]
    public void ExactlyAtLimit_Unchanged()
    {
        var s = new string('y', OrchAPISession.MaxErrorBodyChars);
        Assert.Equal(s, OrchAPISession.CapErrorBody(s));
    }

    [Fact]
    public void OverLimit_TruncatedWithMarker()
    {
        var big = new string('x', OrchAPISession.MaxErrorBodyChars + 5000);
        var capped = OrchAPISession.CapErrorBody(big);

        Assert.True(capped.Length < big.Length);
        Assert.EndsWith("[truncated]", capped);
        // The kept prefix is exactly MaxErrorBodyChars of original content.
        Assert.StartsWith(new string('x', OrchAPISession.MaxErrorBodyChars), capped);
        Assert.Equal(OrchAPISession.MaxErrorBodyChars + "… [truncated]".Length, capped.Length);
    }
}
