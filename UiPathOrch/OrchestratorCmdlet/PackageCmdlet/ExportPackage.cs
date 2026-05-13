using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchPackage", SupportsShouldProcess = true)]
public class ExportPackageCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(IdCompleter))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(VersionCompleter))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
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
            var paramVersion = GetFakeBoundParameters(fakeBoundParameters, "Version").ToList();
            var wpVersion = paramVersion.Select(ver => new WildcardPattern(ver, WildcardOptions.IgnoreCase)).ToList();

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

    private class VersionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
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
                var packages = drive.GetPackages(folder)
                    .FilterByWildcards(p => p?.Id, wpId);
                return ParallelResults.GroupBy(packages, package =>
                    drive.GetPackageVersions(folder, package.Id!));
            });

            foreach (var result in results)
            {
                foreach (var package in result)
                {
                    foreach (var version in package
                        .Where(v => wp.IsMatch(v.Version))
                        .ExcludeByWildcards(v => v?.Version, wpVersion))
                    //.OrderBy(v => v.Version!, VersionComparer.Instance))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(version.Version));
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(Path, Recurse.IsPresent);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        if (Destination is null)
        {
            Destination = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        // Convert the PSDrive path to the actual file system path
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        if (!Directory.Exists(Destination))
        {
            throw new DirectoryNotFoundException($"A directory '{Destination}' does not exist.");
        }

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export packages");
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var packages = drive.GetPackages(folder);

                string feedFolder = folder.GetPackageFeedFolder();
                string folderDisplayName = feedFolder.MakeValidFolderName();

                string feedId = drive.FolderFeedId.Get(folder);

                string destination;
                if (string.IsNullOrEmpty(feedFolder))
                {
                    destination = Destination;
                }
                else
                {
                    destination = System.IO.Path.Combine(Destination, folderDisplayName);
                }

                foreach (var package in packages!
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower()).WithCancellation(cancelHandler.Token))
                {
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            //.OrderBy(v => v.Version!, VersionComparer.Instance)
                            .ToList();

                        int index = 0;
                        reporter.TotalNum = versions!.Count;
                        foreach (var version in versions!.WithCancellation(cancelHandler.Token))
                        {
                            string target = version.GetPSPath() + ':' + version.Version;
                            reporter.WriteProgress(++index, target);
                            if (ShouldProcess(target, "Export Package"))
                            {
                                try
                                {
                                    Directory.CreateDirectory(destination);
                                    var (fileName, fileContent) = drive.OrchAPISession.DownloadPackage(feedId!, version.Id!, version.Version!);
                                    string filePath = System.IO.Path.Combine(destination, fileName!);
                                    File.WriteAllBytes(filePath, fileContent);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "ExportPackageError", ErrorCategory.InvalidOperation, version));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(package.GetPSPath(), ex), "GetPackageVersionError", ErrorCategory.InvalidOperation, package));
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
