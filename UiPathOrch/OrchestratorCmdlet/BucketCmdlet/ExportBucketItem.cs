using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchBucketItem", SupportsShouldProcess = true)]
public class ExportBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketFullPathCompleter))]
    [SupportsWildcards]
    public string[]? FullPath { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (drive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpFullPath = FullPath.ConvertToWildcardPatternList();

        if (Destination is null)
        {
            Destination = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        // Convert PSDrive path to actual file system path
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        if (!Directory.Exists(Destination))
        {
            throw new DirectoryNotFoundException($"A directory '{Destination}' does not exist.");
        }

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Buckets.Get(df.folder));

        // Dedup buckets across the recursive folder walk: a bucket linked to
        // multiple folders shows up in each enumerated folder with the same
        // Id, and the -Recurse path would otherwise write the same items
        // once per folder it is accessible from. First-seen wins (the items
        // land under the relative path of whichever folder is processed
        // first), and we WriteWarning the second / third / ... encounters so
        // the user sees the link rather than silently losing the per-folder
        // mirror behaviour the previous code shipped.
        // Maps bucket Id → the folder whose path actually held the items, for
        // a precise warning message on the duplicate side.
        var seenBuckets = new Dictionary<Int64, string>();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var (_, folder) = result.Source;

                foreach (var bucket in entities
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    if (seenBuckets.TryGetValue(bucket.Id ?? 0, out var firstFolderPath))
                    {
                        WriteWarning($"Bucket '{bucket.Name}' is also accessible from '{folder.GetPSPath()}' via a folder link. Items already downloaded under '{firstFolderPath}'; skipping to avoid duplicate writes.");
                        continue;
                    }
                    seenBuckets[bucket.Id ?? 0] = folder.GetPSPath();

                    // Get the relative path from Path
                    var eachDestination = System.IO.Path.Combine(Destination, folder.GetRelativePath(srcRootFolder), bucket.Name!.MakeValidFolderName());

                    string target = null;
                    try
                    {
                        var files = drive.BucketFiles.Get(folder, bucket);

                        foreach (var file in files
                            .Where(f => !string.IsNullOrEmpty(f?.FullPath))
                            .FilterByWildcards(f => f!.FullPath, wpFullPath)
                            .OrderBy(f => f.FullPath).WithCancellation(cancelHandler.Token))
                        {
                            var destinationFullPath = System.IO.Path.Combine(eachDestination, file.FullPath!.MakeValidFileName());

                            target = $"Item: {file.GetPSPath()} Destination: {destinationFullPath}";
                            if (ShouldProcess(target, "Export BucketItem"))
                            {
                                try
                                {
                                    Directory.CreateDirectory(eachDestination);
                                    var access = drive.OrchAPISession.GetBucketReadUri(folder.Id!.Value, bucket.Id!.Value, file.FullPath!);
                                    drive.OrchAPISession.ReadBucketItem(access, System.IO.Path.Combine(eachDestination, destinationFullPath), cancelHandler.Token);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(bucket.GetPSPath(), ex), "GetBucketFileError", ErrorCategory.InvalidOperation, bucket));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "GetBucketFileError", ErrorCategory.InvalidOperation, bucket));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetBucketError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
