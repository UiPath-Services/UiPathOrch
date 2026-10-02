using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchPackage", SupportsShouldProcess = true)]
public class RemovePackageCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(VersionsCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PathCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // Exclude already-selected Id values from the candidates
            var wpId = CreateSelfExclusionList(commandAst, "Id", wordToComplete);

            // Only target the Version values selected via parameters
            var wpVersion = GetFakeBoundParameters(fakeBoundParameters, "Version").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

            foreach (var result in results)
            {
                foreach (var package in result
                    .Where(m => wp.IsMatch(m.Id))
                    .ExcludeByWildcards(p => p?.Id, wpId)
                    .FilterByWildcards(p => p?.Version, wpVersion)
                    .OrderBy(m => m.Id))
                {
                    string tiphelp = TipHelp(package);
                    yield return new CompletionResult(PathTools.EscapePSText(package.Id), package.Id, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class VersionsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // Only target the Id values selected via parameters
            var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();

            // Exclude already-selected Version values from the candidates
            var wpVersion = CreateSelfExclusionList(commandAst, "Version", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                var packages = drive.GetPackages(folder).FilterByWildcards(p => p?.Id, wpId);
                return ParallelResults.GroupBy(packages, package =>
                    drive.GetPackageVersions(folder, package.Id!));
            });

            foreach (var result in results)
            {
                foreach (var package in result)
                {
                    foreach (var version in package
                        .Where(v => wp.IsMatch(v.Version!))
                        .ExcludeByWildcards(v => v?.Version, wpVersion))
                    //.OrderBy(v => v.Version!, VersionComparer.Instance))
                    {
                        string tiphelp = TipHelp(version);
                        yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    private class PathCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = SessionState.EnumAllOrchDrives()
                .Where(d => d.OrchAPISession.AuthManager.IsAuthenticated);

            var feedFolders = SessionState.EnumPackageFeedFolders(drives.SelectMany(d => new[] { $"{d.Name}:{System.IO.Path.DirectorySeparatorChar}", $"{d.Name}:{System.IO.Path.DirectorySeparatorChar}*" }))
                .Select(df => df.folder.GetPSPath());

            // Exclude already-selected Path values from the candidates
            var wpPath = CreateSelfExclusionList(commandAst, "Path", wordToComplete);

            // Also exclude already-selected Destination values from the candidates
            var wpDestination = GetFakeBoundParameters(fakeBoundParameters, "Destination").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var path in feedFolders
                .Where(path => wp.IsMatch(path))
                .ExcludeByWildcards(path => path, wpPath)
                .ExcludeByWildcards(path => path, wpDestination))
            {
                yield return new CompletionResult(PathTools.EscapePSText(path));
            }
        }
    }

    // Orchestrator's "Package is referred in active processes and cannot be deleted."
    private const int PackageReferredErrorCode = 1013;

    // feed-owning folder -> (package id, version) -> the processes that run it. Built on the
    // FIRST refusal for that folder and kept, because "Remove-OrchPackage * *" is refused
    // once per version and the answer does not change between them.
    private readonly Dictionary<string, (Dictionary<(string, string), List<string>> Map, int Unlistable)> _referrers = new();

    /// <summary>
    /// The "who is using it" half of a refusal, appended after the server's own words.
    ///
    /// Only for error 1013, and keyed on the code rather than the message: see
    /// OrchException.ExtractErrorCode. For anything else this returns "" and the error reads
    /// exactly as it did before.
    ///
    /// Searched is the feed-owning FOLDER AND ITS SUBFOLDERS, not this command's scope and not
    /// the whole drive. A package lives in a feed, and only a process in a folder served by
    /// that feed can name it -- for a folder feed that is the owning folder's subtree, and for
    /// the tenant feed the owning folder is the drive root, so the same rule widens to the
    /// tenant on its own with no special case. Searching the command's own scope instead would
    /// reliably find nothing: it walks feed-owning folders, and processes live in ordinary
    /// ones. One listing per candidate folder, cached, and only ever on the path where a
    /// delete has already been refused.
    /// </summary>
    private string ReferrerNote(OrchDriveInfo drive, Entities.Folder feedFolder, Exception ex, string packageId, string version, CancellationToken token)
    {
        if (OrchException.ExtractErrorCode(ex) != PackageReferredErrorCode) return "";

        try
        {
            var (map, unlistable) = ReferrersOf(drive, feedFolder, token);

            if (map.TryGetValue((packageId, version), out var processes) && processes.Count > 0)
            {
                return System.Environment.NewLine + "  Referred by: " + string.Join(", ", processes);
            }

            // Say that we looked and found nothing. Silence here reads the same as not
            // having looked, and sends the operator off deleting folders to find out.
            string unlisted = unlistable > 0
                ? $" ({unlistable} folder(s) could not be listed and were not searched)"
                : "";
            return System.Environment.NewLine +
                $"  No process under {feedFolder.GetPSPath()} refers to it{unlisted}.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // The note is a courtesy. Losing it must not cost the real error.
            return "";
        }
    }

    private (Dictionary<(string, string), List<string>> Map, int Unlistable) ReferrersOf(
        OrchDriveInfo drive, Entities.Folder feedFolder, CancellationToken token)
    {
        string cacheKey = feedFolder.GetPSPath();
        if (_referrers.TryGetValue(cacheKey, out var cached)) return cached;

        var map = new Dictionary<(string, string), List<string>>();
        int unlistable = 0;

        // The feed's reach. For the tenant feed the owning folder IS the root, so this is
        // every folder; for a folder feed it is that folder's subtree and nothing else.
        var all = drive.GetFolders();
        var folders = (feedFolder == drive.RootFolder
            ? all
            : all.Where(f => f == feedFolder || IsUnder(f, feedFolder))).ToList();

        // Its own bar, with an allocated id so it cannot land on the two the removal is
        // already showing. On a large tenant this is one listing per folder, and the
        // operator is looking at a failed delete wondering what the cmdlet is doing now.
        using (var reporter = new ProgressReporter(this, folders.Count, "Referrers"))
        {
            int index = 0;
            foreach (var folder in folders)
            {
                token.ThrowIfCancellationRequested();
                reporter.WriteProgress(++index, folder.GetPSPath());

                try
                {
                    foreach (var release in drive.Releases.Get(folder) ?? [])
                    {
                        if (string.IsNullOrEmpty(release?.ProcessKey)) continue;

                        // Keyed by version too: the refusal is about one version, and naming
                        // a process that pins a different one would be a wrong answer
                        // delivered confidently.
                        var key = (release.ProcessKey!, release.ProcessVersion ?? "");
                        if (!map.TryGetValue(key, out var processes))
                        {
                            map[key] = processes = [];
                        }
                        processes.Add(System.IO.Path.Combine(folder.GetPSPath(), release.Name ?? ""));
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // No permission, most often. Counted, so the "found nothing" line can
                    // admit that the search had a hole in it rather than claiming the
                    // tenant is clean.
                    unlistable++;
                }
            }
        }

        cached = (map, unlistable);
        _referrers[cacheKey] = cached;
        return cached;
    }

    // Path-prefix rather than a walk up ParentId: GetFolders() already hands back every
    // folder with its full path, and the separator guard keeps "Finance2" from counting as
    // a child of "Finance".
    private static bool IsUnder(Entities.Folder candidate, Entities.Folder ancestor)
    {
        string prefix = ancestor.GetPSPath();
        if (prefix.Length == 0) return true;
        if (prefix[^1] != System.IO.Path.DirectorySeparatorChar)
        {
            prefix += System.IO.Path.DirectorySeparatorChar;
        }
        return candidate.GetPSPath().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);

        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                // Two bars, the same pair as Remove-OrchLibrary: the top one names the
                // package, the one nested under it the version being removed. This was a
                // single bar carrying both in its ACTIVITY -- "Removing versions of <id> in
                // <folder>" -- so the label was a different width for every package and the
                // bar walked left and right as the run went on. Labels are short, hardcoded
                // and of one length (8).
                var packages = drive.GetPackages(folder)
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower())
                    .ToList();

                int packageIndex = 0;
                using var packageReporter = new ProgressReporter(this, packages.Count, "Packages");

                foreach (var package in packages)
                {
                    packageReporter.WriteProgress(++packageIndex, $"{folder.GetPSPath()} {package.Id}");
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            //.OrderBy(v => v.Version!, VersionComparer.Instance)
                            .ToList();

                        // Disposed at the end of each package, so the bar belongs to the
                        // package above it rather than accumulating across the run.
                        int versionIndex = 0;
                        using var versionReporter = new ProgressReporter(this, versions.Count, "Versions", packageReporter);

                        foreach (var version in versions)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();
                            versionReporter.WriteProgress(++versionIndex, version.Version);

                            string target = $"{version.GetPSPath()}:{version.Version}";
                            if (ShouldProcess(target, "Remove Package"))
                            {
                                try
                                {
                                    string feedId = drive.FolderFeedId.Get(folder);
                                    drive.OrchAPISession.RemovePackage(version.Id!, version.Version!, feedId);
                                    drive.Packages.ClearCache(feedId ?? "");
                                    // Drop all cached version lists for this feed (the original
                                    // code did the same — the per-packageId TryRemove was
                                    // redundant because the next line cleared the whole feedId).
                                    drive.PackageVersions.ClearCache(k => k.feedId == (feedId ?? ""));
                                }
                                catch (Exception ex)
                                {
                                    var errorRecord = new ErrorRecord(
                                        new OrchException(target, ex, ReferrerNote(drive, folder, ex, package.Id!, version.Version!, cancelHandler.Token)),
                                        "RemovePackageError", ErrorCategory.InvalidOperation, version);
                                    WriteError(errorRecord);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
