using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmExternalApiResource")]
[OutputType(typeof(Entities.ExternalResource))]
public class GetPmExternalApiResourceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            // Exclude Names already selected via parameters from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmExternalApiResources.Get());

            foreach (var result in results)
            {
                foreach (var resource in result
                    .Where(r => wp.IsMatch(r?.name))
                    .ExcludeByWildcards(r => r?.name!, wpName)
                    .OrderBy(r => r?.name))
                {
                    string tiphelp = resource.GetPSPath(result.Source.NameColonSeparator);
                    yield return new CompletionResult(PathTools.EscapePSText(resource?.name), resource?.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. Filtering / WriteObject stay on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmExternalApiResources.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var resources = result.GetResult(cancelHandler.Token);
                if (resources is null) continue;
                WriteObject(resources
                    .FilterByWildcards(a => a!.name!, wpName)
                    .OrderBy(a => a!.name)
                    .Select(a => { var c = a!.ShallowClone(); c.Path = result.Source.NameColonSeparator; return c; }),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetIdExternalApiResourceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
