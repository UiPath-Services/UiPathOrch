using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Set, "OrchSetting", SupportsShouldProcess = true)]
public class SetSettingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ValueCompleter))]
    public string? Value { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private readonly List<Settings> _pendingSettings = [];
    private string[]? _resolvedPath;

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

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Settings.Get());

            foreach (var result in results)
            {
                foreach (var item in result
                    .Where(e => wp.IsMatch(e.Name))
                    .OrderBy(e => e.Name))
                {
                    string tooltip = $"{item.Name}  current value: [{item.Value}]";
                    yield return new CompletionResult(PathTools.EscapePSText(item.Name), item.Name, CompletionResultType.ParameterValue, tooltip);
                }
            }
        }
    }

    private class ValueCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);
            var paramName = GetFakeBoundParameter(fakeBoundParameters, "Name");
            if (string.IsNullOrEmpty(paramName)) yield break;

            var results = ParallelResults.GroupBy(drives, drive => drive.Settings.Get());

            foreach (var result in results)
            {
                var item = result.FirstOrDefault(e => string.Equals(e.Name, paramName, StringComparison.OrdinalIgnoreCase));
                if (item is not null && item.Value is not null)
                {
                    string tooltip = $"{item.Name}  current value";
                    yield return new CompletionResult(PathTools.EscapePSText(item.Value), item.Value, CompletionResultType.ParameterValue, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _resolvedPath ??= Path;
        _pendingSettings.Add(new Settings { Name = Name, Value = Value });
    }

    protected override void EndProcessing()
    {
        if (_pendingSettings.Count == 0) return;

        var drives = SessionState.EnumOrchDrives(_resolvedPath);

        foreach (var drive in drives)
        {
            string target = drive.NameColonSeparator;
            string settingNames = string.Join(", ", _pendingSettings.Select(s => s.Name));

            if (ShouldProcess(target, $"Set {_pendingSettings.Count} setting(s): {settingNames}"))
            {
                try
                {
                    drive.OrchAPISession.UpdateSettingsBulk(_pendingSettings);
                    drive.Settings.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "SetSettingError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
