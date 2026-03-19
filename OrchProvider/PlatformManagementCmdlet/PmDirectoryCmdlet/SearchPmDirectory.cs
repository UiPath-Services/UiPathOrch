using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Search, "PmDirectory")]
[OutputType(typeof(PmDirectoryEntityInfo))]
public class SearchPmDirectoryCommand : OrchestratorPSCmdlet
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
            string name = GetFakeBoundParameter(fakeBoundParameters, parameterName);
            if (string.IsNullOrEmpty(name))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolvePmDrives(fakeBoundParameters);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.SearchPmDirectory(name));

            foreach (var result in results)
            {
                foreach (var directoryEntry in result
                    .OrderBy(s => s.identityName))
                {
                    string tiphelp = directoryEntry.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(directoryEntry.identityName), directoryEntry.identityName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            try
            {
                var entityInfo = drive.SearchPmDirectory(Name!);
                if (entityInfo is null) continue;

                WriteObject(entityInfo, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchPmDirectoryError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
