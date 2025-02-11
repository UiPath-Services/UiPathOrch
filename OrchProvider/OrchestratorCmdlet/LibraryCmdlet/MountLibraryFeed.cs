using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

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
