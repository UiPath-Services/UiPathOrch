using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuExtractor")]
[OutputType(typeof(Entities.DuExtractor))]
public class GetDuExtractorCommand : OrchestratorPSCmdlet
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
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesProjects = SessionState.EnumDuFolders(paramPath, recurse);

            // Exclude already-selected DocumentTypeName values from completion candidates
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuDocumentTypes(dp.project));

            foreach (var result in results)
            {
                foreach (var documentType in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpName)
                    .OrderBy(e => e?.name))
                {
                    string tooltip = documentType.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(documentType.name), documentType.name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumDuFolders(Path, Recurse);
        var wpName = Name.ConvertToWildcardPatternList();

        // Synchronous version
        //foreach (var driveProject in drivesProjects)
        //{
        //    var (drive, project) = driveProject;

        //    WriteObject(drive.GetDuExtractors(project)?
        //        .FilterByWildcards(u => u.name!, wpName)
        //        .OrderBy(e => e.name),
        //        true);
        //}

        // Asynchronous version
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

                WriteObject(entities!
                    .FilterByWildcards(u => u?.name, wpName)
                    .OrderBy(e => e.name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuExtractorError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
