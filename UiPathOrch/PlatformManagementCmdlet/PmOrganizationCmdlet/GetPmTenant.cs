using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The tenants of the organization, from the Organization Management Service: name, status,
// region, environment, id. Automation Cloud and Automation Suite only.
[Cmdlet(VerbsCommon.Get, "PmTenant")]
[OutputType(typeof(Entities.PmTenant))]
public class GetPmTenantCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmTenants.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(t => t?.name, wpName)
                    .OrderBy(t => t.name)
                    .Select(t => { var c = t.ShallowClone(); c.Path = result.Source.NameColonSeparator; return c; }), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmTenantError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}

// Tenant names of the organizations of the -Path drives, for Get-PmTenant -Name and
// Get-PmTenantService -Tenant.
internal class PmTenantNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);
        var wpExclude = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => drive.PmTenants.Get());

        foreach (var result in results)
        {
            var drive = result.Source;
            foreach (var t in result
                .Where(t => !string.IsNullOrEmpty(t?.name))
                .ExcludeByWildcards(t => t!.name!, wpExclude)
                .OrderBy(t => t.name))
            {
                string tiphelp = $"{drive.NameColonSeparator}{t.name}  {t.status} {t.region}";
                yield return new CompletionResult(PathTools.EscapePSText(t.name!), t.name, CompletionResultType.Text, tiphelp);
            }
        }
    }
}
