using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Completes tenant display names of the target organization from the per-tenant
/// license allocations (the only place the portal pairs a tenant name with the
/// GUID the licensing endpoints take). Shared by the -Tenant parameter of
/// Get-PmLicenseProductAllocation and Set-PmLicenseAllocation; Get-PmLicenseAllocation
/// keeps its own nested copy because its tooltip shows different counts.
/// </summary>
internal class PmTenantCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);

        // Exclude tenants already given on the command line from the candidates.
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
                string tiphelp = $"{drive.NameColonSeparator}{a.tenant!.name}  AppTest={a.appTestRobot ?? 0} Unatt={a.unattendedRobot ?? 0} PlatformUnits={a.platformUnits ?? 0}";
                yield return new CompletionResult(PathTools.EscapePSText(a.tenant.name), a.tenant.name, CompletionResultType.Text, tiphelp);
            }
        }
    }
}
