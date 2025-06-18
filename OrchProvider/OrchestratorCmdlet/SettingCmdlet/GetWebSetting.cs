using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Key;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchWebSetting")]
[OutputType(typeof(ResponseDictionaryItem))]
public class GetWebSettingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyCompleter))]
    [SupportsWildcards]
    public string[]? Key { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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

            // パラメータで選択済みの Key は、候補から除外する
            var wpKey = CreateWPListFromParameter(commandAst, "Key", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drives, drive => drive.WebSettings.Get());

            foreach (var item in results
                .Select(r => r.Item)
                .Where(b => wp.IsMatch(b.Key))
                .ExcludeByWildcards(b => b?.Key, wpKey)
                .OrderBy(b => b.Key))
            {
                string tooltip = item.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(item.Key), item.Key, CompletionResultType.Text, tooltip);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpKey = Key.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.WebSettings.Get());

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
                WriteError(new ErrorRecord(ex, "GetWebSettingError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
