using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.XPath;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Key;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchWebSetting")]
    [OutputType(typeof(ResponseDictionaryItem))]
    public class GetWebSettingCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(KeyCompleter))]
        [SupportsWildcards]
        public string[]? Key { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Key>))]
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
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Key は、候補から除外する
                var wpKey = CreateWPListFromParameter(commandAst, "Key", Positional.Key.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetWebSettings());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var item in entities!
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
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpKey = Key.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetWebSettings());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

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
}
