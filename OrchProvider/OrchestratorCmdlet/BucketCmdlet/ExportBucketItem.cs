using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_FullPath_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchBucketItem", SupportsShouldProcess = true)]
public class ExportBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketFullPathCompleter<TPositional>))]
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

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var(_, folder) = result.Source;

                foreach (var bucket in entities
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    // Get the relative path from Path
                    var eachDestination = System.IO.Path.Combine(Destination, folder.GetRelativePath(srcRootFolder), bucket.Name!.MakeValidFolderName());

                    string target = null;
                    try
                    {
                        var files = drive.BucketFiles.Get(folder, bucket);

                        foreach (var file in files
                            .Where(f => !string.IsNullOrEmpty(f?.FullPath))
                            .FilterByWildcards(f => f!.FullPath, wpFullPath)
                            .OrderBy(f => f.FullPath))
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

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
