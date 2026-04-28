using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(QueueItem))]
public class CopyQueueItemCommand : OrchestratorPSCmdlet
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

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);
        var srcDrivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);

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
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                var srcQueues = srcDrive.Queues.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
                if (srcQueues.Count == 0) continue;

                var dstQueues = dstDrive.Queues.Get(dstFolder);

                foreach (var srcQueue in srcQueues.OrderBy(q => q.Name))
                {
                    ++idxQueue;
                    var dstQueue = dstQueues.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, true) == 0);
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

                        #region Build the comment to append to srcItems
                        // Let's skip this processing for now..
                        //string comment = "This item was copied to ";
                        //if (srcDrive._psDrive.Root != dstDrive._psDrive.Root)
                        //{
                        //    comment += $"{dstDrive._psDrive.Root} ";
                        //}
                        //comment += $"'{dstFolder.FullyQualifiedName}/{dstQueue.Name}' with UiPathOrch.";
                        #endregion

                        ulong first = 0;

                        // In this loop, retrieve srcQueue items 100 at a time and add them to dstQueue 100 at a time.
                        DateTime getQueueItemsCalled = DateTime.Now;
                        while (true)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

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

                            try
                            {
                                #region Bulk-add items to dstQueue; Ids of failed items go into failedSrcItemIds
                                BulkAddQueueItemsRequest payload = new()
                                {
                                    queueName = dstQueue.Name,
                                    commitType = "ProcessAllIndependently",
                                    queueItems = new QueueItemData[srcItems.Count]
                                };

                                int index = 0;
                                foreach (var srcItem in srcItems)
                                {
                                    payload.queueItems[index++] = new QueueItemData()
                                    {
                                        Name = dstQueue.Name,
                                        Priority = srcItem.Priority,
                                        SpecificContent = srcItem.SpecificContent,
                                        DeferDate = srcItem.DeferDate,
                                        DueDate = srcItem.DueDate,
                                        RiskSlaDate = srcItem.RiskSlaDate,
                                        Reference = srcItem.Reference,
                                        Progress = srcItem.Progress,
                                        //Source = item.
                                        //ParentOperationId = item.
                                        Id = srcItem.Id,  // Not included in the payload, but kept for processing purposes
                                        Key = srcItem.Key // Not included in the payload, but kept for processing purposes
                                    };
                                }

                                var failedItems = dstDrive.OrchAPISession.BulkAddQueueItem(dstFolder.Id ?? 0, payload);
                                Dictionary<Int64, QueueItem> copiedSrcItems = srcItems.ToDictionary(i => i.Id!.Value);
                                #endregion

                                #region Output items that failed to copy and display warnings on screen
                                HashSet<Int64> failedSrcItemIds = [];
                                foreach (var failedDstItem in failedItems?.FailedItems ?? [])
                                {
                                    if (failedDstItem.Ordinal is null) continue;
                                    var failedPayloadItem = payload.queueItems[failedDstItem.Ordinal.Value - 1]; // Ordinal appears to be 1-based
                                    if (failedSrcItemIds.Add(failedPayloadItem.Id!.Value))
                                    {
                                        string warning = $"'{srcQueue.GetPSPath()}': Add item failed: {failedDstItem.ErrorMessage} Id: {failedPayloadItem.Id} Key: '{failedPayloadItem.Key}'";
                                        if (!string.IsNullOrEmpty(failedPayloadItem.Reference))
                                            warning += $" Reference: '{failedPayloadItem.Reference}'.";
                                        WriteWarning(warning);
                                        copiedSrcItems.Remove(failedPayloadItem.Id.Value);
                                    }
                                }
                                #endregion

                                // Output the items from srcQueue that were successfully copied
                                WriteObject(copiedSrcItems.Values, true);

                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(dstQueue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, dstQueue));
                            }

                            Thread.Sleep(int.Max(0, (int)(601 - (DateTime.Now - getQueueItemsCalled).TotalMilliseconds))); // Wait to avoid API call rate limit
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
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyQueueError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
