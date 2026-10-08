using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// The sign-in success page names the server: its edition (from configuration, always known) and,
// when /api/Status/Version answered, the product build and API version.
public class SignInServerLineTests
{
    [Theory]
    [InlineData(OrchEdition.Cloud, "26.3.0-s203.8780", 20.0, "Automation Cloud 26.3.0-s203.8780 (API v20.0)")]
    [InlineData(OrchEdition.AutomationSuite, "24.10.11", 18.0, "Automation Suite 24.10.11 (API v18.0)")]
    [InlineData(OrchEdition.OnPremises, "25.10.2-standalone", 17.0, "Standalone Orchestrator 25.10.2-standalone (API v17.0)")]
    [InlineData(OrchEdition.OnPremises, null, 11.1, "Standalone Orchestrator (API v11.1)")]
    [InlineData(OrchEdition.OnPremises, null, null, "Standalone Orchestrator")]          // probe failed or timed out
    [InlineData(OrchEdition.Cloud, "26.3.0", null, "Automation Cloud 26.3.0")]
    public void Names_the_edition_and_whatever_versions_were_reported(OrchEdition edition, string? product, double? api, string expected)
        => Assert.Equal(expected, OrchestratorAuthManager.FormatServerLine(edition, product, api));

    // One parser for the header, whether it arrives on the sign-in probe or on any API response.
    [Theory]
    [InlineData(new[] { "20.0" }, 20.0)]
    [InlineData(new[] { "11.1" }, 11.1)]
    [InlineData(new[] { "16.0, 17.0" }, 17.0)]
    [InlineData(new[] { "17.0", "18.0" }, 18.0)]
    [InlineData(new[] { "" }, null)]
    [InlineData(new[] { "x" }, null)]
    public void Api_supported_versions_yields_the_highest(string[] header, double? expected)
        => Assert.Equal(expected, OrchAPISession.ParseApiSupportedVersions(header));

    // Measured limits (2026-10-08); builds not measured have none.
    [Theory]
    [InlineData("21.10.4", 300)]
    [InlineData("22.10.1", 500)]
    [InlineData("23.4.0", 500)]
    [InlineData("24.10.0-standalone", 750)]
    [InlineData("24.10.8-standalone", 1250)]
    [InlineData("24.10.11", 1250)]
    [InlineData("25.10.2-standalone", 1250)]
    [InlineData("24.10.3", null)]
    [InlineData("26.3.0-s203.8780", null)]
    [InlineData(null, null)]
    public void Known_scope_length_limit_by_product_version(string? product, int? limit)
        => Assert.Equal(limit, OrchestratorAuthManager.KnownScopeLengthLimit(product));

    // Over-length comes back as invalid_request "Invalid scope"; an unknown scope as invalid_scope.
    [Theory]
    [InlineData("invalid_request", "Invalid scope", true)]
    [InlineData("invalid_request", "Invalid scope.", true)]
    [InlineData("invalid_scope", "Invalid scope", false)]
    [InlineData("invalid_request", "Invalid redirect_uri", false)]
    [InlineData("invalid_request", null, false)]
    public void Scope_length_rejection_is_recognized(string error, string? description, bool expected)
        => Assert.Equal(expected, OrchestratorAuthManager.IsScopeLengthRejection(error, description));

    [Fact]
    public void Advice_with_a_known_limit_names_the_drive_the_file_and_how_much_to_cut()
    {
        string advice = OrchestratorAuthManager.BuildScopeTooLongAdvice("op2110:", 397, "21.10.4")!;
        Assert.Contains("op2110:", advice);
        Assert.Contains("the limit is 300", advice);
        Assert.Contains("at least 97 characters", advice);
        Assert.Contains("Edit-OrchConfig", advice);
        Assert.Contains("Import-OrchConfig", advice);
    }

    [Fact]
    public void Advice_without_a_known_limit_gives_the_length()
    {
        string advice = OrchestratorAuthManager.BuildScopeTooLongAdvice("x:", 1300, null)!;
        Assert.Contains("probably too long (1300 characters", advice);
    }

    // Confidential app: the token endpoint's bare invalid_scope may mean length or an ungranted scope.
    [Theory]
    [InlineData(305, "21.10.4", "is too long for Orchestrator 21.10.4")]
    [InlineData(305, "25.10.2-standalone", null)]  // within that server's limit: not length
    [InlineData(305, null, "may be too long (305 characters")]
    [InlineData(300, null, null)]                  // within the smallest limit: never length
    public void Ambiguous_refusal_mentions_length_only_when_it_can_be_the_reason(int length, string? product, string? expected)
    {
        string? advice = OrchestratorAuthManager.BuildScopeTooLongAdvice("op2110c:", length, product, lengthIsCertain: false);
        if (expected is null) Assert.Null(advice);
        else Assert.Contains(expected, advice);
    }

    [Fact]
    public void No_advice_when_the_scope_is_within_the_known_limit()
        => Assert.Null(OrchestratorAuthManager.BuildScopeTooLongAdvice("op2110:", 300, "21.10.4"));

    [Fact]
    public void Callback_error_carries_the_advice_in_place_of_the_generic_one()
    {
        string message = OrchestratorAuthManager.BuildOAuthCallbackErrorMessage("invalid_request", "Invalid scope", null, "ADVICE.");
        Assert.EndsWith("Invalid scope. ADVICE.", message);
        Assert.DoesNotContain("Verify the application registration", message);
    }

    [Fact]
    public void Requested_scope_is_the_configured_scope_plus_offline_access()
        => Assert.Equal("OR.Folders OR.Jobs offline_access", OrchestratorAuthManager.RequestedScope("OR.Folders OR.Jobs"));

    [Theory]
    [InlineData("en")]
    [InlineData("ja")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("ko")]
    [InlineData("ro")]
    [InlineData("tr")]
    public void Every_template_formats_with_the_server_argument(string lang)
    {
        using var stream = typeof(OrchestratorAuthManager).Assembly
            .GetManifestResourceStream($"UiPathOrch.Resources.{lang}.MountSuccessNotification.html");
        Assert.NotNull(stream);
        string template = new System.IO.StreamReader(stream!).ReadToEnd();
        string page = string.Format(template, "https://x", "X:", "1.0.0", "", "", "u", "display:none", "Automation Cloud");
        Assert.Contains("Automation Cloud", page);
    }
}
