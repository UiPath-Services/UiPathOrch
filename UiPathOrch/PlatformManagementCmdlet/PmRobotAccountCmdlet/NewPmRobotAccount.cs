using System.Management.Automation;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Create a NEW robot account — strict create: errors if an account with the
// name already exists (like New-Item), leaving it untouched. For
// create-or-update use Set-PmRobotAccount; to change an existing account's
// group memberships use Add-/Remove-PmGroupMember (-Type DirectoryRobotUser).
// Shares all parameters and the create path with Set via
// PmRobotAccountWriteCmdletBase; only the existing-account behavior differs.
[Cmdlet(VerbsCommon.New, "PmRobotAccount", DefaultParameterSetName = "ConsoleInput", SupportsShouldProcess = true)]
[OutputType(typeof(PmRobotAccount))]
public class NewPmRobotAccountCmdlet : PmRobotAccountWriteCmdletBase
{
    protected override bool ErrorIfExists => true;
}
