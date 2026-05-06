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

    private const int MaxDegreeOfParallelism = 4;

    // SimpleFolder has no GetPSPath() — it's a value type returned by the
    // GetFoldersFor* family. Build the PSPath as drive prefix + FullyQualifiedName,
    // mirroring Folder.GetPSPath()'s output.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    private record FolderRow(Asset Asset, AccessibleFoldersDto? Accessible);

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        var token = cancelHandler.Token;

        // One semaphore caps every API round-trip — Assets.Get, GetFoldersForAsset
        // alike — at MaxDegreeOfParallelism concurrent. The previous Phase-1 /
        // Phase-2 split serialized through the entire Assets.Get drain before
        // any GetFoldersForAsset could run, which made -Recurse over many
        // folders look like "wait for everything, then dump". Here every
        // folder's chain (Assets.Get → fan-out GetFoldersForAsset) starts the
        // moment the semaphore lets it, so folder[0]'s output becomes
        // available after just two round-trips' worth of latency, not after
        // the full Assets.Get drain.
        using var sem = new SemaphoreSlim(MaxDegreeOfParallelism);

        var folderChains = drivesFolders.Select(df => Task.Run(async () =>
        {
            // Phase 1: fetch the folder's asset list.
            await sem.WaitAsync(token);
            List<Asset> assets;
            try
            {
                assets = df.drive.Assets.Get(df.folder)
                    .FilterByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name)
                    .ToList();
            }
            finally
            {
                sem.Release();
            }

            // Phase 2: resolve accessible folders per matching asset, in
            // parallel under the same global cap. Order is preserved by
            // building the result array in the same shape as `assets`.
            var perAsset = await Task.WhenAll(assets.Select(asset => Task.Run<FolderRow>(async () =>
            {
                await sem.WaitAsync(token);
                try
                {
                    return new FolderRow(asset, df.drive.GetFoldersForAsset(df.folder, asset));
                }
                finally
                {
                    sem.Release();
                }
            })));

            return perAsset;
        })).ToList();

        // Main thread drains folder chains in alphabetical (input) order. As
        // soon as folder[i]'s chain completes, its rows are emitted; we don't
        // wait for folder[i+1..N]. Within a folder, asset order matches the
        // OrderBy(Name) in Phase 1.
        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

        for (int i = 0; i < drivesFolders.Count; i++)
        {
            var (drive, folder) = drivesFolders[i];
            FolderRow[] rows;
            try
            {
                rows = folderChains[i].GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            Int64 srcId = folder.Id ?? 0;
            string sourcePath = folder.GetPSPath();

            foreach (var row in rows)
            {
                if (row.Accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                Int64 assetId = row.Asset.Id ?? 0;

                foreach (var linkFolder in row.Accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    Int64 linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, assetId, linkId))) continue;

                    WriteObject(new AssetLink
                    {
                        Path = sourcePath,
                        Name = row.Asset.Name,
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
