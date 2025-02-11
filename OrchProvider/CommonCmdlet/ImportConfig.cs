using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// だめだ、実装する方法が見つからない。。
// 既存の PSDrive を削除することはできるが、

//[Cmdlet(VerbsData.Import, "OrchConfig", SupportsShouldProcess = true)]
class ImportConfigCommand : PSCmdlet
{
    //protected override Collection<PSDriveInfo>? InitializeDefaultDrives()

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumAllOrchDrives();
        foreach (var drive in drives)
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        var duDrives = OrchDuDriveInfo.EnumAllOrchDrives();
        foreach (var drive in duDrives)
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        var tmDrives = OrchTmDriveInfo.EnumAllOrchDrives();
        foreach (var drive in tmDrives)
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        var providerInfo = SessionState.Provider.Get("OrchProvider").FirstOrDefault();
        if (providerInfo is not null)
        {

        }

        //var newDrive = provider.ImportOrchConfig();
    }
}
