using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional; // AvailableUserBundlesItems (license code → display name)

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicenseInventory")]
[OutputType(typeof(Entities.LicenseInventory))]
public class GetPmLicenseInventoryCmdlet : OrchestratorPSCmdlet
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
                if (inventory is null) continue;

                var c = inventory.ShallowClone();
                c.Path = result.Source.NameColonSeparator;

                // The bundle rows come back with an EMPTY name and no statement of what the
                // bundle permits, which is the whole reason a license question meant decoding
                // the contract by hand. Both are filled in here.
                //
                // Rebuilt into a NEW array of copies, not stamped in place: ShallowClone
                // copies the array REFERENCE, so writing to its elements would write through
                // to the org-shared cached inventory.
                var bundleEntitlements = result.Source.GetUserBundleEntitlements();
                c.userLicensingBundles = inventory.userLicensingBundles?.Select(b =>
                {
                    var copy = b.ShallowClone();
                    copy.Path = result.Source.NameColonSeparator;
                    copy.name = string.IsNullOrEmpty(copy.code)
                        ? copy.name
                        : AvailableUserBundlesItems.Items.GetValueOrDefault(copy.code!, copy.code!);
                    copy.entitlements = GetPmUserLicenseCmdlet.BuildEntitlements(
                        copy.code is null ? null : [copy.code], bundleEntitlements);
                    return copy;
                }).ToArray();

                WriteObject(c);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseInventoryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
