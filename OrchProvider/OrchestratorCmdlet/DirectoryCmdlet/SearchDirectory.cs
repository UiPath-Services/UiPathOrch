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
            wordToComplete = RemoveEnclosingQuotes(wordToComplete);
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            var results = ParallelResults3.GroupBy(drives, drive => drive.SearchDirectory(wordToComplete));

            bool bFound = false;
            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var obj in result
                    .OrderBy(s => s.identityName))
                {
                    bFound = true;
                    string tiphelp = drive.NameColonSeparator + obj.identityName;
                    yield return new CompletionResult(PathTools.EscapePSText(obj.identityName), obj.identityName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{wordToComplete}')""");
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.SearchDirectory(Name!));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var directoryObjects = result.GetResult(cancelHandler.Token);
                if (directoryObjects is null) continue;

                WriteObject(directoryObjects, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "SearchDirectoryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
