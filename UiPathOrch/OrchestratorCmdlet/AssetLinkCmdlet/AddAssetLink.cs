using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchAssetLink", SupportsShouldProcess = true)]
public class AddAssetLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Link { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var drivesLinks = SessionState.EnumFolders(Link).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        // Parallel prefetch warms the per-folder Assets cache.
        Parallel.ForEach(drivesFolders, df =>
        {
            try { df.drive.Assets.Get(df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AddAssetLink prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            ICollection<Asset> assets;
            try
            {
                assets = drive.Assets.Get(folder);
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "AddAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            // Cross-drive linking is not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var asset in assets
                .FilterByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name).WithCancellation(cancelHandler.Token))
            {
                // Batch all targets for this (folder, asset) into a single API call.
                // ShareAssetsToFolders accepts arrays for both the asset list and the
                // ToAdd folder list, so 1 asset × N targets is one round-trip.
                var toAddIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)   // can't link to self
                    .Distinct()
                    .ToList();
                if (toAddIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, asset.Name!);
                string action = $"Add AssetLink → {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    drive.OrchAPISession.ShareAssetsToFolders(
                        folder.Id ?? 0,
                        new List<Int64> { asset.Id ?? 0 },
                        toAddIds,
                        new List<Int64>());
                    // Invalidate just what changed: this asset's link set, and the
                    // per-folder asset list for each newly-targeted folder (which
                    // now exposes this asset where it didn't before).
                    drive.ClearAssetLinkCache(asset.Id ?? 0);
                    foreach (var dl in sameDriveLinks.Where(dl => toAddIds.Contains(dl.folder.Id ?? 0)))
                    {
                        drive.Assets.ClearCache(dl.folder);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddAssetLinkError",
                        ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
