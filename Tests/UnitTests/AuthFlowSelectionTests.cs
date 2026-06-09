using Xunit;
using static UiPath.OrchAPI.OrchestratorAuthManager;

namespace UnitTests;

// Exhaustive routing tests for the auth-flow selection extracted from RequestToken /
// RenewAccessToken (OrchestratorAuthManager). These lock the 1.9.1 on-prem fix: a token RENEWAL
// for user/password and PAT drives must re-run the initial request (re-authenticate / re-apply),
// NOT send a refresh_token grant -- only the interactive (PKCE) flow ever obtains a refresh token,
// so posting one for the other modes sent refresh_token=null and broke the drive at the expiry
// fallback. Driving live token endpoints is deliberately avoided (no HTTP mocking, like the
// ParseTokens / IsTokenApplied tests), so the routing is verified at the decision layer that
// RequestToken / RenewAccessToken now both call.
//
// Expected values are passed as strings (the AuthFlow enum is internal, and a public xUnit test
// method may not expose it in its signature); the assertions compare against AuthFlow.ToString().
public class AuthFlowSelectionTests
{
    // ---------------- SelectInitialFlow ----------------
    // Dispatch order: a stored PAT wins, then a confidential app, then interactive PKCE,
    // otherwise on-prem user/password.

    [Theory]
    [InlineData(true, false, false, "PatReapply")]         // PAT
    [InlineData(true, true, true, "PatReapply")]           // PAT precedence over everything
    [InlineData(false, true, false, "ClientCredentials")]  // confidential app
    [InlineData(false, true, true, "ClientCredentials")]   // confidential precedence over user/pass
    [InlineData(false, false, false, "Pkce")]              // interactive external app
    [InlineData(false, false, true, "UserPassword")]       // on-prem user/password
    public void SelectInitialFlow_DispatchesByCredentialShape(
        bool hasAccessToken, bool isConfidentialApp, bool isUserPassword, string expected)
        => Assert.Equal(expected, SelectInitialFlow(hasAccessToken, isConfidentialApp, isUserPassword).ToString());

    // ---------------- SelectRenewalFlow ----------------
    // The refresh_token grant is used ONLY by an interactive (PKCE) drive that actually holds a
    // refresh token; every other mode renews by re-running its initial flow.

    [Theory]
    // PKCE that holds a refresh token -> refresh_token grant (unchanged behaviour).
    [InlineData(false, false, false, true, "RefreshToken")]
    // *** The 1.9.1 fix ***: on-prem user/password has no refresh token -> re-authenticate,
    // NOT a refresh_token grant (which used to post refresh_token=null and break the drive).
    [InlineData(false, false, true, false, "UserPassword")]
    // PAT has no refresh token -> re-apply the stored token.
    [InlineData(true, false, false, false, "PatReapply")]
    // Confidential app: always client_credentials, with or without a refresh token present.
    [InlineData(false, true, false, false, "ClientCredentials")]
    [InlineData(false, true, false, true, "ClientCredentials")]
    // Abnormal: an interactive drive that somehow lost its refresh token re-runs PKCE rather than
    // posting a null refresh_token -- it recovers via re-auth instead of failing.
    [InlineData(false, false, false, false, "Pkce")]
    public void SelectRenewalFlow_RefreshTokenOnlyForPkceWithToken(
        bool hasAccessToken, bool isConfidentialApp, bool isUserPassword, bool hasRefreshToken, string expected)
        => Assert.Equal(expected, SelectRenewalFlow(hasAccessToken, isConfidentialApp, isUserPassword, hasRefreshToken).ToString());
}
