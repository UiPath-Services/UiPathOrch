using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchBucket", SupportsShouldProcess = true)]
public class RemoveBucketCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var entities = drive.Buckets.Get(folder);

                foreach (var bucket in entities
                    .FilterByWildcards(b => b?.Name, wpName)
                    .OrderBy(b => b.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(bucket.GetPSPath(), "Remove Bucket"))
                    {
                        try
                        {
                            drive.OrchAPISession.DeleteBucket(folder.Id ?? 0, bucket.Id ?? 0);
                            drive.Buckets.ClearCache(folder);
                            drive._dicBucketLinks = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(bucket.GetPSPath(), ex), "RemoveBucketError", ErrorCategory.InvalidOperation, bucket));
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetBucketError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // Multi-threaded version
    // Rewrote as single-threaded because it can be slower when HTTP calls are capped
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetBuckets(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var bucket in entities
    //                .FilterByWildcards(b => b.Name!, wpName)
    //                .OrderBy(b => b.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(bucket.GetPSPath(), "Remove Bucket"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveBucket(folder.Id ?? 0, bucket.Id ?? 0);
    //                        drive._dicBuckets?.TryRemove(folder.Id ?? 0, out _);
    //                        drive._dicBucketLinks = null;
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(bucket.GetPSPath(), ex), "RemoveBucketError", ErrorCategory.InvalidOperation, bucket));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetBucketError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
