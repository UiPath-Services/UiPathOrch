using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Shared plumbing for the -Dependency and -Workflow completers.
///
/// Both offer what the package cache already holds. Deliberately passive about the packages:
/// a fetch here would download a `.nupkg` per package in scope, which is not something tab
/// completion may do. Listing the folder's processes is another matter — that is the same
/// cheap OData list ProcessNameCompleter already makes — and it is what tells us whether the
/// cache covers the scope or only part of it.
///
/// When a package in scope has not been read yet, the first completion offered says so and
/// names the cmdlet to run, rather than letting a half-populated list look complete. Inserting
/// it is harmless: it completes to the word already typed.
/// </summary>
internal abstract class PackageContentsCompleterBase : OrchArgumentCompleter
{
    /// <summary>The candidates to offer, as text -> tooltip, from the cached packages.</summary>
    protected abstract SortedDictionary<string, string> Collect(IEnumerable<OrchDriveInfo> drives);

    /// <summary>Named in the "run this first" hint.</summary>
    protected abstract string PrimingCmdlet { get; }

    /// <summary>
    /// The parameters that narrow which packages the command will read (-Name on the process
    /// cmdlets, -Id / -Version on the feed ones). They decide both what the coverage count
    /// means and what the hint's suggested command has to carry: counting the whole scope
    /// while the user typed `-Recurse Autopilot` reported 212 packages for a query that reads
    /// a handful.
    /// </summary>
    protected abstract string[] ScopeParameterNames { get; }

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters).ToList();
        var drives = drivesFolders.Select(df => df.drive).Distinct().ToList();

        if (!IsScopeFullyCached(drivesFolders, fakeBoundParameters, out int missing, out int total, out int unlistedFolders))
        {
            yield return HintResult(commandName, fakeBoundParameters, missing, total, unlistedFolders);
        }

        // Exclude the values already given on the command line from the candidates.
        var wpSelf = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        foreach (var (text, tiphelp) in Collect(drives).ExcludeByWildcards(e => e.Key, wpSelf))
        {
            yield return new CompletionResult(PathTools.EscapePSText(text), text, CompletionResultType.Text, tiphelp);
        }
    }

    /// <summary>
    /// True when every package version the processes in scope name is already in the cache.
    ///
    /// Entirely passive — no API call, however wide the scope. A first draft asked each folder
    /// for its releases, which on a tenant-wide `-Recurse` meant one round trip per folder
    /// before a single candidate appeared, and Tab sat there for seconds.
    ///
    /// A folder in scope that has not been listed still counts as not covered — otherwise
    /// priming one folder would silence the hint for a `-Recurse` over eighty. The exception
    /// is a folder whose listing is known to fail (no permission — eleven of them on the
    /// tenant this was written against): those are skipped, because no amount of priming will
    /// ever cache them and counting them kept the hint on screen forever.
    /// </summary>
    private bool IsScopeFullyCached(
        List<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        IDictionary fakeBoundParameters,
        out int missing, out int total, out int unlistedFolders)
    {
        missing = 0;
        total = 0;
        unlistedFolders = 0;

        // The same filters the command itself will apply, so the count describes the query
        // the user is typing rather than everything in the folders.
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
        var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();

        var cachedByDrive = new Dictionary<string, HashSet<(string, string, string)>>();

        foreach (var (drive, folder) in drivesFolders)
        {
            if (!cachedByDrive.TryGetValue(drive.NameColonSeparator, out var cached))
            {
                cached = drive.PackageContents.CachedEntries.Select(e => e.Key).ToHashSet();
                cachedByDrive[drive.NameColonSeparator] = cached;
            }

            var releases = drive.Releases.CachedValue(folder);
            if (releases is null)
            {
                // Not listable at all (no permission, say): nothing to prime, so it must not
                // hold the hint open. Not listed yet: counted as a folder, not as packages --
                // how many packages it holds is precisely what is not known yet.
                if (drive.Releases.HasCachedException(folder)) continue;

                unlistedFolders++;
                continue;
            }

            drive.FolderFeedId.TryGetCachedValue(folder, out var feedId);

            foreach (var release in releases
                .FilterByWildcards(r => r?.Name, wpName)
                .FilterByWildcards(r => r?.ProcessKey, wpId))
            {
                if (string.IsNullOrEmpty(release?.ProcessKey) || string.IsNullOrEmpty(release.ProcessVersion)) continue;

                total++;
                if (!cached.Contains((feedId ?? "", release.ProcessKey!, release.ProcessVersion!))) missing++;
            }
        }

        return missing == 0 && unlistedFolders == 0;
    }

    private CompletionResult HintResult(
        string commandName, IDictionary fakeBoundParameters, int missing, int total, int unlistedFolders)
    {
        // The message IS the completion, the way the directory-search completers say "Please
        // enter at least one character to search." (OrchCompleter.cs). PowerShell inserts a
        // lone candidate straight away rather than showing its list item, so a hint that
        // completed to anything else would replace itself with a value and say nothing.
        string prime = PrimingCommand(commandName, fakeBoundParameters);

        // Folders nobody has listed are reported as folders: how many packages they hold is
        // exactly what is not known yet, so they cannot be added to a package count.
        string state = (total, missing, unlistedFolders) switch
        {
            (0, _, 0) => "Nothing cached yet.",
            (0, _, var f) => $"Nothing cached yet, and {f} folder(s) not listed yet.",
            (_, 0, var f) => $"All {total} packages in the listed folders are cached, but {f} folder(s) are not listed yet.",
            (_, _, 0) => $"Only {total - missing} of {total} packages cached.",
            (_, _, var f) => $"Only {total - missing} of {total} packages cached, and {f} folder(s) not listed yet.",
        };

        return new CompletionResult(PathTools.EscapePSText($"{state} Run {prime} first for the full list."));
    }

    /// <summary>
    /// The command to suggest: the one being completed, carrying the scope already typed.
    /// A bare "run Get-OrchProcessDependency first" is wrong advice under `-Recurse` — it
    /// primes one folder while the completion covers the whole tree — so -Path, -Recurse and
    /// -Depth are echoed back.
    /// </summary>
    private string PrimingCommand(string commandName, IDictionary fakeBoundParameters)
    {
        var command = new System.Text.StringBuilder(string.IsNullOrEmpty(commandName) ? PrimingCmdlet : commandName);

        var paths = GetFakeBoundParameters(fakeBoundParameters, "Path")?.ToList();
        if (paths is { Count: > 0 })
        {
            command.Append(" -Path ").Append(string.Join(", ", paths));
        }

        if (ResolveSwitchParameter(fakeBoundParameters, "Recurse")) command.Append(" -Recurse");

        var depth = ResolveDepth(fakeBoundParameters);
        if (depth > 0) command.Append(" -Depth ").Append(depth);

        // The narrowing parameters too: priming the whole tenant when the user asked about
        // one process is far more work than the question needs.
        foreach (var name in ScopeParameterNames)
        {
            var values = GetFakeBoundParameters(fakeBoundParameters, name)?.ToList();
            if (values is { Count: > 0 })
            {
                command.Append(" -").Append(name).Append(' ').Append(string.Join(", ", values));
            }
        }

        return command.ToString();
    }
}

