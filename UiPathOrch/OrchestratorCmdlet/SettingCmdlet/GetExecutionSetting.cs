using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchExecutionSetting")]
[OutputType(typeof(ExecutionSettingDefinition))]
public class GetExecutionSettingCmdlet : OrchestratorPSCmdlet
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
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class ScopeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Exclude Scopes already selected by the parameter from the candidates
            var wpScope = CreateSelfExclusionList(commandAst, "Scope", wordToComplete);

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpScope = GetFakeBoundParameters(fakeBoundParameters, "Scope").ConvertToWildcardPatternList();

            var specifiedScopes = scopeList.FilterByWildcards(s => s.Value, wpScope);

            // Exclude DisplayNames already selected by the parameter from the candidates
            var wpDisplayName = CreateSelfExclusionList(commandAst, "DisplayName", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive =>
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
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpScope = Scope.ConvertToWildcardPatternList();

        var specifiedScopes = scopeList.FilterByWildcards(s => s.Value, wpScope);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive =>
            {
                var local = new Dictionary<int, ExecutionSettingDefinition[]?>();
                foreach (var scope in specifiedScopes)
                {
                    local[scope.Key] = drive.GetExecutionSettings(scope.Key, scope.Value);
                }
                return local;
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
                            .FilterByNames(r => r?.DisplayName, DisplayName)
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
