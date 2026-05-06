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

    // Per-folder pre-computed work item: an asset and its accessible-folders snapshot.
    // Built inside the parallel worker so the main thread only sees pre-resolved data.
    private sealed record FolderResult(Asset Asset, AccessibleFoldersDto Accessible);

    // SimpleFolder has no GetPSPath() — it's a value type returned by the
    // GetFoldersFor* family. Build the PSPath as drive prefix + FullyQualifiedName,
    // mirroring Folder.GetPSPath()'s output.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // Parallel across (drive, folder) sources. The semaphore inside
        // OrchThreadPool caps to 4 concurrent workers — well below typical
        // server rate limits while keeping the main thread responsive.
        // Each worker fetches the folder's assets, filters by Name, and
        // resolves accessible folders for each match. Errors propagate as
        // OrchException with the folder path embedded.
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var assets = df.drive.Assets.Get(df.folder);
                var rows = new List<FolderResult>();

                foreach (var asset in assets
                    .FilterByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name))
                {
                    var accessible = df.drive.GetFoldersForAsset(df.folder, asset);
                    if (accessible?.AccessibleFolders is { Length: > 1 })
                    {
                        rows.Add(new FolderResult(asset, accessible));
                    }
                }

                return rows;
            });

        // Triple-level dedup: same (sourceFolder, asset, linkFolder) is never emitted
        // twice. Symmetric pairs ((X, A, Y) and (Y, A, X)) are still both emitted —
        // they describe the same link from different vantage points and each is
        // independently useful when iterating with -Recurse.
        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            List<FolderResult>? rows;
            try
            {
                rows = result.GetResult(cancelHandler.Token);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, ex.Target));
                continue;
            }
            if (rows is null) continue;

            var (drive, folder) = result.Source;
            Int64 srcId = folder.Id ?? 0;
            string sourcePath = folder.GetPSPath();

            foreach (var (asset, accessible) in rows)
            {
                Int64 assetId = asset.Id ?? 0;

                foreach (var linkFolder in accessible.AccessibleFolders!.OrderBy(f => f.FullyQualifiedName))
                {
                    Int64 linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, assetId, linkId))) continue;

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
