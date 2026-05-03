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

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        var drivesLinks = SessionState.EnumFolders(Link);

        // Parallel prefetch warms the per-folder Assets cache so the sequential loop below
        // can iterate without latency. Errors here are non-fatal — the sequential loop will
        // re-raise them through WriteError where the per-folder context can be reported.
        Parallel.ForEach(drivesFolders, driveFolder =>
        {
            var (drive, folder) = driveFolder;
            try
            {
                drive.Assets.Get(folder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddAssetLink prefetch failed for {folder.GetPSPath()}: {ex.Message}");
            }
        });

        foreach (var (drive, folder) in drivesFolders)
        {
            ICollection<Asset> assets = null;
            try
            {
                assets = drive.Assets.Get(folder);
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetError", ErrorCategory.InvalidOperation, target));
                continue;
            }

            var linksIdsToAdd = drivesLinks.Where(dl => dl.drive == drive);

            foreach (var asset in assets
                .FilterByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name))
            {
                foreach (var linkDF in linksIdsToAdd)
                {
                    string target = System.IO.Path.Combine(folder.GetPSPath(), asset.Name!);
                    if (ShouldProcess(target, $"Add AssetLink '{linkDF.folder.GetPSPath()}'"))
                    {
                        try
                        {
                            drive.OrchAPISession.ShareAssetsToFolders(folder.Id ?? 0,
                                [asset.Id ?? 0],
                                [linkDF.folder.Id ?? 0],
                                []);
                            drive._dicAssetLinks = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddAssetLinkError", ErrorCategory.InvalidOperation, target));
                        }
                    }
                }
            }
        }
    }
}