/// <summary>Completes -Dependency from the package contents already in the cache.</summary>
internal class PackageDependencyCompleter : PackageContentsCompleterBase
{
    protected override string PrimingCmdlet => "Get-OrchProcessDependency";

    // -Name on the process cmdlets, -Id on the feed ones; whichever the command has is the
    // one bound, so reading for both keeps one completer serving both.
    protected override string[] ScopeParameterNames => ["Name", "Id", "Version"];

    protected override SortedDictionary<string, string> Collect(IEnumerable<OrchDriveInfo> drives)
    {
        // id -> how many package versions declare it, and the range of ranges they ask for.
        var seen = new SortedDictionary<string, (int Packages, SortedSet<string> Ranges)>(StringComparer.OrdinalIgnoreCase);

        foreach (var drive in drives)
        {
            foreach (var entry in drive.PackageContents.CachedEntries)
            {
                foreach (var dependency in entry.Value?.Dependencies ?? [])
                {
                    if (string.IsNullOrEmpty(dependency.Dependency)) continue;

                    if (!seen.TryGetValue(dependency.Dependency!, out var info))
                    {
                        info = (0, new SortedSet<string>(StringComparer.OrdinalIgnoreCase));
                    }
                    if (!string.IsNullOrEmpty(dependency.Range)) info.Ranges.Add(dependency.Range!);
                    seen[dependency.Dependency!] = (info.Packages + 1, info.Ranges);
                }
            }
        }

        var result = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, info) in seen)
        {
            string ranges = info.Ranges.Count switch
            {
                0 => "",
                1 => $" {info.Ranges.First()}",
                _ => $" {info.Ranges.First()} .. {info.Ranges.Last()}",
            };
            result[id] = $"{id}  ({info.Packages} package version(s)){ranges}";
        }
        return result;
    }
}

/// <summary>Completes -Workflow from the package contents already in the cache.</summary>
internal class PackageWorkflowCompleter : PackageContentsCompleterBase
{
    protected override string PrimingCmdlet => "Get-OrchProcessWorkflow";

    protected override string[] ScopeParameterNames => ["Name", "Id", "Version"];

    protected override SortedDictionary<string, string> Collect(IEnumerable<OrchDriveInfo> drives)
    {
        var seen = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var drive in drives)
        {
            foreach (var entry in drive.PackageContents.CachedEntries)
            {
                foreach (var workflow in entry.Value?.Workflows ?? [])
                {
                    if (string.IsNullOrEmpty(workflow.Workflow)) continue;
                    seen[workflow.Workflow!] = seen.GetValueOrDefault(workflow.Workflow!) + 1;
                }
            }
        }

        var result = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, count) in seen)
        {
            result[path] = $"{path}  ({count} package version(s))";
        }
        return result;
    }
}
