using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(QueueItem))]
public class CopyQueueItemCommand : OrchestratorPSCmdlet
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

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporterQueue = new(this, 3, int.MaxValue, "Copying new items");

        // 処理対象とするキューの数を数える
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
                    reporterQueue.WriteProgress(idxQueue, string.Format(progressNew, 0)); // 最後のゼロは処理中のアイテム数だ。

                    string target = $"Items in '{srcQueue.GetPSPath()}' Destination: '{dstQueue.GetPSPath()}'";

                    if (ShouldProcess(target, "Copy New Items"))
                    {
                        string query = $"&$filter=((QueueDefinitionId eq {srcQueue.Id}) and (Status eq '0'))";

                        #region srcItems に追加するコメントを構築しておく
                        // この処理はいったんしないでおくか。。
                        //string comment = "This item was copied to ";
                        //if (srcDrive._psDrive.Root != dstDrive._psDrive.Root)
                        //{
                        //    comment += $"{dstDrive._psDrive.Root} ";
                        //}
                        //comment += $"'{dstFolder.FullyQualifiedName}/{dstQueue.Name}' with UiPathOrch.";
                        #endregion

                        ulong first = 0;

                        // このループ内で、srcQueue の items を100個ずつ取得して、dstQueue に100個ずつ追加する。
                        DateTime getQueueItemsCalled = DateTime.Now;
                        while (true)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            List<QueueItem> srcItems;
                            try // srcQueue から、Status が New の items を一括取得(上限100コ)
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
                                #region dstQueue に items を一括追加、失敗したアイテムの Id は failedSrcItemIds に入る
                                BulkAddQueueItemsRequest payload = new()
                                {
                                    queueName = dstQueue.Name,
                                    commitType = "ProcessAllIndependently",
                                    queueItems = new QueueItemData[srcItems.Count]
                                };

                                int index = 0;
                                foreach (var srcItem in srcItems)
                                {
#if false // コピー先のアイテムの SpecificContent に、コピー元アイテムの情報を追記するのはどうなのか。あった方が安心な気もするが、
                                    srcItem.SpecificContent ??= [];

                                    if (srcDrive._psDrive.Root != dstDrive._psDrive.Root)
                                    {
                                        srcItem.SpecificContent.TryAdd("UiPathOrch_SourceTenant", srcDrive._psDrive.Root ?? "");
                                    }
                                    srcItem.SpecificContent.TryAdd("UiPathOrch_SourceFolder", srcFolder.FullyQualifiedName ?? "");
                                    srcItem.SpecificContent.TryAdd("UiPathOrch_SourceQueue", srcQueue.Name ?? "");
                                    srcItem.SpecificContent.TryAdd("UiPathOrch_SourceItemKey", srcItem.Key ?? "");
#endif
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
                                        Id = srcItem.Id,  // payload には載せないが、処理の都合上覚えておく
                                        Key = srcItem.Key // payload には載せないが、処理の都合上覚えておく
                                    };
                                }

                                var failedItems = dstDrive.OrchAPISession.BulkAddQueueItem(dstFolder.Id ?? 0, payload);
                                Dictionary<Int64, QueueItem> copiedSrcItems = srcItems.ToDictionary(i => i.Id!.Value);
#endregion

                                #region コピーに失敗したアイテムを出力し、画面に警告を出力
                                HashSet<Int64> failedSrcItemIds = [];
                                foreach (var failedDstItem in failedItems?.FailedItems ?? [])
                                {
                                    if (failedDstItem.Ordinal is null) continue;
                                    var failedPayloadItem = payload.queueItems[failedDstItem.Ordinal.Value-1]; // Ordinal は 1ベースのようだ
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

                                // コピーに成功した srcQueue の items を出力
                                WriteObject(copiedSrcItems.Values, true);

                                #region コピーが成功した srcQueue のすべての items に同じコメントを追加
                                // ちと処理が遅いな。いらないか。。
#if false
                                foreach (var srcItem in copiedSrcItems.Values)
                                {
                                    try
                                    {
                                        srcDrive.OrchAPISession.PostQueueItemComments(srcFolder.Id!.Value, srcItem.Id!.Value, comment);
                                    }
                                    catch { } // とりあえずサボっておくか。。
                                }
#endif
#endregion
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(dstQueue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, dstQueue));
                            }

                            Thread.Sleep(int.Max(0, (int)(601 - (DateTime.Now - getQueueItemsCalled).TotalMilliseconds))); // API call rate limit を回避するため待機する
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
