using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicenseContract")]
[OutputType(typeof(Entities.AccountLicense))]
public class GetPmLicenseContract : OrchestratorPSCmdlet
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
                var contract = drive.PmLicenseContract.Get();
                if (contract is not null) WriteObject(contract.WithPath(drive.NameColonSeparator));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseContractError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
