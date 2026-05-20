using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;


namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuExtractor")]
[OutputType(typeof(Entities.DuExtractor))]
public class GetDuExtractorCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExtractorNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private class ExtractorNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesProjects = SessionState.EnumDuFolders(paramPath, recurse);

            // Exclude already-selected ExtractorName values from completion candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.GetDuExtractors(dp.project));

            foreach (var result in results)
            {
                foreach (var extractor in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpName)
                    .OrderBy(e => e?.name))
                {
                    string tooltip = extractor.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(extractor.name), extractor.name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumDuFolders(Path, Recurse);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.GetDuExtractors(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                // Per-drive ShallowClone() copies with drive-local Path /
                // Project stamped (uniform DU path-isolation pattern; never
                // mutate the cached entity).
                var (_, project) = result.Source;
                var pathProject = project.GetPSPath();
                var projectName = project.name;
                WriteObject(entities!
                    .FilterByWildcards(u => u?.name, wpName)
                    .OrderBy(e => e.name)
                    .Select(e => { var c = e.ShallowClone(); c.Path = pathProject; c.Project = projectName; return c; }),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuExtractorError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
