using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shelved: BusinessRules require OR.BusinessRules / OR.BusinessRules.Read scope, which is
// not exposed by Identity Server scopes_supported and is absent from the External Application
// resource dropdown. Same limitation as Connection Service. Verified 2026-04-27.
// Re-enable by switching to `public` and adding the name to UiPathOrch.psd1 once the scope ships.
[Cmdlet(VerbsCommon.Get, "OrchBusinessRule")]
[OutputType(typeof(Entities.BusinessRule))]
class GetBusinessRuleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.BusinessRules.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var rules = result.GetResult(cancelHandler.Token);
                if (rules is null) continue;

                WriteObject(rules
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetBusinessRuleError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
