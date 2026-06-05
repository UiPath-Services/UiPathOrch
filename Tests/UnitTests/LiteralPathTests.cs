using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit coverage for the -LiteralPath / PSPath mechanism:
//   - StripProviderQualifier: PSPath note-properties (and -LiteralPath via [Alias("PSPath")])
//     arrive provider-qualified ("Module\Provider::Drive:\X"); the shared resolver strips the
//     qualifier to the drive-qualified form. A drive-qualified Orch path never contains "::".
//   - EffectivePath: -Path passes through unchanged (wildcards intact); -LiteralPath is
//     WildcardPattern.Escaped so its metacharacters resolve literally.
//   - Folder no longer carries a parent Path; GetPSPath returns the stamped FullName (own path).
public class LiteralPathTests
{
    [Theory]
    [InlineData(@"UiPathOrch\UiPathOrch::Orch1:\Autopilot", @"Orch1:\Autopilot")]
    [InlineData(@"Microsoft.PowerShell.Core\FileSystem::C:\x", @"C:\x")]
    [InlineData(@"Orch1:\Shared", @"Orch1:\Shared")]   // drive-qualified: no "::", unchanged
    [InlineData(@"Orch1:\", @"Orch1:\")]
    [InlineData("Orch1:", "Orch1:")]
    [InlineData("", "")]
    public void StripProviderQualifier_strips_provider_prefix(string input, string expected)
        => Assert.Equal(expected, SessionStateExtentios.StripProviderQualifier(input));

    [Fact]
    public void StripProviderQualifier_null_returns_null()
        => Assert.Null(SessionStateExtentios.StripProviderQualifier(null));

    [Fact]
    public void StripProviderQualifier_strips_only_to_first_double_colon()
        => Assert.Equal(@"Orch1:\A", SessionStateExtentios.StripProviderQualifier(@"Mod\Prov::Orch1:\A"));

    // ----- EffectivePath (array overload, string[]? -Path / -LiteralPath) -----

    [Fact]
    public void EffectivePath_array_passes_Path_through_when_LiteralPath_null()
    {
        var path = new[] { @"Orch1:\A*", @"Orch1:\B" };
        Assert.Same(path, OrchestratorPSCmdlet.EffectivePath(path, null));   // wildcards preserved, same instance
    }

    [Fact]
    public void EffectivePath_array_null_both_returns_null()
        => Assert.Null(OrchestratorPSCmdlet.EffectivePath((string[]?)null, null));

    [Fact]
    public void EffectivePath_array_escapes_each_LiteralPath_element()
    {
        var result = OrchestratorPSCmdlet.EffectivePath(null, new[] { @"Orch1:\Test[1]", "Robot*" });
        Assert.Equal(WildcardPattern.Escape(@"Orch1:\Test[1]"), result![0]);
        Assert.Equal(WildcardPattern.Escape("Robot*"), result[1]);
    }

    [Fact]
    public void EffectivePath_array_makes_LiteralPath_resolve_literally()
    {
        var token = OrchestratorPSCmdlet.EffectivePath(null, new[] { "Test[1]" })![0]!;
        var wp = new WildcardPattern(token, WildcardOptions.IgnoreCase);
        Assert.True(wp.IsMatch("Test[1]"));   // [1] is literal, not a character class
        Assert.False(wp.IsMatch("Test1"));
    }

    // ----- EffectivePath (scalar overload, string? -Path / -LiteralPath) -----

    [Fact]
    public void EffectivePath_scalar_passes_Path_through_when_LiteralPath_null()
        => Assert.Equal(@"Orch1:\A*", OrchestratorPSCmdlet.EffectivePath(@"Orch1:\A*", (string?)null));

    [Fact]
    public void EffectivePath_scalar_escapes_LiteralPath()
        => Assert.Equal(WildcardPattern.Escape(@"Orch1:\Test[1]"), OrchestratorPSCmdlet.EffectivePath((string?)null, @"Orch1:\Test[1]"));

    // ----- Folder shape (FileSystem-provider parity) -----

    [Fact]
    public void Folder_GetPSPath_returns_FullName()
    {
        var f = new Folder { FullName = @"Orch1:\Finance", DisplayName = "Finance", FullyQualifiedName = "Finance" };
        Assert.Equal(@"Orch1:\Finance", f.GetPSPath());
    }

    [Fact]
    public void Folder_has_no_Path_property_but_has_FullName()
    {
        // A dir item follows the FileSystemInfo convention: FullName + PSPath, no Path.
        Assert.Null(typeof(Folder).GetProperty("Path"));
        Assert.NotNull(typeof(Folder).GetProperty("FullName"));
    }
}
