using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchPackage", SupportsShouldProcess = true)]
public class ExportPackageCommand : OrchestratorPSCmdlet
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
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // Exclude already-selected Id values from the candidates
            var wpId = CreateWPListFromParameter(commandAst, "Id", TPositional.Parameters, wordToComplete);

            // Only target the Version values selected via parameters
            var paramVersion = GetParameterValues(commandAst, "Version", TPositional.Parameters).ToList();
            var wpVersion = paramVersion.Select(ver => new WildcardPattern(ver, WildcardOptions.IgnoreCase)).ToList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

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
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

            // Only target the Id values selected via parameters
            var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

            // Exclude already-selected Version values from the candidates
            var wpVersion = CreateWPListFromParameter(commandAst, "Version", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                var packages = drive.GetPackages(folder)
                    .FilterByWildcards(p => p?.Id, wpId);
                return ParallelResults3.GroupBy(packages, package =>
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

        // Version that makes all API calls asynchronously upfront
        // It works correctly, but might result in too many API calls.
#if false
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var packages = df.drive.GetPackages(df.folder)
                    .FilterByWildcards(p => p.Id!, wpId)
                    .OrderBy(p => p.Id!.ToLower());

                return OrchThreadPool.RunForEach(packages,
                    package => package.GetPSPath(),
                    package => package,
                    package => df.drive.GetPackageVersions(df.folder, package.Id!)
                        .FilterByWildcards(v => v.Version!, wpVersion)
                        .OrderBy(v => v.Version!, VersionComparer.Instance));
            });

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export Package", "Export Package");
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult(cancelHandler.Token);
                var (drive, folder) = result.Source;

                string feedFolder = folder.GetPackageFeedFolder();
                string feedId = drive.GetFolderFeedId(folder);
                string folderDisplayName = OrchDriveInfo.MakeValidFolderName(feedFolder);

                string destination;
                if (string.IsNullOrEmpty(feedFolder))
                {
                    destination = Destination;
                }
                else
                {
                    destination = System.IO.Path.Combine(Destination, folderDisplayName);
                }

                foreach (var thread in threads!)
                {
                    try
                    {
                        var versions = thread.GetResult(cancelHandler.Token);

                        reporter.TotalNum = versions!.Count();
                        int index = 0;
                        foreach (var version in versions!)
                        {
                            string target;
                            if (string.IsNullOrEmpty(folderDisplayName))
                            {
                                target = drive.NameColonSeparator + version.Id + ':' + version.Version;
                            }
                            else
                            {
                                target = drive.NameColonSeparator + folderDisplayName + System.IO.Path.DirectorySeparatorChar + version.Id + ':' + version.Version;
                            }

                            reporter.WriteProgress(++index, $"{version.GetPSPath()}:{version.Version}");
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
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "ExportPackageError", ErrorCategory.InvalidOperation, target));
                                }
                            }
                        }
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
#endif

        // Version that only runs the initial GetPackage() asynchronously.
        // GetPackageVersion() is called just before download.
        // If there are many folder feeds, it may take a while before processing starts.
#if false
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.GetPackages(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, 100, "Export Package", "Export Package");
        foreach (var result in results)
        {
            try
            {
                var packages = result.GetResult(cancelHandler.Token);
                var (drive, folder) = result.Source;

                string feedFolder = folder.GetPackageFeedFolder();
                string folderDisplayName = OrchDriveInfo.MakeValidFolderName(feedFolder);

                string feedId = drive.GetFolderFeedId(folder);

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
                    .FilterByWildcards(p => p.Id!, wpId)
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v.Version!, wpVersion)
                            .OrderBy(v => v.Version!, VersionComparer.Instance).ToList();

                        int index = 0;
                        reporter.TotalNum = versions!.Count;
                        foreach (var version in versions!)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            string target = version.GetPSPath() + ':' + version.Version;
                            reporter.WriteProgress(++index, $"{index:D}/{versions.Count} {target}");
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
#endif

        // Fully single-threaded version
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
                    .OrderBy(p => p.Id!.ToLower()))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    try
                    {
                        var versions = drive.GetPackageVersions(folder, package.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            //.OrderBy(v => v.Version!, VersionComparer.Instance)
                            .ToList();

                        int index = 0;
                        reporter.TotalNum = versions!.Count;
                        foreach (var version in versions!)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

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
