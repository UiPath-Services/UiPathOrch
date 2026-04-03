using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPackageVersion")]
[OutputType(typeof(Entities.Package))]
public class GetPackageVersionCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(VersionCompleter))]
    public string[]? Version { get; set; }

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
            var paramDepth = GetParameterValue(commandAst, "Depth");
            uint.TryParse(paramDepth, out uint depth);

            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);
            int totalFolderCount = drivesFolders.Count;

            // Exclude already-selected Id values from the candidates
            var wpId = CreateSelfExclusionList(commandAst, "Id", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

            foreach (var result in results)
            {
                foreach (var package in result
                    .Where(m => wp.IsMatch(m.Id))
                    .ExcludeByWildcards(p => p?.Id, wpId)
                    .OrderBy(l => l.Id))
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

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(Path, Recurse.IsPresent);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var packages = df.drive.GetPackages(df.folder)
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id!.ToLower());

                return OrchThreadPool.RunForEach(packages,
                    package => package.GetPSPath(),
                    package => package,
                    package => df.drive.GetPackageVersions(df.folder, package.Id!));
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult(cancelHandler.Token);

                foreach (var thread in threads!)
                {
                    try
                    {
                        var versions = thread.GetResult(cancelHandler.Token);
                        WriteObject(versions!
                            .FilterByWildcards(v => v?.Version, wpVersion),
                            //.OrderBy(v => v.Version!, VersionComparer.Instance),
                            true);
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
    }
}
