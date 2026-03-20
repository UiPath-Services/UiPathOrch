using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAuthenticationSetting")]
[OutputType(typeof(ResponseDictionaryItem))]
public class GetAuthenticationSettingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyCompleter))]
    [SupportsWildcards]
    public string[]? Key { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class KeyCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Keys already selected by the parameter from the candidates
            var wpKey = CreateSelfExclusionList(commandAst, "Key", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.AuthenticationSettings.Get());

            foreach (var result in results)
            {
                foreach (var item in result
                    .Where(b => wp.IsMatch(b.Key))
                    .ExcludeByWildcards(b => b?.Key, wpKey)
                    .OrderBy(b => b.Key))
                {
                    string tooltip = item.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(item.Key), item.Key, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpKey = Key.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.AuthenticationSettings.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(e => e?.Key, wpKey)
                    .OrderBy(e => e.Key),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAuthenticationSettingError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
