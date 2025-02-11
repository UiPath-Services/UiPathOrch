using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchQueueItem")]
[OutputType(typeof(Entities.QueueItem))]
public class GetQueueItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemStatusItems, int>))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemRevisionItems, int>))]
    [SupportsWildcards]
    public string[]? Revision { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemPriorityItems, int>))]
    [SupportsWildcards]
    public string[]? Priority { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemExceptionItems, int>))]
    [SupportsWildcards]
    public string[]? Exception { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(RobotCompleter))]
    [SupportsWildcards]
    public string[]? Robot { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ReviewerCompleter))]
    [SupportsWildcards]
    public string[]? Reviewer { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DueDateAfter { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DueDateBefore { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DeferDateAfter { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DeferDateBefore { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartProcessingAfter { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartProcessingBefore { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? EndProcessingAfter { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? EndProcessingBefore { get; set; }

    [Parameter]
    public int? Skip { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public int? First { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<QueueItemOrderableItems>))]
    public string? OrderBy { get; set; }

    [Parameter]
    public SwitchParameter OrderAscending { get; set; }

    [Parameter]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class RobotCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.RobotsFromFolder.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;
                if (entities is null) continue;

                var (drive, folder) = result.Source;

                foreach (var e in entities
                    .Where(e => wp.IsMatch(e?.Name))
                    .ExcludeByWildcards(e => e?.Name, wpUserName)
                    .OrderBy(e => e?.Name))
                {
                    string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, e.Name ?? "");
                    if (!string.IsNullOrEmpty(e.Username))
                    {
                        tiphelp += $" ({e.Username})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(e?.Name), e?.Name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    private class ReviewerCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Reviewers.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;
                if (entities is null) continue;

                var (drive, folder) = result.Source;

                foreach (var e in entities
                    .Where(e => wp.IsMatch(e?.UserName))
                    .ExcludeByWildcards(e => e?.UserName, wpUserName)
                    .OrderBy(e => e?.UserName))
                {
                    string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, e.UserName ?? "");
                    if (!string.IsNullOrEmpty(e.FullName))
                    {
                        tiphelp += $" ({e.FullName})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(e?.UserName), e?.UserName, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    private string MakeFilter(OrchDriveInfo drive, Folder folder, QueueDefinition queue)
    {
        List<string> filter = [$"(QueueDefinitionId eq {queue.Id})"];

        filter.AddIfNotNull(QueueItemStatusItems.Items
            .SelectByWildcards(i => i.Key, Status)
            .CreateOrFilter(i => $"Status eq '{i.Value}'"));

        filter.AddIfNotNull(QueueItemRevisionItems.Items
            .SelectByWildcards(i => i.Key, Revision)
            .CreateOrFilter(i => $"ReviewStatus eq '{i.Value}'"));

        filter.AddIfNotNull(QueueItemPriorityItems.Items
            .SelectByWildcards(i => i.Key, Priority)
            .CreateOrFilter(i => $"Priority eq '{i.Value}'"));

        filter.AddIfNotNull(QueueItemExceptionItems.Items
            .SelectByWildcards(i => i.Key, Exception)
            .CreateOrFilter(i => $"ProcessingExceptionType eq '{i.Value}'"));

        if (Robot is not null && Robot.Length != 0)
        {
            var robots = drive.RobotsFromFolder.Get(folder);
            filter.AddIfNotNull(robots
                .SelectByWildcards(r => r?.Name, Robot)
                .CreateOrFilter(r => $"RobotId eq {r.Id}"));
        }

        if (Reviewer is not null && Reviewer.Length != 0)
        {
            var reviewers = drive.Reviewers.Get(folder);
            filter.AddIfNotNull(reviewers
                .SelectByWildcards(r => r?.UserName, Reviewer)
                .CreateOrFilter(r => $"ReviewerUserId eq {r.Id}"));
        }

        if (DueDateAfter is not null)
        {
            filter.Add($"(DueDate ge {DueDateAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (DueDateBefore is not null)
        {
            filter.Add($"(DueDate lt {DueDateBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (DeferDateAfter is not null)
        {
            filter.Add($"(DeferDate ge {DeferDateAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (DeferDateBefore is not null)
        {
            filter.Add($"(DeferDate lt {DeferDateBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (StartProcessingAfter is not null)
        {
            filter.Add($"(StartProcessing ge {StartProcessingAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (StartProcessingBefore is not null)
        {
            filter.Add($"(StartProcessing lt {StartProcessingBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (EndProcessingAfter is not null)
        {
            filter.Add($"(EndProcessing ge {EndProcessingAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        if (EndProcessingBefore is not null)
        {
            filter.Add($"(EndProcessing lt {EndProcessingBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }

        //filter.Add("Id gt 18288985"); // Id も query に使えるようだ。
        string ret = filter.CreateAndFilter(s => s);
        return $"&$filter={ret}";
    }

    private void WriteQueryUnavailableWarning(QueueDefinition queue, string fieldName)
    {
        WriteWarning($"{queue.GetPSPath()}: The {fieldName} of the last queue item is null, so subsequent queue items cannot be retrieved.");
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        if (string.IsNullOrEmpty(OrderBy)) OrderBy = "DeferDate";

        bool bOutCache = (
            Status is null &&
            Revision is null &&
            Priority is null &&
            Exception is null &&
            Robot is null &&
            Reviewer is null &&
            Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var queueItemsPerFolder) ?? false)
                {
                    foreach (var queuesItems in queueItemsPerFolder.OrderBy(q => q.Key)) // キューの名前でソート
                    {
                        var queueName = queuesItems.Key;
                        var queueItemId_items = queuesItems.Value;

                        if (wpName is not null && !wpName.Any(wp => wp.IsMatch(queueName))) continue;

                        switch (OrderBy)
                        {
                            case "DueDate":
                                if (OrderAscending.IsPresent)
                                    WriteObject(queueItemId_items.Values.OrderBy(i => i.DueDate), true);
                                else
                                    WriteObject(queueItemId_items.Values.OrderByDescending(i => i.DueDate), true);
                                break;
                            case "DeferDate":
                                if (OrderAscending.IsPresent)
                                    WriteObject(queueItemId_items.Values.OrderBy(i => i.DeferDate), true);
                                else
                                    WriteObject(queueItemId_items.Values.OrderByDescending(i => i.DeferDate), true);
                                break;
                            case "StartProcessing":
                                if (OrderAscending.IsPresent)
                                    WriteObject(queueItemId_items.Values.OrderBy(i => i.StartProcessing), true);
                                else
                                    WriteObject(queueItemId_items.Values.OrderByDescending(i => i.StartProcessing), true);
                                break;
                            case "EndProcessing":
                                if (OrderAscending.IsPresent)
                                    WriteObject(queueItemId_items.Values.OrderBy(i => i.EndProcessing), true);
                                else
                                    WriteObject(queueItemId_items.Values.OrderByDescending(i => i.EndProcessing), true);
                                break;
                        }
                    }
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        string msgFolder = "Folder";
        using ProgressReporter reporterFolder = new(this, 1, drivesFolders.Count, msgFolder, msgFolder);
        int indexFolder = 0;
        foreach (var (drive, folder) in drivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            if (drivesFolders.Count > 1)
            {
                reporterFolder.WriteProgress(++indexFolder, $"{indexFolder:D}/{drivesFolders.Count} {folder.GetPSPath()}");
            }

            IEnumerable<QueueDefinition> queues = null;
            try
            {
                queues = drive.Queues.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            var targetQueues = queues
                .FilterByWildcards(q => q?.Name, wpName)
                .OrderBy(q => q.Name).ToList();
            string msgQueue = "Queue ";
            using ProgressReporter reporterQueue = new(this, 2, targetQueues.Count, msgQueue, msgQueue);
            int indexQueue = 0;
            foreach (var queue in targetQueues)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                //reporterQueue.WriteProgress(++indexQueue, $"{indexQueue:D}/{drivesFolders.Count} {queue.GetPSPath()}");
                reporterQueue.WriteProgress(++indexQueue, $"{indexQueue:D}/{targetQueues.Count}");

                string query = MakeFilter(drive, folder, queue);
                int first = First ?? int.MaxValue; // first は、キューごとにリセット
                int skip = Skip ?? 0;

                string msgItem  = "Item  ";
                int intReporterItemTotal;
                string strReporterItemTotal;
                if (first > int.MaxValue)
                {
                    intReporterItemTotal = 1000;
                    strReporterItemTotal = "Unknown";
                }
                else
                {
                    intReporterItemTotal = (int)first;
                    strReporterItemTotal = $"{first}";
                }
                using ProgressReporter reporterItem = new(this, 3, intReporterItemTotal, msgItem, msgItem);
                try
                {
                    while (first > 0)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();
                        reporterItem?.WriteProgress((int)(skip % intReporterItemTotal), $"{(int)skip:D}/{strReporterItemTotal}");

                        var first2 = int.Min(100, first);
                        var items = drive.GetQueueItems(folder, queue, query, (ulong)skip, (ulong)first2, OrderBy, OrderAscending.IsPresent);
                        WriteObject(items, true);
                        Thread.Sleep(600); // API call rate limit を回避するため待機する

                        if (items.Count < 100) break;
                        skip += items.Count;
                        first -= first2;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, queue));
                }
            }
        }


        // multi-threaded な実装。rate limit があるから、single thread で待機しながら回した方が良い。
        //using var cancelHandler = new ConsoleCancelHandler();
        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    IEnumerable<QueueDefinition> queues = null;
        //    try
        //    {
        //        queues = drive.GetQueues(folder);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, folder));
        //        continue;
        //    }

        //    if (bOutCache)
        //    {
        //        foreach (var queue in queues)
        //        {
        //            if (drive._dicQueueItems?.TryGetValue(queue.Id!.Value, out var cachedItems) ?? false)
        //            {
        //                WriteObject(cachedItems.Values.OrderBy(i => i.Id), true);
        //            }
        //        }
        //        continue;
        //    }

        //    using var items = OrchThreadPool.RunForEach(queues.FilterByWildcards(q => q?.Name, Name).OrderBy(q => q.Name),
        //        queue => folder.GetPSPath(),
        //        queue => folder,
        //        queue => drive.GetQueueItems(folder, queue, MakeFilter(drive, folder, queue), skip, first));

        //    foreach (var item in items)
        //    {
        //        try
        //        {
        //            var queueItems = item.GetResult(cancelHandler.Token);
        //            if (queueItems is null) continue;

        //            // Skip と First をサポートするコマンドレットでは OrderBy してはいけない
        //            foreach (var queueItem in queueItems)
        //            {
        //                WriteObject(queueItem);
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetQueueItemError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
