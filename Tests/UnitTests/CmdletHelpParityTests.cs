using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Help-page parity: every command UiPathOrch exports (CmdletsToExport +
// FunctionsToExport in Staging/UiPathOrch.psd1) must ship a PlatyPS help page
// in docs/help/en-US, and there must be no orphan pages. The release pipeline
// builds MAML from these .md files, so a missing page = a shipped command with
// empty Get-Help; an orphan page = help for a command users can't run.
//
// Adding a command touches several sites by hand (DTO, session, cache, cmdlet,
// manifest, help). ModuleManifestSanityTests already makes "forgot the manifest
// entry" a CI failure; this makes "forgot the help page" one too — closing the
// other half of the multi-site extension gap without code generation.
//
// Comparison is case-SENSITIVE (Ordinal): CI builds and tests on Linux/macOS,
// where help-file lookup is case-sensitive, so a page whose casing drifts from
// the command name would break Get-Help there and must fail here.
public class CmdletHelpParityTests
{
    private static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Staging", "UiPathOrch.psd1")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Repo root (with Staging/UiPathOrch.psd1) not found above " + AppContext.BaseDirectory);
    }

    private static List<string> ParseExportArray(string manifest, string key)
    {
        // The export arrays hold only quoted 'Verb-Noun' names (and blank lines),
        // no inner parens, so the first ')' after '@(' is the closing paren.
        var block = Regex.Match(manifest, key + @"\s*=\s*@\((?<body>.*?)\)", RegexOptions.Singleline);
        Assert.True(block.Success, $"{key} = @( ... ) array not found in the manifest.");
        return Regex.Matches(block.Groups["body"].Value, "'([^']+)'")
                    .Select(m => m.Groups[1].Value)
                    .ToList();
    }

    private static (HashSet<string> exported, HashSet<string> helpPages) Load()
    {
        var root = LocateRepoRoot();
        var manifest = File.ReadAllText(Path.Combine(root, "Staging", "UiPathOrch.psd1"));

        var exported = new HashSet<string>(
            ParseExportArray(manifest, "CmdletsToExport")
                .Concat(ParseExportArray(manifest, "FunctionsToExport")),
            StringComparer.Ordinal);

        var helpDir = Path.Combine(root, "docs", "help", "en-US");
        Assert.True(Directory.Exists(helpDir), "Help directory docs/help/en-US not found.");
        var helpPages = new HashSet<string>(
            Directory.EnumerateFiles(helpDir, "*.md").Select(p => Path.GetFileNameWithoutExtension(p)!),
            StringComparer.Ordinal);

        return (exported, helpPages);
    }

    [Fact]
    public void Every_exported_command_has_a_help_page()
    {
        var (exported, helpPages) = Load();
        var missing = exported.Where(c => !helpPages.Contains(c)).OrderBy(c => c, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            "These commands are exported (CmdletsToExport / FunctionsToExport) but have no " +
            "docs/help/en-US/<name>.md page — Get-Help would be empty and the MAML build skips " +
            "them. Add the PlatyPS page:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void No_orphan_help_pages()
    {
        var (exported, helpPages) = Load();
        var orphan = helpPages.Where(p => !exported.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToList();
        Assert.True(orphan.Count == 0,
            "These docs/help/en-US/*.md pages map to no exported command (CmdletsToExport / " +
            "FunctionsToExport) — stale help for a removed/renamed command, or a casing drift " +
            "that breaks Get-Help on Linux/macOS:\n  " + string.Join("\n  ", orphan));
    }
}
