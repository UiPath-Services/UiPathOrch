using System.Collections;
using System.Collections.Concurrent;
using System.Configuration.Provider;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Mount, "OrchLibraryFeed")]
    [OutputType(typeof(Entities.Library))]
    class MountLibraryFeedCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Path>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var providerInfo = this.SessionState.Provider.GetOne("UiPathOrchLib");

            foreach (var drive in drives)
            {
                var driveInfo = new LibraryDriveInfo(drive, providerInfo);
                var addedDrive = SessionState.Drive.New(driveInfo, scope: "Global");
                WriteObject(addedDrive);
            }
        }
    }
}
