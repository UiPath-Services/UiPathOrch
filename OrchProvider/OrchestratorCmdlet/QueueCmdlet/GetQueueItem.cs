using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchQueueItem")]
[OutputType(typeof(Entities.QueueItem))]
public class GetQueueItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemStatusItems, int>))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemRevisionItems, int>))]
    [SupportsWildcards]
    public string[]? Revision { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemPriorityItems, int>))]
    [SupportsWildcards]
    public string[]? Priority { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<QueueItemExceptionItems, int>))]
    [SupportsWildcards]
    public string[]? Exception { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RobotCompleter))]
    [SupportsWildcards]
    public string[]? Robot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ReviewerCompleter))]
    [SupportsWildcards]
    public string[]? Reviewer { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DueDateAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DueDateBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DeferDateAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? DeferDateBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartProcessingAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartProcessingBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? EndProcessingAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? EndProcessingBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public int? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<QueueItemOrderableItems>))]
    public string? OrderBy { get; set; }

    [Parameter]
    public SwitchParameter OrderAscending { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
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

            // Exclude Names already selected by the parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.RobotsFromFolder.Get(df.folder));

            foreach (var result in results)
            {
                var (drive, folder) = result.Source;

                foreach (var e in result
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

            // Exclude Names already selected by the parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Reviewers.Get(df.folder));

            foreach (var result in results)
            {
                var (drive, folder) = result.Source;

                foreach (var user in result
                    .Where(e => wp.IsMatch(e?.UserName))
                    .ExcludeByWildcards(e => e?.UserName, wpUserName)
                    .OrderBy(e => e?.UserName))
                {
                    string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, user.UserName ?? "");
                    if (!string.IsNullOrEmpty(user.FullName))
                    {
                        tiphelp += $" ({user.FullName})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(user?.UserName), user?.UserName, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    // RobotId eq / ReviewerUserId eq causes API to return 400 when there are 21 or more items
    private const int SimpleFieldBatchSize = 15;

    private IReadOnlyList<long?>? ResolveRobotIds(OrchDriveInfo drive, Folder folder)
    {
        if (Robot is null || Robot.Length == 0) return null;
        var robots = drive.RobotsFromFolder.Get(folder);
        return robots.SelectByWildcards(r => r?.Name, Robot).Select(r => r.Id).ToList();
    }

    private IReadOnlyList<long?>? ResolveReviewerIds(OrchDriveInfo drive, Folder folder)
    {
        if (Reviewer is null || Reviewer.Length == 0) return null;
        var reviewers = drive.Reviewers.Get(folder);
        return reviewers.SelectByWildcards(r => r?.UserName, Reviewer).Select(r => r.Id).ToList();
    }

    private string MakeFilter(OrchDriveInfo drive, Folder folder, QueueDefinition queue,
        IReadOnlyList<long?>? robotIdBatch = null, IReadOnlyList<long?>? reviewerIdBatch = null)
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

        if (robotIdBatch is not null)
        {
            filter.AddIfNotNull(robotIdBatch.CreateOrFilter(r => $"RobotId eq {r}"));
        }

        if (reviewerIdBatch is not null)
        {
            filter.AddIfNotNull(reviewerIdBatch.CreateOrFilter(r => $"ReviewerUserId eq {r}"));
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

        //filter.Add("Id gt 18288985"); // It seems Id can also be used in queries.
        string ret = filter.CreateAndFilter(s => s);
        return $"&$filter={ret}";
    }

    private void WriteQueryUnavailableWarning(QueueDefinition queue, string fieldName)
    {
        WriteWarning($"{queue.GetPSPath()}: The {fieldName} of the last queue item is null, so subsequent queue items cannot be retrieved.");
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
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
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var queueItemsPerFolder) ?? false)
                {
                    foreach (var queuesItems in queueItemsPerFolder.OrderBy(q => q.Key)) // Sort by queue name
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
        using ProgressReporter reporterFolder = new(this, 1, drivesFolders.Count, "Folder");
        int indexFolder = 0;
        foreach (var (drive, folder) in drivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            if (drivesFolders.Count > 1)
            {
                reporterFolder.WriteProgress(++indexFolder, folder.GetPSPath());
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

            // Resolve robot/reviewer IDs per folder and split into batches
            // RobotId eq / ReviewerUserId eq causes API to return 400 when there are 21 or more items
            var allRobotIds = ResolveRobotIds(drive, folder);
            var allReviewerIds = ResolveReviewerIds(drive, folder);

            if (Robot is not null && Robot.Length != 0 && allRobotIds is not null && allRobotIds.Count == 0)
                continue; // No matching robots
            if (Reviewer is not null && Reviewer.Length != 0 && allReviewerIds is not null && allReviewerIds.Count == 0)
                continue; // No matching reviewers

            IReadOnlyList<IReadOnlyList<long?>?> robotBatches;
            if (allRobotIds is null || allRobotIds.Count <= SimpleFieldBatchSize)
                robotBatches = new List<IReadOnlyList<long?>?> { allRobotIds };
            else
                robotBatches = allRobotIds.Chunk(SimpleFieldBatchSize).Select(b => (IReadOnlyList<long?>?)b.ToList()).ToList();

            IReadOnlyList<IReadOnlyList<long?>?> reviewerBatches;
            if (allReviewerIds is null || allReviewerIds.Count <= SimpleFieldBatchSize)
                reviewerBatches = new List<IReadOnlyList<long?>?> { allReviewerIds };
            else
                reviewerBatches = allReviewerIds.Chunk(SimpleFieldBatchSize).Select(b => (IReadOnlyList<long?>?)b.ToList()).ToList();

            bool isBatched = robotBatches.Count > 1 || reviewerBatches.Count > 1;

            var targetQueues = queues
                .FilterByWildcards(q => q?.Name, wpName)
                .OrderBy(q => q.Name).ToList();
            using ProgressReporter reporterQueue = new(this, 2, targetQueues.Count, "Queue ");
            int indexQueue = 0;
            foreach (var queue in targetQueues)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                //reporterQueue.WriteProgress(++indexQueue, queue.GetPSPath());
                reporterQueue.WriteProgress(++indexQueue);

                int first = First ?? int.MaxValue; // first is reset per queue
                int skip = Skip ?? 0;

                int intReporterItemTotal = isBatched ? 0 : (int)first;
                using ProgressReporter reporterItem = new(this, 3, intReporterItemTotal, "Item  ");

                var allItems = isBatched ? new List<object>() : null;
                try
                {
                    foreach (var robotBatch in robotBatches)
                    {
                        foreach (var reviewerBatch in reviewerBatches)
                        {
                            string query = MakeFilter(drive, folder, queue, robotBatch, reviewerBatch);
                            int localSkip = isBatched ? 0 : skip;
                            int localFirst = isBatched ? int.MaxValue : first;

                            while (localFirst > 0)
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();
                                if (!isBatched) reporterItem?.WriteProgress(localSkip % intReporterItemTotal);

                                var first2 = int.Min(100, localFirst);
                                var items = drive.GetQueueItems(folder, queue, query, (ulong)localSkip, (ulong)first2, OrderBy, OrderAscending.IsPresent);
                                if (allItems is not null)
                                    foreach (var item in items) allItems.Add(item);
                                else
                                    WriteObject(items, true);
                                Thread.Sleep(600); // Wait to avoid API call rate limit

                                if (items.Count < 100) break;
                                localSkip += items.Count;
                                localFirst -= first2;
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
                    WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, queue));
                }

                if (allItems is not null)
                {
                    IEnumerable<object> result = allItems;
                    if (skip > 0) result = result.Skip(skip);
                    if (first < int.MaxValue) result = result.Take(first);
                    WriteObject(result, true);
                }
            }
        }


        // Multi-threaded implementation. Since there is a rate limit, it's better to use single thread with waits.
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

        //            // Cmdlets that support Skip and First must not OrderBy
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
