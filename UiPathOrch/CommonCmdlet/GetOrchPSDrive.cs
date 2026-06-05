using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPSDrive")]
[OutputType(typeof(Entities.OrchPSDrive))]
public class GetOrchPSDriveCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    // If we could create an OrchDriveInfoBase class, this could be implemented more cleanly.
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
                // Confidential apps skip GetCurrentUser(), so _tenantId / _tenantKey
                // are not populated as a side effect of authentication. Trigger the
                // /odata/Users + /odata/Users(id) fallback explicitly so -Force returns
                // a fully populated OrchPSDrive for both Conf and Non-Conf apps.
                drive.GetTenantId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetOrchPSDriveError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    private void WriteOrchProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllOrchDrives();
        else drives = SessionState.EnumOrchDrives(effectivePath);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteDuProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchDuDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllDuDrives();
        else drives = SessionState.EnumDuDrives(effectivePath);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive.ParentDrive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteTmProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchTmDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllTmDrives();
        else drives = SessionState.EnumTmDrives(effectivePath);

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
