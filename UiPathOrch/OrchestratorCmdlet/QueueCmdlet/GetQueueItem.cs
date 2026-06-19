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
public class GetQueueItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    // Item ids to fetch (comma-separated). Emitted as an OData "Id in (...)" filter, so it
    // goes through the same list query as the other filters (no by-id endpoint).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueItemIdCompleter))]
    public Int64[]? Id { get; set; }

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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class RobotCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.RobotsFromFolder.Get(df.folder));

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Reviewers.Get(df.folder));

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

    // Completes -Id from the QueueItems cache only (no network — completers must be fast):
    // suggests ids of items already loaded for the targeted queue(s), excluding ids the user
    // has already typed on the same -Id list.
    private class QueueItemIdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            // Scope to the bound -Name queue(s) (item.Name == queue.Name, stamped by the cache).
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            // Exclude ids already present in this -Id argument.
            var wpExclude = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var cached = drive.QueueItems.GetCache(folder)?.Values;
                if (cached is null) continue;

                foreach (var item in cached.OrderByDescending(i => i.Id))
                {
                    if (wpName is not null && (item.Name is null || !wpName.Any(w => w.IsMatch(item.Name)))) continue;

                    var idText = item.Id?.ToString();
                    if (string.IsNullOrEmpty(idText)) continue;
                    if (!wp.IsMatch(idText)) continue;
                    if (wpExclude is not null && wpExclude.Any(w => w.IsMatch(idText))) continue;

                    string tip = $"{idText}  {item.Status}  {item.Reference}";
                    yield return new CompletionResult(idText, idText, CompletionResultType.ParameterValue, tip);
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

        if (Id is not null && Id.Length > 0)
        {
            // OData `in` operator (single round trip for many ids).
            filter.Add($"Id in ({string.Join(",", Id)})");
        }

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

        filter.AddTimeRange("DueDate", DueDateAfter, DueDateBefore);
        filter.AddTimeRange("DeferDate", DeferDateAfter, DeferDateBefore);
        filter.AddTimeRange("StartProcessing", StartProcessingAfter, StartProcessingBefore);
        filter.AddTimeRange("EndProcessing", EndProcessingAfter, EndProcessingBefore);

        //filter.Add("Id gt 18288985"); // It seems Id can also be used in queries.
        // OData filter values use raw spaces and single quotes in the
        // expression builders above (e.g. "Status eq '0'"); strict
        // Orchestrator builds reject those as "Invalid OData query options"
        // unless the URL-encoded forms are sent. None of the QueueItem
        // filter values contain literal spaces or quotes inside themselves,
        // so a blanket replace is safe.
        string ret = (filter.CreateAndFilter(s => s) ?? "")
            .Replace(" ", "%20")
            .Replace("'", "%27");
        return $"&$filter={ret}";
    }

    // Ordering for the no-filter cache-output path. Extracted as a pure,
    // testable function. The default arm is the regression guard: -OrderBy
    // defaults to "Id" (the Orchestrator web UI default for QueueItems,
    // $orderby=Id desc). Previously the cache path switched only on the four
    // date fields, so the default "Id" matched no case and every item was
    // silently dropped. "Id" and any other unlisted value now order on the
    // monotonic Id, descending unless -OrderAscending.
    internal static IEnumerable<Entities.QueueItem> OrderQueueItemsForOutput(
        IEnumerable<Entities.QueueItem> items, string? orderBy, bool ascending) =>
        orderBy switch
        {
            "DueDate" => ascending ? items.OrderBy(i => i.DueDate) : items.OrderByDescending(i => i.DueDate),
            "DeferDate" => ascending ? items.OrderBy(i => i.DeferDate) : items.OrderByDescending(i => i.DeferDate),
            "StartProcessing" => ascending ? items.OrderBy(i => i.StartProcessing) : items.OrderByDescending(i => i.StartProcessing),
            "EndProcessing" => ascending ? items.OrderBy(i => i.EndProcessing) : items.OrderByDescending(i => i.EndProcessing),
            _ => ascending ? items.OrderBy(i => i.Id) : items.OrderByDescending(i => i.Id),
        };

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // 'DeferDate' is rejected by some Orchestrator builds with
        // "Invalid OData query options: The property 'DeferDate' cannot be
        // used in the $orderby query option." Use Id, which is universally
        // sortable and matches the original intent — the legacy
        // GetQueueItems API call appended a (non-spec) `&orderby=Id desc`
        // alongside the `$orderby` parameter to force Id ordering server-side.
        if (string.IsNullOrEmpty(OrderBy)) OrderBy = "Id";

        bool bOutCache = (
            Id is null &&
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
                // Flat (folderId → itemId) cache; regroup by item.Name for the
                // per-queue display structure.
                var allItems = drive.QueueItems.GetCache(folder)?.Values;
                if (allItems is null) continue;

                foreach (var queueGroup in allItems
                    .Where(i => i.Name is not null)
                    .GroupBy(i => i.Name!)
                    .OrderBy(g => g.Key))
                {
                    var queueName = queueGroup.Key;
                    if (wpName is not null && !wpName.Any(wp => wp.IsMatch(queueName))) continue;

                    WriteObject(OrderQueueItemsForOutput(queueGroup, OrderBy, OrderAscending.IsPresent), true);
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporterFolder = new(this, 1, drivesFolders.Count, "Folder");
        int indexFolder = 0;
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
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
            foreach (var queue in targetQueues.WithCancellation(cancelHandler.Token))
            {
                //reporterQueue.WriteProgress(++indexQueue, queue.GetPSPath());
                reporterQueue.WriteProgress(++indexQueue);

                int first = First ?? int.MaxValue; // first is reset per queue
                int skip = Math.Max(0, Skip ?? 0); // negative -Skip is meaningless; treat as 0 (it was otherwise cast to a huge $skip on the non-batched path)

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

                                // Final-page short-circuit: no more API calls, so skip the rate-limit wait.
                                if (items.Count < 100) break;
                                localSkip += items.Count;
                                localFirst -= first2;

                                // Rate-limit wait before the next page. Cancellable: WaitOne returns early
                                // if the token is signaled, so Ctrl+C isn't blocked for up to 600ms.
                                cancelHandler.Token.Sleep(600);
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
                    // Each robot/reviewer batch was $orderby-sorted server-side independently; re-sort the
                    // merged list so -First/-Skip slice the global order, not the per-batch concatenation.
                    IEnumerable<object> result = OrderQueueItemsForOutput(allItems.Cast<Entities.QueueItem>(), OrderBy, OrderAscending.IsPresent);
                    if (skip > 0) result = result.Skip(skip);
                    if (first < int.MaxValue) result = result.Take(first);
                    WriteObject(result, true);
                }
            }
        }
    }
}
