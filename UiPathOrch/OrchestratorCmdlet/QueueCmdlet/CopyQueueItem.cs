using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(QueueItem))]
public class CopyQueueItemCmdlet : OrchestratorPSCmdlet
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

        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporterQueue = new(this, 3, int.MaxValue, "Copying new items");

        // Count the number of queues to be processed
        reporterQueue.TotalNum = srcDrivesFolders.CountEntities(
            drive => drive.Queues,
            e => e.FilterByWildcards(q => q?.Name, wpName)
        );

        int idxQueue = 0;
        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, createIfMissing: true, createCache: dstFolderCache);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                var srcQueues = srcDrive.Queues.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
                if (srcQueues.Count == 0) continue;

                var dstQueues = dstDrive.Queues.Get(dstFolder);

                foreach (var srcQueue in srcQueues.OrderBy(q => q.Name))
                {
                    ++idxQueue;
                    var dstQueue = dstQueues.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstQueue is null)
                    {
                        WriteWarning($"'{srcQueue.GetPSPath()}': A queue named as '{srcQueue.Name}' doesn't exist in {dstFolder.GetPSPath()}.");
                        continue;
                    }

                    string progressNew = $"{{0}} {srcQueue.GetPSPath()} to {dstQueue.GetPSPath()}";
                    reporterQueue.WriteProgress(idxQueue, string.Format(progressNew, 0)); // The trailing zero is the number of items being processed.

                    string target = $"Items in '{srcQueue.GetPSPath()}' Destination: '{dstQueue.GetPSPath()}'";

                    if (ShouldProcess(target, "Copy New Items"))
                    {
                        string query = $"&$filter=((QueueDefinitionId eq {srcQueue.Id}) and (Status eq '0'))";

                        ulong first = 0;

                        // In this loop, retrieve srcQueue items 100 at a time and add them to dstQueue 100 at a time.
                        while (true)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            // Pace each GetQueueItems -> BulkAdd cycle to >= 601 ms to avoid the API rate limit.
                            DateTime cycleStarted = DateTime.Now;

                            List<QueueItem> srcItems;
                            try // Bulk-retrieve items with Status "New" from srcQueue (up to 100)
                            {
                                srcItems = srcDrive.GetQueueItems(srcFolder, srcQueue, query, 100 * first, 100);
                                if (srcItems.Count == 0) break;
                                reporterQueue.WriteProgress(idxQueue, string.Format(progressNew, (first++ * 100) + (ulong)srcItems.Count));
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(srcQueue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, srcQueue));
                                break;
                            }

                            // Build the bulk-add payload for this batch. Id/Key are carried (not
                            // serialized) so a rejected ordinal maps back to its source item.
                            var payload = new QueueItemData[srcItems.Count];
                            for (int i = 0; i < srcItems.Count; i++)
                            {
                                var srcItem = srcItems[i];
                                payload[i] = new QueueItemData()
                                {
                                    Name = dstQueue.Name,
                                    Priority = srcItem.Priority,
                                    SpecificContent = srcItem.SpecificContent,
                                    DeferDate = srcItem.DeferDate,
                                    DueDate = srcItem.DueDate,
                                    RiskSlaDate = srcItem.RiskSlaDate,
                                    Reference = srcItem.Reference,
                                    Progress = srcItem.Progress,
                                    Id = srcItem.Id,  // not serialized; kept to map a failed ordinal back
                                    Key = srcItem.Key // not serialized; kept to map a failed ordinal back
                                };
                            }

                            BulkOperationResponseDtoOfFailedQueueItem? response;
                            try
                            {
                                response = dstDrive.OrchAPISession.BulkAddQueueItem(dstFolder.Id ?? 0, new BulkAddQueueItemsRequest
                                {
                                    queueName = dstQueue.Name,
                                    commitType = "ProcessAllIndependently",
                                    queueItems = payload
                                });
                            }
                            catch (Exception ex)
                            {
                                // The whole batch failed (the chokepoint already retried any transient
                                // 429/503/504). The fetch loop advances by skip and the copy does not
                                // change the source items' status, so this batch is never re-fetched.
                                // Report it and emit every source item as failed so it can be retried
                                // (its Id alone is enough to re-copy it).
                                WriteError(new ErrorRecord(new OrchException(dstQueue.GetPSPath(), ex), "CopyQueueItemError", ErrorCategory.InvalidOperation, dstQueue));
                                WriteObject(srcItems, true);
                                Thread.Sleep(int.Max(0, (int)(601 - (DateTime.Now - cycleStarted).TotalMilliseconds)));
                                continue;
                            }

                            // Map the items the server rejected back to their source item via Ordinal
                            // (1-based) and emit each as a failed source item. Its Id is enough to retry
                            // the copy (e.g. Get-OrchQueueItem -Id ... | Import-OrchQueueItem); the
                            // per-item warning carries the reason.
                            var failedIds = new HashSet<long>();
                            foreach (var failedDstItem in response?.FailedItems ?? [])
                            {
                                if (failedDstItem.Ordinal is null) continue;
                                int idx = failedDstItem.Ordinal.Value - 1;
                                if (idx < 0 || idx >= payload.Length) continue;
                                var failedItem = payload[idx];
                                if (failedItem.Id.HasValue) failedIds.Add(failedItem.Id.Value);

                                string warning = $"'{srcQueue.GetPSPath()}': Add item failed: {failedDstItem.ErrorMessage} Id: {failedItem.Id} Key: '{failedItem.Key}'";
                                if (!string.IsNullOrEmpty(failedItem.Reference))
                                    warning += $" Reference: '{failedItem.Reference}'.";
                                WriteWarning(warning);
                            }

                            // Emit the source items that failed to copy so they can be piped to a CSV
                            // and retried.
                            if (failedIds.Count > 0)
                                WriteObject(srcItems.Where(i => i.Id.HasValue && failedIds.Contains(i.Id!.Value)), true);

                            Thread.Sleep(int.Max(0, (int)(601 - (DateTime.Now - cycleStarted).TotalMilliseconds))); // Wait to avoid API call rate limit
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
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyQueueItemError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
