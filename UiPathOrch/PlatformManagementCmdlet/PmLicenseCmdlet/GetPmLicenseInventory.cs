using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicenseInventory")]
[OutputType(typeof(Entities.LicenseInventory))]
public class GetPmLicenseInventory : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            try
            {
                var inventory = drive.PmLicenseInventory.Get();
                if (inventory is not null) WriteObject(inventory.WithPath(drive.NameColonSeparator));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseInventoryError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
