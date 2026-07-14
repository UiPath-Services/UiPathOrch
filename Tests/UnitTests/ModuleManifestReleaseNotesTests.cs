using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using Xunit;

namespace UnitTests;

// Pins the module manifest's ReleaseNotes against the PowerShell Gallery's hard limit.
//
// The Gallery rejects a package whose ReleaseNotes exceed 10600 characters:
//
//     nuget.exe failed to push ... 400 (The package is invalid. The error encountered was:
//     'A package's ReleaseNotes property extracted from the PowerShell manifest may not be
//     more than 10600 characters long.')
//
// ReleaseNotes accumulates one block per version, so it grows every release until it trips this.
// It did, on v1.11.7 — and it surfaced at the LAST step of the release workflow, AFTER the tag was
// pushed, after build / format / test / signing all passed. Nothing had been published, but the tag
// had to be moved and the run redone.
//
// A test is the right place for that: it fails in seconds, on the bump commit, before any tag.
// When it goes red, drop the oldest version block from the manifest's ReleaseNotes — CHANGELOG.md
// holds the full history and the notes link to it.
public class ModuleManifestReleaseNotesTests
{
    // The Gallery's limit, verbatim from its own error message.
    private const int PSGalleryReleaseNotesLimit = 10600;

    private static string ManifestPath
    {
        get
        {
            string root = typeof(ModuleManifestReleaseNotesTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(a => a.Key == "RepoRoot").Value!;
            return Path.GetFullPath(Path.Combine(root, "Staging", "UiPathOrch.psd1"));
        }
    }

    private static string ReleaseNotes()
    {
        Assert.True(File.Exists(ManifestPath), $"not found: {ManifestPath}");

        using var ps = PowerShell.Create();
        ps.AddCommand("Import-PowerShellDataFile").AddParameter("Path", ManifestPath);
        var manifest = (System.Collections.Hashtable)ps.Invoke()[0].BaseObject;

        Assert.False(ps.HadErrors, "the module manifest does not parse");

        var privateData = (System.Collections.Hashtable)manifest["PrivateData"]!;
        var psData = (System.Collections.Hashtable)privateData["PSData"]!;

        return (string)psData["ReleaseNotes"]!;
    }

    [Fact]
    public void ReleaseNotes_fit_the_PSGallery_limit()
    {
        string notes = ReleaseNotes();

        Assert.True(
            notes.Length <= PSGalleryReleaseNotesLimit,
            $"ReleaseNotes is {notes.Length} characters; the PowerShell Gallery rejects anything over " +
            $"{PSGalleryReleaseNotesLimit} and the release workflow would fail at 'Publish to PSGallery', " +
            "with the tag already pushed. Drop the oldest version block from Staging\\UiPathOrch.psd1 — " +
            "CHANGELOG.md keeps the full history.");
    }

    [Fact]
    public void ReleaseNotes_lead_with_the_version_being_shipped()
    {
        // The Gallery shows these notes on the module's page, newest first. A bump that updated
        // ModuleVersion but not the notes would ship the previous version's text.
        string notes = ReleaseNotes();

        using var ps = PowerShell.Create();
        ps.AddCommand("Import-PowerShellDataFile").AddParameter("Path", ManifestPath);
        var manifest = (System.Collections.Hashtable)ps.Invoke()[0].BaseObject;
        string version = (string)manifest["ModuleVersion"]!;

        Assert.StartsWith(version, notes.TrimStart());
    }
}
