using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shelved for now because the API returns Forbidden.
[Cmdlet(VerbsCommon.Get, "TmRole")]
[OutputType(typeof(Entities.TmRole))]
class GetTmRoleCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(TmProjectNameCompleter))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmDriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class TmProjectNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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
            var wpname = CreateSelfExclusionList(commandAst, "name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.GetTmProjects());

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
        var drives = SessionState.EnumTmDrives(EffectivePath(Path, LiteralPath));
        //var wpProjectName = Name.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            WriteObject(drive.OrchAPISession.GetTmRoles(), true);
        }
    }
}
