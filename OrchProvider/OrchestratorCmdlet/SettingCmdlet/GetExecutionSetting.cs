using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Scope_DisplayName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchExecutionSetting")]
[OutputType(typeof(ExecutionSettingDefinition))]
public class GetExecutionSettingCommand : OrchestratorPSCmdlet
{
    private static readonly Dictionary<int, string> scopeList = new()
    {
        { 0, "Global" },
        { 1, "Robot" }
    };

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ScopeCompleter))]
    [SupportsWildcards]
    public string[]? Scope { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DisplayNameCompleter))]
    [SupportsWildcards]
    public string[]? DisplayName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class ScopeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // パラメータで選択済みの Scope は、候補から除外する
            var wpScope = CreateWPListFromParameter(commandAst, "Scope", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var scope in scopeList.Values
                    .Where(e => wp.IsMatch(e))
                    .ExcludeByWildcards(e => e, wpScope)
                    .OrderBy(e => e))
            {
                yield return new CompletionResult(PathTools.EscapePSText(scope), scope, CompletionResultType.Text, scope);
            }
        }
    }

    private class DisplayNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpScope = CreateWPListFromOtherParameters(commandAst, "Scope", TPositional.Parameters);

            var specifiedScopes = scopeList.FilterByWildcards(s => s.Value, wpScope);

            // パラメータで選択済みの Key は、候補から除外する
            var wpDisplayName = CreateWPListFromParameter(commandAst, "DisplayName", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive =>
            {
                List<string> existingDisplayNames = [];

                foreach (var specifiedScope in specifiedScopes)
                {
                    var settings = drive.GetExecutionSettings(specifiedScope.Key, specifiedScope.Value);
                    existingDisplayNames.AddRange(settings?.Select(s => s.DisplayName!) ?? []);
                }
                return existingDisplayNames;
            });

            foreach (var result in results)
            {
                foreach (var item in result
                    .Where(e => wp.IsMatch(e))
                    .ExcludeByWildcards(e => e, wpDisplayName)
                    .OrderBy(e => e))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(item), item, CompletionResultType.Text, item);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpScope = Scope.ConvertToWildcardPatternList();
        var wpDisplayName = DisplayName.ConvertToWildcardPatternList();

        var specifiedScopes = scopeList.FilterByWildcards(s => s.Value, wpScope);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive =>
            {
                foreach (var scope in specifiedScopes)
                {
                    drive.GetExecutionSettings(scope.Key, scope.Value);
                }
                return drive._dicExecutionSettings;
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                foreach (var scope in specifiedScopes
                    .OrderBy(s => s.Key))
                {
                    if (entities!.TryGetValue(scope.Key, out var settingArray))
                    {
                        WriteObject(settingArray!
                            .FilterByWildcards(r => r?.DisplayName, wpDisplayName)
                            .OrderBy(r => r.DisplayName),
                            true);
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetExecutionSettingError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
