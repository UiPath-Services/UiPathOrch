using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPmExternalApiResource")]
[OutputType(typeof(Entities.ExternalResource))]
public class GetPmExternalApiResourceCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
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
            var drives = ResolveDrives(fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drives, drive => drive.PmExternalApiResources.Get());

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .Where(r => wp.IsMatch(r?.name))
                    .ExcludeByWildcards(r => r?.name!, wpName)
                    .OrderBy(r => r?.name))
                {
                    string tiphelp = e.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(e?.name), e?.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmExternalApiResources.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(a => a!.name!, wpName)
                    .OrderBy(a => a!.name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetIdExternalApiResourceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
