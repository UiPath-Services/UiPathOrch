using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

// このエンドポイントからは空が返ってきてしまう。。
// いったん非公開で残しておく。
//[Cmdlet(VerbsCommon.Get, "OrchPmUserProfile")]
[OutputType(typeof(Entities.UserProfile))]
class GetPmUserProfileCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        foreach (var drive in drives)
        {
            var u = drive.OrchAPISession.GetPmUserProfile();
            WriteObject(u);
            //drive.OrchAPISession.GetIdSetting();
        }
    }
}
