using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchQueue", SupportsShouldProcess = true)]
public class CopyQueueCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(EffectivePath(Path, LiteralPath));
        var srcDrivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);
        var dstFolderCache = new Dictionary<string, Folder?>();

        // If the source and destination are the same, do nothing
        if (srcRootFolder == dstRootFolder) return;

        srcDrive.QueueLinks.ClearCache();

        // Since the cache is cleared just before copying, there is no point in retrieving it here

        using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, "Copying queues...");
        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // If there are no entities to copy, there is no need to look up the dstFolder
                //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.Queues.Get(srcFolder).FilterByNames(b => b?.Name, Name);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, createIfMissing: true, createCache: dstFolderCache);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                Core.OrchProvider.CopyQueues(this,
                    srcDrive, srcFolder, Name,
                    dstDrive, dstFolder, reporterQueues,
                    false, cancelHandler.Token);
                dstDrive.Queues.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyQueueError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
