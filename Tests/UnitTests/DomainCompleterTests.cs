using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the -Domain completer (DomainCompleter), added alongside
// Add-OrchUser -Domain / Add-OrchFolderUser -Domain.
//
// Two things can't be exercised end-to-end without an EntraID-federated
// OnPrem tenant (there is none in the test env — the reporting customer runs
// on FastRetailing's OnPrem, which we have no fixture for):
//   1. the /api/DirectoryService/GetDomains wire contract, and
//   2. that frc/root render with the isDefault domain surfaced first.
// Non-federated tenants / Automation Cloud return [] so the completer is
// silently empty there (verified live, Orch1 2026-06-29). So we pin (1) by
// deserializing the captured HAR payload and (2) by testing the pure
// ordering/rendering helper directly.
public class DomainCompleterTests
{
    // The exact GetDomains response captured in the FR HAR (dev-rpa.fastretailing.com, 2026-06-23).
    private const string HarPayload =
        "[{\"name\":\"frc\",\"isDefault\":true},{\"name\":\"root\",\"isDefault\":false}]";

    [Fact]
    public void Deserializes_GetDomains_HarPayload()
    {
        // Strict (case-sensitive) options: the lowercase field names must bind
        // the wire JSON exactly, independent of the module's serializer config.
        var domains = JsonSerializer.Deserialize<DirectoryDomain[]>(HarPayload);

        Assert.NotNull(domains);
        Assert.Equal(2, domains!.Length);

        var frc = domains.Single(d => d.name == "frc");
        var root = domains.Single(d => d.name == "root");
        Assert.True(frc.isDefault);
        Assert.False(root.isDefault);
    }

    private static List<System.Management.Automation.CompletionResult> Build(
        IEnumerable<DirectoryDomain> domains, string? word)
        => DomainCompleter.BuildDomainCompletions(domains, word).ToList();

    private static DirectoryDomain D(string? name, bool? isDefault = null)
        => new() { name = name, isDefault = isDefault };

    [Fact]
    public void DefaultDomain_SurfacesFirst_RegardlessOfInputOrder()
    {
        // root listed first in the input, frc flagged default -> frc must lead.
        var results = Build([D("root", false), D("frc", true)], "*");
        Assert.Equal(new[] { "frc", "root" }, results.Select(r => r.ListItemText).ToArray());
    }

    [Fact]
    public void Tooltip_MarksOnlyTheDefault()
    {
        var results = Build([D("frc", true), D("root", false)], "*");
        Assert.Contains("(default)", results.Single(r => r.ListItemText == "frc").ToolTip);
        Assert.DoesNotContain("(default)", results.Single(r => r.ListItemText == "root").ToolTip);
    }

    [Fact]
    public void PrefixWord_FiltersByName()
    {
        // CreateWPFromWordToComplete appends '*' to a plain word -> "ro" => "ro*".
        var results = Build([D("frc", true), D("root", false)], "ro");
        Assert.Equal(new[] { "root" }, results.Select(r => r.ListItemText).ToArray());
    }

    [Fact]
    public void BlankNames_AreDropped()
    {
        var results = Build([D("frc", true), D("", false), D(null, false)], "*");
        Assert.Equal(new[] { "frc" }, results.Select(r => r.ListItemText).ToArray());
    }

    [Fact]
    public void EmptyInput_YieldsNoCandidates()
    {
        Assert.Empty(Build([], "*"));
        Assert.Empty(Build(System.Array.Empty<DirectoryDomain>(), null));
    }
}
