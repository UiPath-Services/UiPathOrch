using System.Text.Json;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins the token-expiry logic introduced to stop assuming a flat 1h lifetime.
// Two pure pieces are exercised:
//   - OrchestratorAuthManager.ParseExpiresInSeconds: read `expires_in` from a
//     token response body (0 when absent / non-numeric / non-positive).
//   - OrchAPISession.ComputeTokenExpiry: turn that lifetime into an expiry
//     instant, falling back to 1h when the lifetime is unknown (PAT /
//     user-password flows never report one).
public class TokenExpiryTests
{
    private static JsonElement Root(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void ParseExpiresIn_ReadsPositiveValue()
    {
        Assert.Equal(3600, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "access_token": "x", "expires_in": 3600 }""")));
    }

    [Fact]
    public void ParseExpiresIn_ShorterThanDefaultIsHonored()
    {
        // The whole point: an IdP issuing < 1h tokens (e.g. Automation Suite /
        // on-prem policies) must be respected, not overridden by the 1h guess.
        Assert.Equal(1800, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "expires_in": 1800 }""")));
    }

    [Fact]
    public void ParseExpiresIn_MissingPropertyReturnsZero()
    {
        Assert.Equal(0, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "access_token": "x", "refresh_token": "y" }""")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ParseExpiresIn_NonPositiveReturnsZero(int value)
    {
        Assert.Equal(0, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root($$"""{ "expires_in": {{value}} }""")));
    }

    [Fact]
    public void ParseExpiresIn_QuotedNumberIsParsed()
    {
        // A non-conforming IdP may quote the number; honor it (don't throw, and
        // don't discard a real lifetime into the 1h fallback).
        Assert.Equal(3600, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "expires_in": "3600" }""")));
    }

    [Fact]
    public void ParseExpiresIn_QuotedShortLifetimeIsHonored()
    {
        Assert.Equal(1800, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "expires_in": "1800" }""")));
    }

    [Fact]
    public void ParseExpiresIn_NonNumericStringReturnsZero()
    {
        Assert.Equal(0, OrchestratorAuthManager.ParseExpiresInSeconds(
            Root("""{ "expires_in": "soon" }""")));
    }

    [Fact]
    public void ComputeExpiry_PositiveLifetimeUsesReportedSeconds()
    {
        var from = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(from.AddSeconds(1800), OrchAPISession.ComputeTokenExpiry(from, 1800));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComputeExpiry_UnknownLifetimeFallsBackToOneHour(int seconds)
    {
        var from = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(from.AddHours(1), OrchAPISession.ComputeTokenExpiry(from, seconds));
    }
}
