using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmExternalApiResource")]
[OutputType(typeof(Entities.ExternalResource))]
public class GetPmExternalApiResourceCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
            var drives = ResolvePmDrives(fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PmExternalApiResources.Get());

            foreach (var result in results)
            {
                foreach (var resource in result
                    .Where(r => wp.IsMatch(r?.name))
                    .ExcludeByWildcards(r => r?.name!, wpName)
                    .OrderBy(r => r?.name))
                {
                    string tiphelp = resource.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(resource?.name), resource?.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            try
            {
                var resources = drive.PmExternalApiResources.Get();
                WriteObject(resources
                    .FilterByWildcards(a => a!.name!, wpName)
                    .OrderBy(a => a!.name),
                    true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetIdExternalApiResourceError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
