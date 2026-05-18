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

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. WriteObject stays on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicenseContract.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var contract = result.GetResult(cancelHandler.Token);
                if (contract is not null) { var c = contract.ShallowClone(); c.Path = result.Source.NameColonSeparator; WriteObject(c); }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseContractError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
