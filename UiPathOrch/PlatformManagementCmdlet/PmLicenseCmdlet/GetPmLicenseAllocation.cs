using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicenseAllocation")]
[OutputType(typeof(Entities.TenantAllocation))]
public class GetPmLicenseAllocation : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TenantCompleter))]
    [SupportsWildcards]
    public string[]? Tenant { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class TenantCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);
            var wpTenant = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicenseAllocations.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var a in result
                    .Where(a => !string.IsNullOrEmpty(a?.tenant?.name))
                    .ExcludeByWildcards(a => a?.tenant?.name!, wpTenant)
                    .OrderBy(a => a?.tenant?.name))
                {
                    string tiphelp = $"{drive.NameColonSeparator}{a.tenant!.name}  Unatt={a.unattendedRobot ?? 0} NonPr={a.nonProductionRobot ?? 0} Test={a.testingRobot ?? 0}";
                    yield return new CompletionResult(PathTools.EscapePSText(a.tenant.name), a.tenant.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        var wpTenant = Tenant.ConvertToWildcardPatternList();

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. Filtering / WriteObject stay on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicenseAllocations.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var targetEntities = entities
                    .FilterByWildcards(a => a?.tenant?.name, wpTenant)
                    .OrderBy(a => a?.tenant?.name);

                WriteObject(targetEntities.Select(e => { var c = e.ShallowClone(); c.Path = result.Source.NameColonSeparator; return c; }), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseAllocationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
