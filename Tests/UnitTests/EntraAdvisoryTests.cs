using System;
using System.Text;
using Xunit;
using static UiPath.OrchAPI.OrchestratorAuthManager;

namespace UnitTests;

// Tests for the Entra-ID local-user advisory decision logic, extracted from the
// GetChildItems probe into pure statics on OrchestratorAuthManager. These lock the
// fix for the advisory that was detected correctly but never displayed: the
// once-per-session gate (EntraIdWarningChecked) latched shut before the advisory
// could be queued — a probe taken before the token / partition id / org auth
// setting were available, or an unrelated advisory draining first, would suppress
// it for the rest of the session.
//
// Two layers are covered:
//   * ClassifyEntraUserKind — JWT ext_idp_disp_name -> tri-state principal kind,
//     returning Unknown (NOT "not a local user") for an absent/malformed token so
//     the caller retries instead of latching prematurely.
//   * DecideEntraAdvisory — (kind, partition known?, auth setting fetched?, type)
//     -> (queue the advisory?, latch the gate?). The gate latches ONLY on a
//     conclusive outcome.
//
// The internal EntraUserKind enum can't appear in a public test method signature
// (cf. AuthFlowSelectionTests), so it is passed/compared as a string.
public class EntraAdvisoryTests
{
    // Build a JWT-shaped token (header.payload.signature) whose middle segment is
    // the base64url-encoded payload JSON — matching what Jwt.DecodePayloadJson reads.
    private static string MakeToken(string payloadJson)
    {
        string segment = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return "header." + segment + ".signature";
    }

    // ---------------- ClassifyEntraUserKind ----------------

    [Theory]
    [InlineData(null)]                        // no token yet
    [InlineData("")]                          // empty
    [InlineData("onlytwo.parts")]             // not a 3-part JWT
    [InlineData("header.@@notbase64@@.sig")]  // undecodable payload segment
    public void ClassifyEntraUserKind_AbsentOrMalformed_IsUnknown(string? token)
        => Assert.Equal("Unknown", ClassifyEntraUserKind(token).ToString());

    [Theory]
    [InlineData("aad", "EntraOrNotApplicable")] // signed in via the directory
    [InlineData("GlobalIdp", "LocalUser")]      // local / social account
    [InlineData("auth0", "LocalUser")]          // any non-"aad" idp
    [InlineData("Okta SAML", "LocalUser")]
    public void ClassifyEntraUserKind_ByIdpDisplayName(string idp, string expected)
        => Assert.Equal(expected, ClassifyEntraUserKind(MakeToken($"{{\"ext_idp_disp_name\":\"{idp}\"}}")).ToString());

    [Fact]
    public void ClassifyEntraUserKind_MissingClaim_IsNotApplicable()
        => Assert.Equal("EntraOrNotApplicable", ClassifyEntraUserKind(MakeToken("{\"sub\":\"x\"}")).ToString());

    // ---------------- DecideEntraAdvisory ----------------
    // (kind, partitionKnown, authSettingFetched, authenticationSettingType)
    //   -> (QueueWarning, Latch).

    [Theory]
    // Unknown: no token yet -> never latch, never warn (a later enumeration retries).
    [InlineData("Unknown", false, false, null, false, false)]
    [InlineData("Unknown", true, true, "aad", false, false)]    // other inputs ignored when Unknown
    // Entra / not-applicable: conclusive no-warn, latch.
    [InlineData("EntraOrNotApplicable", false, false, null, false, true)]
    // Local user, but the probe is inconclusive -> retry (no latch).
    [InlineData("LocalUser", false, false, null, false, false)] // partition id not known yet
    [InlineData("LocalUser", true, false, null, false, false)]  // org auth setting not fetched yet
    // Local user, conclusive.
    [InlineData("LocalUser", true, true, "aad", true, true)]     // org is Entra-integrated -> warn + latch
    [InlineData("LocalUser", true, true, "ad", false, true)]     // org not Entra -> latch only
    [InlineData("LocalUser", true, true, null, false, true)]     // fetched but type none -> latch only
    public void DecideEntraAdvisory_Cases(
        string kindName, bool partitionKnown, bool authSettingFetched, string? authType,
        bool expectQueueWarning, bool expectLatch)
    {
        var kind = Enum.Parse<EntraUserKind>(kindName);
        var decision = DecideEntraAdvisory(kind, partitionKnown, authSettingFetched, authType);
        Assert.Equal(expectQueueWarning, decision.QueueWarning);
        Assert.Equal(expectLatch, decision.Latch);
    }
}
