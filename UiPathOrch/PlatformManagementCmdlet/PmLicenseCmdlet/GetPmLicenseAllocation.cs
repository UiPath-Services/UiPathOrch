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

    private class TenantCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
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
        var drives = SessionState.EnumPmDrives(Path);

        var wpTenant = Tenant.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmLicenseAllocations.Get();
                if (entities is null) continue;

                var targetEntities = entities
                    .FilterByWildcards(a => a?.tenant?.name, wpTenant)
                    .OrderBy(a => a?.tenant?.name);

                WriteObject(targetEntities.Select(e => e.WithPath(drive.NameColonSeparator)), true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseAllocationError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
