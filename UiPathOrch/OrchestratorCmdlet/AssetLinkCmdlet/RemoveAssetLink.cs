using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchAssetLink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public class RemoveAssetLinkCommand : OrchestratorPSCmdlet
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
                    $"RemoveAssetLink prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
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
                WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            // Cross-drive operations are not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var asset in assets
                .FilterByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                // Batch all targets for this (folder, asset) into a single API call.
                // The ShareToFolders endpoint accepts both ToAdd and ToRemove arrays;
                // we pass an empty ToAdd here and the targets as ToRemove.
                var toRemoveIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)
                    .Distinct()
                    .ToList();
                if (toRemoveIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, asset.Name!);
                string action = $"Remove AssetLink ✗ {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    drive.OrchAPISession.ShareAssetsToFolders(
                        folder.Id ?? 0,
                        new List<Int64> { asset.Id ?? 0 },
                        new List<Int64>(),
                        toRemoveIds);
                    drive._dicAssetLinks = null;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveAssetLinkError",
                        ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
