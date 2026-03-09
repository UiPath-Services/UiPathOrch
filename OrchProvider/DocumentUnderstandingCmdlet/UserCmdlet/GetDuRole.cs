using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuRole")]
[OutputType(typeof(Entities.DuRole))]
public class GetDuRoleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class RoleCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveDuDrives(fakeBoundParameters);

            // Exclude already-selected DuRole values from completion candidates
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.GetDuRoles());

            foreach (var result in results)
            {
                foreach (var role in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpName)
                    .OrderBy(e => e?.name))
                {
                    string tiphelp = role.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(role.name), role.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumDuDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.GetDuRoles());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(u => u?.name, wpName)
                    .OrderBy(e => e.name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuRoleError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
