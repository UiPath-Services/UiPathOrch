using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Cross-checks the module manifest (Staging/UiPathOrch.psd1) against the
// cmdlet classes actually compiled into the assembly. The v1.5.x release
// cycle added a dozen cmdlets one at a time, each needing a manifest
// entry; the failure mode is "cmdlet ships in the DLL but is missing from
// CmdletsToExport, so Install-Module users can't see it" (the same bug
// that hid Remove-OrchAssetLink for a release). These tests make that
// class of omission a CI failure.
public class ModuleManifestSanityTests
{
    private static string LocateModuleManifest()
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "Staging", "UiPathOrch.psd1");
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException(
            "Staging/UiPathOrch.psd1 not found above " + System.AppContext.BaseDirectory);
    }

    private static string LocateRepoFile(string fileName)
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, fileName);
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException($"{fileName} not found above " + System.AppContext.BaseDirectory);
    }

    // Guard against the "bumped ModuleVersion but forgot the release notes"
    // drift that shipped stale PSData.ReleaseNotes through 1.5.1 / 1.5.2 (the
    // PSGallery page showed the previous version's notes). The manifest
    // version, its PSData.ReleaseNotes, and the CHANGELOG must all agree on
    // the current version — bumping the version without touching the other
    // two is now a CI failure, not a silent stale-notes release.
    [Fact]
    public void ModuleVersion_HasMatchingReleaseNotes_AndChangelogEntry()
    {
        var psd1 = System.IO.File.ReadAllText(LocateModuleManifest());

        var version = Regex.Match(psd1, @"ModuleVersion\s*=\s*'([^']+)'").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(version), "ModuleVersion not found in the manifest.");

        var rn = Regex.Match(psd1, @"ReleaseNotes\s*=\s*@'\r?\n(.*?)\r?\n'@", RegexOptions.Singleline);
        Assert.True(rn.Success, "PSData.ReleaseNotes here-string not found in the manifest.");
        Assert.Contains(version, rn.Groups[1].Value, System.StringComparison.Ordinal);

        var changelog = System.IO.File.ReadAllText(LocateRepoFile("CHANGELOG.md"));
        Assert.Contains($"## [{version}]", changelog, System.StringComparison.Ordinal);
    }

    private static string ManifestText => System.IO.File.ReadAllText(LocateModuleManifest());

    // All concrete cmdlet classes in the UiPathOrch assembly, mapped to
    // their "Verb-Noun" exported names.
    private static System.Collections.Generic.IEnumerable<string> AllCmdletExportNames()
    {
        var asm = typeof(OrchProvider).Assembly;
        foreach (var t in asm.GetTypes())
        {
            if (t.IsAbstract) continue;
            var attr = t.GetCustomAttribute<CmdletAttribute>();
            if (attr is null) continue;
            yield return $"{attr.VerbName}-{attr.NounName}";
        }
    }

    // Cmdlets deliberately compiled but NOT exported — work-in-progress or
    // internal-only features. Surfaced by EveryCompiledCmdlet_IsListedInManifest
    // on 2026-05-22; left unexported intentionally. If a v1.5.x (or later)
    // cmdlet ever lands here it means the manifest entry was forgotten —
    // remove it from this allowlist and add it to CmdletsToExport instead.
    private static readonly System.Collections.Generic.HashSet<string> IntentionallyUnexported = new(System.StringComparer.OrdinalIgnoreCase)
    {
        // Business Rules — feature not yet shipped publicly.
        "Get-OrchBusinessRule", "New-OrchBusinessRule", "Update-OrchBusinessRule", "Remove-OrchBusinessRule",
        // Data Fabric (Df) — preview surface.
        "Get-OrchDfEntity", "Get-OrchDfRecord", "Invoke-OrchDfQuery",
        // Integration Service connections — preview.
        "Get-OrchConnection",
        // Test Manager (Tm) auxiliary cmdlets — not part of the public surface.
        "Get-TmDefect", "Get-TmRole", "Get-TmTestExecutionResult",
        // Platform Management licensing helper — implementation complete
        // (PUT /UserLicense with userIds + licenseCodes, ShouldProcess gated),
        // held back from the public surface until the PUT is live-verified on
        // a test tenant. Remove from this allowlist and add to
        // CmdletsToExport in the psd1 once verified.
        "Add-PmLicenseToPmLicensedUser",
        // Classic robot enable — incomplete implementation; Classic robots deprecated.
        "Enable-OrchClassicRobot",
    };

    [Fact]
    public void EveryCompiledCmdlet_IsListedInManifest_OrIntentionallyUnexported()
    {
        var manifest = ManifestText;
        var missing = AllCmdletExportNames()
            .Distinct()
            .Where(name => !manifest.Contains($"'{name}'"))
            .Where(name => !IntentionallyUnexported.Contains(name))
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            "These cmdlets are compiled into the assembly but missing from " +
            "Staging/UiPathOrch.psd1 CmdletsToExport (Install-Module users won't see them). " +
            "Either add a manifest entry, or — if intentionally hidden — add to the " +
            "IntentionallyUnexported allowlist in this test with a reason:\n  " +
            string.Join("\n  ", missing));
    }

    [Theory]
    // The v1.5.x additions, explicitly enumerated so a regression on any
    // one of them is named in the failure rather than buried in the
    // bulk check above.
    [InlineData("New-OrchApiTrigger")]
    [InlineData("Update-OrchApiTrigger")]
    [InlineData("New-OrchTestSet")]
    [InlineData("Get-OrchTestSetDetail")]
    [InlineData("New-OrchTestSetSchedule")]
    [InlineData("Update-OrchTestSetSchedule")]
    [InlineData("New-OrchTestDataQueue")]
    [InlineData("New-OrchActionCatalog")]
    [InlineData("New-OrchWebhook")]
    public void V15Cmdlet_IsExported(string name)
    {
        Assert.Contains($"'{name}'", ManifestText);
    }

    [Fact]
    public void ManifestModuleVersion_IsParseable()
    {
        // A malformed ModuleVersion would make Import-Module fail outright.
        var m = Regex.Match(ManifestText, @"ModuleVersion\s*=\s*'(?<v>[^']+)'");
        Assert.True(m.Success, "ModuleVersion line not found in manifest.");
        Assert.True(System.Version.TryParse(m.Groups["v"].Value, out _),
            $"ModuleVersion '{m.Groups["v"].Value}' is not a parseable version.");
    }
}
