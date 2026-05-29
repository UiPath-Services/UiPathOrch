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

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(Path, Recurse.IsPresent);

        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var packages = drive.GetPackages(folder);

                foreach (var package in packages
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!);

                        foreach (var version in versions
                            .FilterByWildcards(v => v?.Version, wpVersion))
                        //.OrderBy(v => v.Version!, VersionComparer.Instance))
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

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
                                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemovePackageError", ErrorCategory.InvalidOperation, version);
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
