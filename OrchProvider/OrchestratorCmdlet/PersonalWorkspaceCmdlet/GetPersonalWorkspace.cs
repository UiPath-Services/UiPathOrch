using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPersonalWorkspace")]
[OutputType(typeof(Entities.PersonalWorkspace))]
public class GetPersonalWorkspaceCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PersonalWorkspaces.Get());

            foreach (var result in results)
            {
                foreach (var personalWorkspace in result
                    .Where(q => wp.IsMatch(q.Name))
                    .ExcludeByWildcards(q => q?.Name, wpName)
                    .OrderBy(q => q.Name))
                {
                    string tiphelp = TipHelp(personalWorkspace);
                    yield return new CompletionResult(PathTools.EscapePSText(personalWorkspace.Name), personalWorkspace.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PersonalWorkspaces.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var wss = result.GetResult(cancelHandler.Token);
                if (wss is null) continue;

                WriteObject(wss
                    .FilterByWildcards(ws => ws?.Name, wpName)
                    .OrderBy(ws => ws.Name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPersonalWorkspaceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
