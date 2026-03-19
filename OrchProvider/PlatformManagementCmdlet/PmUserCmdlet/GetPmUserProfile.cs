using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// This endpoint returns empty results...
// Keeping it non-public for now.
//[Cmdlet(VerbsCommon.Get, "PmUserProfile")]
[OutputType(typeof(Entities.UserProfile))]
class GetPmUserProfileCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            var u = drive.OrchAPISession.GetPmUserProfile();
            WriteObject(u);
            //drive.OrchAPISession.GetIdSetting();
        }
    }
}
