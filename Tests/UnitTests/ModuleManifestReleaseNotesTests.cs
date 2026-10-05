using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
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

    // Read the manifest the way Import-PowerShellDataFile does -- parse it, then take the value of
    // its one hashtable with SafeGetValue, which accepts only literals -- but without a runspace.
    //
    // It used to call Import-PowerShellDataFile through PowerShell.Create(), and that failed on the
    // v1.19.0 release run with "Cannot find drive. A drive with the name 'D' does not exist." while
    // the CI run of the same commit, on the same runner image, passed. A runspace resolves -Path
    // through the FileSystem provider's drives, and the first runspace of a test process was being
    // created while OrchProviderHarness built others in parallel test classes. Parsing needs no
    // provider, no drive and no runspace, so there is nothing left to race.
    private static Hashtable Manifest()
    {
        Assert.True(File.Exists(ManifestPath), $"not found: {ManifestPath}");

        var ast = Parser.ParseFile(ManifestPath, out _, out ParseError[] errors);
        Assert.True(errors.Length == 0,
            "the module manifest does not parse: " + string.Join("; ", errors.Select(e => e.Message)));

        var table = ast.Find(a => a is HashtableAst, searchNestedScriptBlocks: false) as HashtableAst;
        Assert.NotNull(table);

        return (Hashtable)table!.SafeGetValue();
    }

    private static string ReleaseNotes()
    {
        var privateData = (Hashtable)Manifest()["PrivateData"]!;
        var psData = (Hashtable)privateData["PSData"]!;

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
        string version = (string)Manifest()["ModuleVersion"]!;

        Assert.StartsWith(version, notes.TrimStart());
    }
}
