using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Switch, "OrchCurrentUser", SupportsShouldProcess = true)]
[OutputType(typeof(User))]
public class SwitchOrchCurrentUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        foreach (var drive in SessionState.EnumOrchDrives(Path))
        {
            if (ShouldProcess(drive.NameColonSeparator, "Switch CurrentUser"))
            {
                try
                {
                    // Clear existing authentication and cache, then re-authenticate
                    drive.OrchAPISession.ClearAuthentication();
                    drive.ClearAllCache();
                    drive.OrchAPISession.AuthManager.UseInPrivate = true;

                    drive.OrchAPISession.EnsureAuthenticated();
                    drive.GetPartitionGlobalId();

                    var currentUser = drive.CurrentUser.Get();
                    if (currentUser is not null)
                    {
                        WriteObject(currentUser);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "SwitchOrchCurrentUserError", ErrorCategory.ConnectionError, drive));
                }
                finally
                {
                    drive.OrchAPISession.AuthManager.UseInPrivate = false;
                }
            }
        }
    }
}
