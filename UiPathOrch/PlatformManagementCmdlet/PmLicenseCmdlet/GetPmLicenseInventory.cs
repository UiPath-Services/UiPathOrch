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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        // Fetch in parallel. Per-org caches serialize same-partition fetches
        // internally (KeyedSingle/Single/List CachePerOrganization lock per
        // partitionGlobalId), so concurrent drives in the same org don't
        // double-fetch; different orgs run truly parallel. WriteObject stays
        // on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicenseInventory.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var inventory = result.GetResult(cancelHandler.Token);
                if (inventory is not null) { var c = inventory.ShallowClone(); c.Path = result.Source.NameColonSeparator; WriteObject(c); }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseInventoryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
