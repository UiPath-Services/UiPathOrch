using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: see GetBusinessRule.cs for rationale (OR.BusinessRules scope not available
// to External Applications or Personal Access Tokens).
[Cmdlet(VerbsCommon.Remove, "OrchBusinessRule", SupportsShouldProcess = true)]
class RemoveBusinessRuleCommand : RemoveFolderEntityCmdletBase<BusinessRule>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "BusinessRule";
    protected override Func<BusinessRule?, string?> GetName => r => r?.Name;
    protected override Func<BusinessRule, string> GetPSPath => r => r.GetPSPath();
    protected override Func<IEnumerable<BusinessRule>, IEnumerable<BusinessRule>>? PreFilter
        => rules => rules.Where(r => !string.IsNullOrEmpty(r.Id));

    protected override IEnumerable<BusinessRule> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.BusinessRules.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, BusinessRule rule)
    {
        drive.OrchAPISession.RemoveBusinessRule(folder.Id ?? 0, rule.Id!);
        drive.BusinessRules.ClearCache(folder);
    }
}
