using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    // このエンドポイントからは空が返ってきてしまう。。
    // いったん非公開で残しておく。
    //[Cmdlet(VerbsCommon.Get, "OrchPmUserProfile")]
    [OutputType(typeof(Entities.UserProfile))]
    class GetPmUserProfileCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Path>))]
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
}
