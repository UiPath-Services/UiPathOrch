using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchRole", SupportsShouldProcess = true)]
public class RemoveRoleCmdlet : RemoveDriveEntityCmdletBase<Role>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Role";
    protected override Func<Role?, string?> GetName => r => r?.Name;
    protected override Func<Role, string> GetPSPath => r => r.GetPSPath();
    protected override Func<IEnumerable<Role>, IEnumerable<Role>>? PreFilter
        => roles => roles.Where(r => !r.IsStatic.GetValueOrDefault());

    protected override IEnumerable<Role> GetEntities(OrchDriveInfo drive)
        => drive.Roles.Get();

    protected override void Remove(OrchDriveInfo drive, Role role)
    {
        drive.OrchAPISession.DeleteRole(role.Id ?? 0);
        drive.Roles.ClearCache();
    }
}
