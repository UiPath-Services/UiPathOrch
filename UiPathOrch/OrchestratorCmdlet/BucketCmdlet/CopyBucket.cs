using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchBucket", SupportsShouldProcess = true)]
public class CopyBucketCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
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
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);
        var srcDrivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);

        // If source and destination are the same, do nothing
        if (srcRootFolder == dstRootFolder) return;

        var wpName = Name.ConvertToWildcardPatternList();

        using var reporterBuckets = new ProgressReporter(this, 1000, Int32.MaxValue, "Copying buckets...");
        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // If there are no entities to copy, there is no need to look up the dstFolder
                //srcDrive._dicBuckets?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.Buckets.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetBucketError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                Core.OrchProvider.CopyBuckets(this,
                    srcDrive, srcFolder, wpName,
                    dstDrive, dstFolder, reporterBuckets,
                    false, cancelHandler.Token);
                dstDrive.Buckets.ClearCache(dstFolder);
                dstDrive._dicBucketLinks = null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyBucketError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
