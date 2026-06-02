using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Suggests the notification delivery modes for -Mode.
internal class PmNotificationModeCompleter : OrchArgumentCompleter
{
    private static readonly string[] Modes = ["InApp", "Email"];

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);
        foreach (var mode in Modes.Where(m => wp.IsMatch(m)))
        {
            yield return new CompletionResult(mode, mode, CompletionResultType.ParameterValue, mode);
        }
    }
}
