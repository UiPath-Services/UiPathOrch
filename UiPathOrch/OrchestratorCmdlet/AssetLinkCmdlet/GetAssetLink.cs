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

        // Phase 1: parallel fetch of the assets-per-folder list. One API call
        // per worker, typically already cached, so this drains quickly. We have
        // to gather all (folder, asset) sources before Phase 2 can launch
        // because OrchThreadPool needs the full source list up front to build
        // its index-aligned thread array.
        using var assetResults = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Assets.Get(df.folder)
                .FilterByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name)
                .ToList());

        var sources = new List<(OrchDriveInfo drive, Folder folder, Asset asset)>();
        foreach (var ar in assetResults)
        {
            try
            {
                var assets = ar.GetResult(cancelHandler.Token);
                if (assets is null) continue;
                var (drive, folder) = ar.Source;
                foreach (var asset in assets)
                {
                    sources.Add((drive, folder, asset));
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 2: parallel GetFoldersForAsset per (folder, asset). One API call
        // per worker, so each result becomes available very quickly. Iterating
        // results in OrchThreadPool input order yields output in alphabetical
        // (folder, asset) order — same shape as GetBucket's streaming output.
        using var linkResults = OrchThreadPool.RunForEach(sources,
            s => System.IO.Path.Combine(s.folder.GetPSPath(), s.asset.Name ?? ""),
            s => s.asset,
            s => s.drive.GetFoldersForAsset(s.folder, s.asset));

        // Triple-level dedup just in case the same (sourceFolder, asset,
        // linkFolder) is encountered twice (defense in depth — the source list
        // shouldn't contain duplicates, but symmetric AccessibleFolders entries
        // could in theory yield the same triple).
        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

        foreach (var lr in linkResults)
        {
            AccessibleFoldersDto? accessible;
            try
            {
                accessible = lr.GetResult(cancelHandler.Token);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, ex.Target));
                continue;
            }
            if (accessible?.AccessibleFolders is not { Length: > 1 }) continue;

            var (drive, folder, asset) = lr.Source;
            Int64 srcId = folder.Id ?? 0;
            Int64 assetId = asset.Id ?? 0;
            string sourcePath = folder.GetPSPath();

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
