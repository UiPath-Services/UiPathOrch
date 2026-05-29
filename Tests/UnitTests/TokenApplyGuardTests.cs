using System.Text.Json;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Guards against the stale-token loop: a token endpoint can answer 200 with no
// access_token. OrchestratorAuthManager.ParseTokens reports that as "" (it does
// NOT throw), and OrchAPISession.IsTokenApplied / SetToken then refuse to apply
// it — so EnsureAuthenticated does not advance its expiry (nor mark the session
// authenticated) behind a Bearer header that was never set, which would
// otherwise pin the stale/absent token until a 401.
public class TokenApplyGuardTests
{
    private static JsonElement Root(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void ParseTokens_MissingAccessToken_ReturnsEmpty_NotThrow()
    {
        var (access, refresh) = OrchestratorAuthManager.ParseTokens(
            Root("""{ "token_type": "Bearer", "expires_in": 3600 }"""));
        Assert.Equal("", access);
        Assert.Equal("", refresh);
    }

    [Fact]
    public void ParseTokens_ReadsBothTokens()
    {
        var (access, refresh) = OrchestratorAuthManager.ParseTokens(
            Root("""{ "access_token": "AT", "refresh_token": "RT" }"""));
        Assert.Equal("AT", access);
        Assert.Equal("RT", refresh);
    }

    [Fact]
    public void ParseTokens_MissingRefresh_AccessStillRead()
    {
        var (access, refresh) = OrchestratorAuthManager.ParseTokens(
            Root("""{ "access_token": "AT" }"""));
        Assert.Equal("AT", access);
        Assert.Equal("", refresh);
    }

    [Fact]
    public void ParseTokens_NullJsonValues_ReturnEmpty()
    {
        var (access, refresh) = OrchestratorAuthManager.ParseTokens(
            Root("""{ "access_token": null, "refresh_token": null }"""));
        Assert.Equal("", access);
        Assert.Equal("", refresh);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("AT", true)]
    public void IsTokenApplied_OnlyNonEmptyIsUsable(string? token, bool expected)
    {
        // SetToken returns this and the (re)auth flow advances expiry / marks
        // authenticated only when it is true.
        Assert.Equal(expected, OrchAPISession.IsTokenApplied(token));
    }
}
