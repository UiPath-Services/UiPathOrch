using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchClassicEnvironment")]
[OutputType(typeof(Entities.Environment))]
public class GetClassicEnvironmentCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Environments.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var env in result
                    .Where(s => wp.IsMatch(s.Name))
                    .ExcludeByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name))
                {
                    string tiphelp = env.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(env?.Name), env?.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(
            drivesFolders.Where(df => df.folder.ProvisionType == "Manual"),
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Environments.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var env = result.GetResult(cancelHandler.Token);
                if (env is null) continue;

                WriteObject(env
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetClassicEnvironmentError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
