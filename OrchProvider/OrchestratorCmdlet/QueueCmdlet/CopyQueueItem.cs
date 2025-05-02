using System.Collections.Generic;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchQueueItem", SupportsShouldProcess = true)]
//[OutputType(typeof(Entities.QueueDefinition))]
class CopyQueueItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
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
        var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
        var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

        // コピー元とコピー先が同じなら、何もしない
        if (srcRootFolder == dstRootFolder) return;

        var wpName = Name.ConvertToWildcardPatternList();

        //string msg = "Copying queue items...";
        //using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, msg, msg);
        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.Queues.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                var srcQueues = srcDrive.Queues.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                var dstQueues = dstDrive.Queues.Get(dstFolder);

                foreach (var srcQueue in srcQueues.OrderBy(q => q.Name))
                {
                    var dstQueue = dstQueues.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, true) == 0);
                    if (dstQueue is null)
                    {
                        WriteWarning($"'{srcQueue.GetPSPath()}': A queue named as '{srcQueue.Name}' doesn't exist in {dstFolder.GetPSPath()}.");
                        continue;
                    }

                    string target = $"Queue items in '{srcQueue.GetPSPath()}' Destination: '{dstQueue.GetPSPath()}'";

#if false // ひとつずつトランザクションを取得して、ひとつずつコピーする実装。残念ながら、これは技術的に実装できない。。
                    // トランザクションを開始する API は、Robot からしか呼び出せないようになっている。
                    if (ShouldProcess(target, "Copy Queue Item"))
                    {
                        while (true)
                        {
                            try
                            {
                                //var item = srcDrive.OrchAPISession.StartTransaction(srcFolder.Id!.Value, payload);
                            }
                            catch (Exception ex)
                            {
                                WriteWarning(ex.Message);
                            }
                        }
                    }
#endif

#if true // bulk で取得して、bulk で更新する実装。
                    // もれなく処理できるのか、なんだか心配だな。。
                    // コピー元アイテムのステータスは Deleted になる。
                    if (ShouldProcess(target, "Copy Queue Item"))
                    {
                        string query = $"&$filter=((QueueDefinitionId eq {srcQueue.Id}) and (Status eq '0'))";

                        //using ProgressReporter reporterItem = new(this, 3, intReporterItemTotal, msgItem, msgItem);
                        //int previousCount = 0;
                        HashSet<Int64> accumulatedSucceedSrcItemIds = [];
                        HashSet<Int64> accumulatedFailedSrcItemIds = [];
                        while (true)
                        {
                            try
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();
                                //reporterItem?.WriteProgress((int)(skip % intReporterItemTotal), $"{(int)skip:D}/{strReporterItemTotal}");

                                #region srcQueue から、Status が New の items を一括取得(上限100コ)
                                var srcItems = srcDrive.GetQueueItems(srcFolder, srcQueue, query, 0, 100).ToDictionary(i => i.Id!.Value);
                                // すでに一度でもコピーに失敗したアイテムは除外する
                                foreach (var id in accumulatedFailedSrcItemIds)
                                {
                                    srcItems.Remove(id);
                                }
                                if (srcItems.Count == 0) break;
                                #endregion

                                #region dstQueue に items を一括追加、失敗したアイテムの Id は failedSrcItemIds に入る
                                BulkAddQueueItemsRequest payload = new()
                                {
                                    queueName = dstQueue.Name,
                                    commitType = "ProcessAllIndependently",
                                    queueItems = new QueueItemData[srcItems.Count]
                                };

                                int index = 0;
                                foreach (var srcItem in srcItems.Values)
                                {
                                    srcItem.SpecificContent ??= [];
                                    srcItem.SpecificContent["UiPathOrch_OriginalKey"] = srcItem.Key!;
                                    if (!string.IsNullOrEmpty(srcItem.Reference))
                                        srcItem.SpecificContent["UiPathOrch_OriginalReference"] = srcItem.Reference;
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
                                        Id = srcItem.Id // payload には載せないが、処理の都合上覚えておく
                                    };
                                }

                                var failedItems = dstDrive.OrchAPISession.BulkAddQueueItem(dstFolder.Id ?? 0, payload);
                                foreach (var failedDstItem in failedItems?.FailedItems ?? [])
                                {
                                    if (failedDstItem.Ordinal is null) continue;
                                    var failedPayloadItem = payload.queueItems[failedDstItem.Ordinal.Value];
                                    var failedItemKey = Int64.Parse(failedPayloadItem.SpecificContent!["Orch_OriginalKey"].ToString()!);
                                    if (accumulatedFailedSrcItemIds.Add(failedPayloadItem.Id!.Value))
                                    {
                                        string warning = $"'{srcQueue.GetPSPath()}': Copy item failed. Key: {failedItemKey}";
                                        if (!string.IsNullOrEmpty(failedPayloadItem.Reference))
                                            warning += $" Reference: '{failedPayloadItem.Reference}'.";
                                        WriteWarning(warning);
                                    }
                                }
                                #endregion

                                #region srcQueue の items にコメントを追加、削除すべき srcItemIdsRemoved を構築
                                List<QueueItem> srcItemIdsRemoved = [];
                                string comment = "This item was copied to ";
                                if (srcDrive != dstDrive)
                                {
                                    comment += $"{dstDrive._psDrive.Root} ";
                                }
                                comment += $"'{dstFolder.FullyQualifiedName}/{dstQueue.Name}' with UiPathOrch and removed.";
                                foreach (var srcItem in srcItems.Values)
                                {
                                    if (!accumulatedFailedSrcItemIds.Contains(srcItem.Id!.Value))
                                    {
                                        srcItemIdsRemoved.Add(srcItem);
                                        try
                                        {
                                            srcDrive.OrchAPISession.PostQueueItemComments(srcFolder.Id!.Value, srcItem.Id!.Value, comment);
                                        }
                                        catch { } // とりあえずサボっておくか。。
                                    }
                                }
                                #endregion

                                #region srcQueue の items を、一括削除
                                QueueItemDeleteBulkRequest payload2 = new()
                                {
                                    queueItems = srcItemIdsRemoved.Select(srcItem => new LongVersionedEntity()
                                    {
                                        Id = srcItem.Id,
                                        RowVersion = srcItem.RowVersion
                                    }).ToList()
                                };
                                srcDrive.OrchAPISession.DeleteBulkQueueItem(srcFolder.Id!.Value, payload2);
                                #endregion

                                Thread.Sleep(600); // API call rate limit を回避するため待機する
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(srcQueue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, srcQueue));
                            }
                        }
                    }
#endif
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
