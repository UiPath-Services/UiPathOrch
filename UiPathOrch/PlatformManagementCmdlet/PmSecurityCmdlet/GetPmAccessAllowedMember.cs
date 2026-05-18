using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAccessAllowedMember")]
[OutputType(typeof(AccessAllowedMember))]
public class GetPmAccessAllowedMemberCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            System.Collections.IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            // Exclude Names already selected via parameters from candidates
            var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmAccessAllowedMember.Get());

            foreach (var result in results)
            {
                foreach (var member in result
                    .Where(m => !string.IsNullOrEmpty(m.name))
                    .Where(m => wp.IsMatch(m.name))
                    .ExcludeByWildcards(m => m?.name!, wpName)
                    .OrderBy(m => m?.name))
                {
                    string tooltip = member.GetPSPath(result.Source.NameColonSeparator);
                    if (!string.IsNullOrEmpty(member.displayName))
                        tooltip += $" ({member.displayName})";
                    yield return new CompletionResult(PathTools.EscapePSText(member?.name), member?.name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. Filtering / WriteObject stay on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmAccessAllowedMember.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var targetUsers = entities
                    .FilterByWildcards(u => u?.name, wpName)
                    .OrderBy(u => u.name);

                WriteObject(targetUsers.Select(u => { var c = u.ShallowClone(); c.Path = result.Source.NameColonSeparator; return c; }), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "PmAccessAllowedMember", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
