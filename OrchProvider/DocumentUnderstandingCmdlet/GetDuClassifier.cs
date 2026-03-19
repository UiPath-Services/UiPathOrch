using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuClassifier")]
[OutputType(typeof(Entities.DuClassifier))]
public class GetDuClassifierCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ClassifierNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private class ClassifierNameCompleter : OrchArgumentCompleter
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

            // Exclude already-selected ClassifierName values from completion candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuClassifiers(dp.project));

            foreach (var result in results)
            {
                foreach (var classifier in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpName)
                    .OrderBy(e => e?.name))
                {
                    string tooltip = classifier.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(classifier.name), classifier.name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumDuFolders(Path, Recurse);
        var wpClassifierName = Name.ConvertToWildcardPatternList();

        // Synchronous version
        //foreach (var driveProject in drivesProjects)
        //{
        //    var (drive, project) = driveProject;

        //    WriteObject(drive.GetDuClassifiers(project)?
        //        .FilterByWildcards(u => u.name!, wpClassifierName)
        //        .OrderBy(e => e.name),
        //        true);
        //}

        // Asynchronous version
        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.GetDuClassifiers(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(u => u?.name, wpClassifierName)
                    .OrderBy(e => e.name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuClassifierError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
