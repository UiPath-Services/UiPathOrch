using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

// Shelved for now because the API returns Forbidden.
[Cmdlet(VerbsCommon.Get, "TmRole")]
[OutputType(typeof(Entities.TmRole))]
class GetTmRoleCommand : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(TmProjectNameCompleter))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmDriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class TmProjectNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drives = SessionState.EnumTmDrives(paramPath);

            // Exclude already-selected Name values from completion candidates
            var wpname = CreateWPListFromParameter(commandAst, "name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.GetTmProjects());

            foreach (var result in results)
            {
                foreach (var project in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpname)
                    .OrderBy(e => e?.name))
                {
                    string tooltip = project.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(project.name), project.name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumTmDrives(Path);
        //var wpProjectName = Name.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            WriteObject(drive.OrchAPISession.GetTmRoles(), true);
        }
    }
}
