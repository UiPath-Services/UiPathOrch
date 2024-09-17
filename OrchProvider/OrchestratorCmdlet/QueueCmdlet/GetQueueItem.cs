using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security.AccessControl;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchQueueItem")]
    [OutputType(typeof(Entities.QueueItem))]
    public class GetQueueItemCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(QueueNameCompleter<Name>))]
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
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

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
                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetRobotsFromFolder(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;
                    if (entities == null) continue;

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
                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetReviewers(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;
                    if (entities == null) continue;

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

            if (Robot != null && Robot.Length != 0)
            {
                var robots = drive.GetRobotsFromFolder(folder);
                filter.AddIfNotNull(robots
                    .SelectByWildcards(r => r?.Name, Robot)
                    .CreateOrFilter(r => $"RobotId eq {r.Id}"));
            }

            if (Reviewer != null && Reviewer.Length != 0)
            {
                var reviewers = drive.GetReviewers(folder);
                filter.AddIfNotNull(reviewers
                    .SelectByWildcards(r => r?.UserName, Reviewer)
                    .CreateOrFilter(r => $"ReviewerUserId eq {r.Id}"));
            }

            string ret = filter.CreateAndFilter(s => s);
            return $"&$filter=({ret})";
        }

        protected override void ProcessRecord()
        {
            ulong skip = Skip ?? 0;
            ulong first = First ?? ulong.MaxValue;

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            if (string.IsNullOrEmpty(OrderBy)) OrderBy = "EndProcessing";

            bool bOutCache = (
                Status == null &&
                Revision == null &&
                Priority == null &&
                Exception == null &&
                Robot == null &&
                Reviewer == null &&
                Skip == null && First == null);

            if (bOutCache)
            {
                WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");
            }

            if (bOutCache)
            {
                foreach (var (drive, folder) in drivesFolders)
                {
                    if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var queueItemsPerFolder) ?? false)
                    {
                        foreach (var queuesItems in queueItemsPerFolder.OrderBy(q => q.Key)) // キューの名前でソート
                        {
                            var queueName = queuesItems.Key;
                            var queueItemId_items = queuesItems.Value;

                            if (wpName != null && !wpName.Any(wp => wp.IsMatch(queueName))) continue;

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

            foreach (var (drive, folder) in drivesFolders)
            {
                IEnumerable<QueueDefinition> queues = null;
                try
                {
                    queues = drive.GetQueues(folder);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, folder));
                    continue;
                }

                foreach (var queue in queues
                    .FilterByWildcards(q => q?.Name, wpName)
                    .OrderBy(q => q.Name))
                {
                    try
                    {
                        var items = drive.GetQueueItems(folder, queue, MakeFilter(drive, folder, queue), skip, first, OrderBy, OrderAscending.IsPresent);

                        // Skip と First をサポートするコマンドレットでは OrderBy してはいけない
                        WriteObject(items, true);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, queue));
                    }
                    Thread.Sleep(600); // API call rate limit を回避するため待機する
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
            //            if (queueItems == null) continue;

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
}
