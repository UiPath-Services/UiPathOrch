using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Search, "OrchDirectory")]
[OutputType(typeof(DirectoryObject))]
public class SearchDirectoryCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            string name = GetParameterValue(commandAst, parameterName, TPositional.Parameters);
            if (string.IsNullOrEmpty(name))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var users = drive.SearchDirectory(name);

                foreach (var obj in users.OrderBy(s => s.identityName))
                {
                    bFound = true;
                    string tiphelp = drive.NameColonSeparator + obj.identityName;
                    yield return new CompletionResult(PathTools.EscapePSText(obj.identityName), obj.identityName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
            if (!bFound)
            {
                string text = $@"""(No users found for '{name}')""";
                yield return new CompletionResult(text, text, CompletionResultType.Text, text);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var drive in drives)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var directoryObjects = drive.SearchDirectory(Name!);
                if (directoryObjects is null) continue;

                WriteObject(directoryObjects, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
