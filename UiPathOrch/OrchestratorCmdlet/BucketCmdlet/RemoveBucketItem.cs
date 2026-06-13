using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchBucketItem", SupportsShouldProcess = true)]
public class RemoveBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketFullPathCompleter))]
    [SupportsWildcards]
    public string[]? FullPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpFullPath = FullPath.ConvertToWildcardPatternList();

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

                var (drive, folder) = result.Source;

                foreach (var bucket in entities
                    .FilterByNames(e => e?.Name, Name)
                    .OrderBy(e => e.Name))
                {
                    ICollection<Entities.BlobFile> files = null;

                    try
                    {
                        files = drive.BucketFiles.Get(folder, bucket);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(bucket.GetPSPath(), ex), "GetBucketFilesError", ErrorCategory.InvalidOperation, bucket));
                        continue;
                    }

                    foreach (var file in files
                        .FilterByWildcards(file => file?.FullPath, wpFullPath)
                        .OrderBy(file => file.FullPath).WithCancellation(cancelHandler.Token))
                    {
                        string target = System.IO.Path.Combine(bucket.GetPSPath(), file.FullPath ?? "");
                        bool bRemoved = false;
                        if (ShouldProcess(target, "Remove BucketItem"))
                        {
                            try
                            {
                                drive.OrchAPISession.DeleteBucketItem(folder.Id!.Value, bucket.Id!.Value, file.FullPath!);
                                bRemoved = true;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveBucketItemError", ErrorCategory.InvalidOperation, bucket));
                                continue;
                            }
                        }
                        if (bRemoved)
                        {
                            drive.BucketFiles.ClearCache(folder, bucket.Id!.Value);
                        }
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
