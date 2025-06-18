using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name;

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
            string name = GetParameterValue(commandAst, parameterName, TPositional.Parameters);
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
        var drives = OrchDriveInfo.EnumPmDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.SearchPmDirectory(Name!));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entityInfo = result.GetResult(cancelHandler.Token);
                if (entityInfo is null) continue;

                WriteObject(entityInfo, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "SearchPmDirectoryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
