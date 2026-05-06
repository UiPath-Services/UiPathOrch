using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAssetLink")]
[OutputType(typeof(AssetLink))]
public class GetAssetLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Construct the PSPath of an accessible folder (which is a SimpleFolder, not
    // a Folder, so it has no GetPSPath() of its own). Format: "{drive}:{sep}{full}"
    // mirroring what Folder.GetPSPath produces.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        // Parallel prefetch warms Assets cache and the per-asset accessibleFolders cache.
        // Errors here are non-fatal — the sequential loop re-raises them through
        // WriteError where the per-folder context can be reported.
        Parallel.ForEach(drivesFolders, df =>
        {
            try
            {
                var assets = df.drive.Assets.Get(df.folder);
                Parallel.ForEach(assets.FilterByWildcards(a => a?.Name, wpName), asset =>
                {
                    df.drive.GetFoldersForAsset(df.folder, asset);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetAssetLink prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        // Triple-level dedup: same (sourceFolder, asset, linkFolder) is never emitted
        // twice. Symmetric pairs ((X, A, Y) and (Y, A, X)) are still both emitted —
        // they describe the same link relationship from different vantage points,
        // which is useful when the user is iterating with -Recurse over the tree.
        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

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
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            foreach (var asset in assets
                .FilterByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                AccessibleFoldersDto? accessible;
                try
                {
                    accessible = drive.GetFoldersForAsset(folder, asset);
                }
                catch (Exception ex)
                {
                    string target = System.IO.Path.Combine(folder.GetPSPath(), asset.Name!);
                    WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError",
                        ErrorCategory.InvalidOperation, target));
                    continue;
                }

                // Asset is "linked" only when accessible from more than one folder.
                if (accessible?.AccessibleFolders is null || accessible.AccessibleFolders.Length <= 1) continue;

                Int64 srcId = folder.Id ?? 0;
                Int64 assetId = asset.Id ?? 0;
                string sourcePath = folder.GetPSPath();

                foreach (var linkFolder in accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    Int64 linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;                                  // skip the source itself
                    if (!emitted.Add((srcId, assetId, linkId))) continue;           // already reported

                    WriteObject(new AssetLink
                    {
                        Path = sourcePath,
                        Name = asset.Name,
                        Link = LinkFolderPSPath(drive, linkFolder),
                        AssetId = assetId,
                        FolderId = srcId,
                        LinkFolderId = linkId,
                    });
                }
            }
        }
    }
}
