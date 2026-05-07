using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Redo, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(BulkOperationResponse))]
public class RedoQueueItemCommand : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo Drive, Folder Folder, QueueDefinition Queue), HashSet<Int64>>? _csvLines = null;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    //[Parameter]
    //public SwitchParameter Force { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private static string MakeFilter(QueueDefinition queue) =>
        $"&$filter=((QueueDefinitionId eq {queue.Id!.Value}) and (Status eq '2') and (ReviewStatus eq '0' or ReviewStatus eq '1'))";

    internal class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Only target Names already selected by the parameter
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var wpId = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

            foreach (var result in results)
            {
                var (drive, folder) = result.Source;

                foreach (var queue in result
                    .FilterByWildcards(q => q?.Name, wpName)
                    .OrderBy(q => q.Name))
                {
                    List<QueueItem> items = null;

                    #region Get cached items
                    if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var itemsPerFolder) ?? false)
                    {
                        if (itemsPerFolder.TryGetValue(queue.Name!, out var itemsPerQueue))
                        {
                            if (itemsPerQueue is not null)
                            {
                                items = itemsPerQueue.Values.ToList();
                            }
                        }
                    }
                    #endregion

                    // If the cache is empty, retrieve the first 100 retryable items
                    if (items is null)
                    {
                        items = drive.GetQueueItems(folder, queue, MakeFilter(queue), 0, 100);
                    }

                    if (items is null) continue;

                    foreach (var item in items
                        .Where(i => wp.IsMatch(i.Id!.Value.ToString()))
                        .Where(i => i.Status == "Failed" && i.ReviewStatus != "Retried" && i.ReviewStatus != "Verified")
                        .ExcludeByWildcards(i => i!.Id.ToString(), wpId)
                        .OrderBy(i => i.Id))
                    {
                        string tooltip = TipHelp(item);
                        string strId = item.Id.ToString();
                        yield return new CompletionResult(strId, strId, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _csvLines ??= [];

        var drivesFolders = SessionState.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        foreach (var (drive, folder) in drivesFolders)
        {
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

            var targetQueues = queues.FilterByWildcards(q => q!.Name, wpName);

            foreach (var queue in targetQueues)
            {
                if (!_csvLines.TryGetValue((drive, folder, queue), out var ids))
                {
                    ids = [];
                    _csvLines[(drive, folder, queue)] = ids;
                }

                ids.UnionWith(Id!);
            }
        }
    }

    //private bool SkipItem(QueueItem item, string tiphelp)
    //{
    //    if (item.Status != "Failed")
    //    {
    //        if (MyInvocation.BoundParameters.ContainsKey("Verbose"))
    //        {
    //            WriteWarning($"{tiphelp}: Skipped because the status is '{item.Status}', not 'Failed'.");
    //        }
    //        return true;
    //    }

    //    if (item.ReviewStatus == "Retried" || item.ReviewStatus == "Verified")
    //    {
    //        if (MyInvocation.BoundParameters.ContainsKey("Verbose"))
    //        {
    //            WriteWarning($"{tiphelp}: Skipped because it has already been '{item.ReviewStatus}'.");
    //        }
    //        return true;
    //    }
    //    return false;
    //}

    protected override void EndProcessing()
    {
        if (_csvLines is null) return;

        var drivesFolders = SessionState.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var csvLine in _csvLines)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            var (drive, folder, queue) = csvLine.Key;
            var ids = csvLine.Value.Order();

            Dictionary<Int64, QueueItem> retryableQueueItems = [];
            try
            {
                // Retrieve retryable items. The upper limit for items is 1000.
                ulong skip = 0;
                while (skip < 1000)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var items = drive.GetQueueItems(folder, queue, MakeFilter(queue), skip, 100);

                    foreach (var item in items)
                    {
                        retryableQueueItems[item.Id!.Value] = item;
                    }

                    if (items.Count < 100) break;
                    skip += (ulong)items.Count;

                    // Cancellable rate-limit wait — Ctrl+C bails out of the
                    // 600 ms sleep promptly instead of always burning the
                    // full delay.
                    cancelHandler.Token.WaitHandle.WaitOne(600);
                }

            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "GetQueueItemsError", ErrorCategory.InvalidData, queue));
                continue;
            }

            List<QueueItem> retryingItems = [];

            foreach (var id in ids)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                QueueItem item = null;
                bool found = retryableQueueItems.TryGetValue(id, out item);

                // Querying one by one is slow, so we don't do it.
                // We could allow querying only when -Force is specified,
                // but that feels like over-engineering..
                //if (!found && Force)
                //{
                //    try
                //    {
                //        item = drive.GetQueueItemById(folder, queue, id);
                //    }
                //    catch (Exception ex)
                //    {
                //        WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidData, queue));
                //        continue;
                //    }
                //    if (item is null) continue;
                //    string tiphelp2 = OrchArgumentCompleter.TipHelp(item);
                //    if (SkipItem(item, tiphelp2)) continue;
                //}

                if (!found || item is null) continue;

                string tiphelp = OrchArgumentCompleter.TipHelp(item);

                // This TipHelp should be an extension method. It should be placed alongside GetPSPath().
                if (ShouldProcess(tiphelp, "Retry QueueItem"))
                {
                    retryingItems.Add(item);
                }
            }

            if (retryingItems.Count == 0) continue;

            List<RetryQueueItem> payload = [];

            foreach (var retryingItem in retryingItems)
            {
                RetryQueueItem p = new()
                {
                    Id = retryingItem.Id,
                    RowVersion = retryingItem.RowVersion
                };
                payload.Add(p);
            }

            try
            {
                var result = drive.OrchAPISession.RetryQueueItem(folder.Id!.Value, payload);
                if (result is not null)
                {
                    result.Path = folder.GetPSPath();
                    result.PathQueue = queue.GetPSPath();
                    result.Queue = queue.Name;
                    WriteObject(result);
                }
                drive._dicQueueItems?.TryRemove(folder.Id.Value, out _);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "RetryQueueItem", ErrorCategory.InvalidData, queue));
            }
        }
    }
}
