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

        using var cancelHandler = new ConsoleCancelHandler();
        var token = cancelHandler.Token;

        // Outer folder loop: sequential. -Recurse over many folders no longer
        // queues a global flood of Phase 1 tasks ahead of folder[0]'s Phase 2;
        // we move on to folder[i+1] only after folder[i]'s rows are emitted.
        // The win is predictable streaming order: folder[0]'s links emit
        // alphabetically-by-asset before any of folder[1]'s start.
        //
        // Inner asset list: parallel via OrchThreadPool. Each worker resolves
        // GetFoldersForAsset for one asset (1 API call), and the main thread
        // drains that pool in input (Name) order — matching how GetBucket
        // streams its per-folder output.
        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

        foreach (var (drive, folder) in drivesFolders)
        {
            token.ThrowIfCancellationRequested();

            List<Asset> assets;
            try
            {
                assets = drive.Assets.Get(folder)
                    .FilterByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }
            if (assets.Count == 0) continue;

            string sourcePath = folder.GetPSPath();
            Int64 srcId = folder.Id ?? 0;

            using var assetResults = OrchThreadPool.RunForEach(assets,
                asset => System.IO.Path.Combine(sourcePath, asset.Name ?? ""),
                asset => asset,
                asset => drive.GetFoldersForAsset(folder, asset));

            foreach (var ar in assetResults)
            {
                AccessibleFoldersDto? accessible;
                Asset asset;
                try
                {
                    accessible = ar.GetResult(token);
                    asset = ar.Source;
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetAssetLinkError",
                        ErrorCategory.InvalidOperation, ex.Target));
                    continue;
                }
                if (accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                Int64 assetId = asset.Id ?? 0;

                foreach (var linkFolder in accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
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
