using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPSDrive")]
[OutputType(typeof(Entities.OrchPSDrive))]
public class GetOrchPSDriveCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    // OrchDriveInfoBase クラスを作成できれば、ここをいい感じで実装できそうだが。。
    //private void EnumDrives<T>() where T : OrchDriveInfoBase
    //{
    //    IEnumerable<T> drives = null;
    //    if (Path is null)
    //    {
    //        drives = T.EnumAllOrchDrives();
    //    }
    //    else
    //    {
    //        drives = T.EnumOrchDrives(Path);
    //    }
    //}

    private void ConnectToOrchDrive(OrchDriveInfo drive)
    {
        if (Force.IsPresent)
        {
            try
            {
                drive.OrchAPISession.EnsureAuthenticated();
                drive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetActivitySettingsError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    private void WriteOrchProviderInfo()
    {
        IEnumerable<OrchDriveInfo> drives;
        if (Path is null) drives = OrchDriveInfo.EnumAllOrchDrives();
        else              drives = OrchDriveInfo.EnumOrchDrives(Path);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteDuProviderInfo()
    {
        IEnumerable<OrchDuDriveInfo> drives;
        if (Path is null) drives = OrchDuDriveInfo.EnumAllOrchDrives();
        else              drives = OrchDuDriveInfo.EnumOrchDuDrives(Path);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive.ParentDrive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteTmProviderInfo()
    {
        IEnumerable<OrchTmDriveInfo> drives;
        if (Path is null) drives = OrchTmDriveInfo.EnumAllOrchDrives();
        else              drives = OrchTmDriveInfo.EnumOrchTmDrives(Path);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive.ParentDrive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    protected override void ProcessRecord()
    {
        WriteOrchProviderInfo();
        WriteDuProviderInfo();
        WriteTmProviderInfo();
    }
}
