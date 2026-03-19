using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(QueueItem))]
public class RemoveQueueItemCommand : OrchestratorPSCmdlet
{
    class CsvLine(OrchDriveInfo drive, Folder folder, QueueDefinition queue, long id, string? rowVersion)
    {
        public OrchDriveInfo Drive { get; set; } = drive;
        public Folder Folder { get; set; } = folder;
        public QueueDefinition Queue { get; set; } = queue;
        public long Id { get; set; } = id;
        public string? RowVersion { get; set; } = rowVersion;
    }
    List<CsvLine>? _csvLines = null;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? RowVersion { get; set; }

    //[Parameter]
    private int ChunkSize { get; set; } = 1000; // ChunkSize is fixed at 1000

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    // Enumerate Ids available in the cache
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

            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude Ids already selected by the parameter from the candidates
            var ids = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

            foreach (var queues in results)
            {
                var (drive, folder) = queues.Source;

                if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var itemsPerFolder) ?? false)
                {
                    foreach (var q in queues
                        .FilterByWildcards(q => q?.Name, wpName)
                        .OrderBy(q => q.Name))
                    {
                        if (itemsPerFolder.TryGetValue(q.Name ?? "", out var itemsPerQueue))
                        {
                            foreach (var item in itemsPerQueue.Values
                                .Where(i => i.Status != "Deleted")
                                .Where(i => wp.IsMatch(i.Id!.Value.ToString()))
                                .ExcludeByClassValues(i => i?.Id?.ToString(), ids)
                                .OrderBy(i => i.Id!.Value))
                            {
                                string id = item.Id!.Value.ToString();
                                string tiphelp = $"Id:{item.Id} Key:{item.Key}";
                                if (!string.IsNullOrEmpty(item.Reference)) tiphelp += $" Reference:{item.Reference}";
                                yield return new CompletionResult(PathTools.EscapePSText(id), id, CompletionResultType.ParameterValue, tiphelp);
                            }
                        }
                    }
                }
            }
        }
    }

    private QueueDefinition? _currentQueue = null;
    protected override void ProcessRecord()
    {
        #region Resolve the drive, folder, and queue for this line
        var (drive, folder) = SessionState.ResolveToSingleFolder(Path);
        var wpName = new string[] { Name! }.ConvertToWildcardPatternList();

        var queues = drive.Queues.Get(folder).FilterByWildcards(q => q?.Name, wpName).Take(2).ToList();
        if (queues.Count != 1)
        {
            string queuePath = System.IO.Path.Combine(folder.GetPSPath(), Name!);
            if (queues.Count == 0) throw new Exception($"\"{queuePath}\" does not match any queue.");
            if (queues.Count > 1) throw new Exception($"\"{queuePath}\" matches more than one queue.");
        }

        var queue = queues[0];
        if (queue is null) return; // Should never be null here, but just in case
        #endregion

        if (_currentQueue != queue)
        {
            _currentQueue = queue;
            if (_csvLines?.Count > 0)
            {
                // Delete the items contained in the accumulated _csvLines from the queue
                var csvLines = _csvLines;
                _csvLines = null; // Re-entry from multiple threads should not happen, but just in case
                RemoveItems(csvLines);
            }
        }

        _csvLines ??= [];
        foreach (var id in Id!.Distinct())
        {
            _csvLines.Add(new(drive, folder, queue, id, RowVersion));
        }
    }

    protected override void EndProcessing()
    {
        RemoveItems(_csvLines);
        _csvLines = null;
    }

    private void RemoveItems(List<CsvLine>? csvLines)
    {
        //ChunkSize ??= 100;
        if (csvLines is null || csvLines.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();

        // The csvLines passed to this RemoveItems() should only contain a single queue, so GroupBy is unnecessary.
        //foreach (var groupedLines in csvLines.GroupBy(l => l.Queue).OrderBy(g => g.Key.Name))
        var drive = csvLines.First().Drive;
        var folder = csvLines.First().Folder;
        var queue = csvLines.First().Queue;

        string target = queue.GetPSPath();
        if (ShouldProcess(target, "Remove Queue Items"))
        {
            #region Verify RowVersion for all _csvLines
            {
                using ProgressReporter reporterRowVersion = new(this, 105, csvLines.Count, "Confirming RowVersions");
                int idxRowVersion = 0;
                foreach (var line in csvLines)
                {
                    reporterRowVersion.WriteProgress(++idxRowVersion, queue.GetPSPath());

                    // If rowVersion is not specified
                    if (string.IsNullOrEmpty(line.RowVersion))
                    {
                        // First, look in the cache
                        if (line.Drive._dicQueueItems?.TryGetValue(line.Folder.Id!.Value, out var itemsPerFolder) ?? false)
                        {
                            if (itemsPerFolder?.TryGetValue(queue.Name!, out var itemsPerQueue) ?? false)
                            {
                                if (itemsPerQueue?.TryGetValue(line.Id, out var itemToRemove) ?? false)
                                {
                                    line.RowVersion = itemToRemove.RowVersion;
                                }
                            }
                        }

                        // If not in the cache, retrieve via API
                        if (string.IsNullOrEmpty(line.RowVersion))
                        {
                            QueueItem item = null;
                            try
                            {
                                item = line.Drive.GetQueueItemById(line.Folder, queue, line.Id);
                                line.RowVersion = item?.RowVersion;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(System.IO.Path.Combine(target, line.Id.ToString()), ex), "GetQueueItemError", ErrorCategory.InvalidOperation, queue));
                                continue;
                            }
                            if (string.IsNullOrEmpty(line.RowVersion))
                            {
                                WriteWarning($"\"{queue.GetPSPath()}\": No item found with Id {line.Id}.");
                                continue;
                            }
                        }
                    }
                }
            }
            #endregion

            var linesToRemove = csvLines.Where(l => !string.IsNullOrEmpty(l.RowVersion)).ToList();
            // At this point, the list of items to delete has been built. Delete them in chunks of ChunkSize

            int idxRemove = 0;
            using ProgressReporter reporterRemove = new(this, 5, linesToRemove.Count, "Removing items");
            foreach (var chunk in linesToRemove.Chunk(ChunkSize))
            {
                reporterRemove.WriteProgress(idxRemove++ * ChunkSize + chunk.Length, queue.GetPSPath());
                cancelHandler.Token.ThrowIfCancellationRequested();
                QueueItemDeleteBulkRequest payload = new()
                {
                    queueItems = chunk.Select(srcItem => new LongVersionedEntity()
                    {
                        Id = srcItem.Id,
                        RowVersion = srcItem.RowVersion
                    }).ToList()
                };
                try
                {
                    var result = drive.OrchAPISession.DeleteBulkQueueItem(folder.Id!.Value, payload);

                    // Get the cache
                    Dictionary<string, Dictionary<Int64, QueueItem>> itemsPerFolder = null;
                    drive._dicQueueItems?.TryGetValue(folder.Id.Value, out itemsPerFolder);

                    Dictionary<Int64, QueueItem> itemsPerQueue = null;
                    itemsPerFolder?.TryGetValue(queue.Name!, out itemsPerQueue);

                    // Output items that failed to delete. Better not to output RowVersion.
                    if (result?.FailedItems is not null && result.FailedItems.Length > 0)
                    {
                        WriteWarning($"\"{queue.GetPSPath()}\" Failed to remove items: {result.Message}");

                        foreach (var failedItemId in result.FailedItems)
                        {
                            if (itemsPerQueue?.TryGetValue(failedItemId, out var item) ?? false)
                            {
                                item.RowVersion = null;
                                WriteObject(item);
                            }
                            else
                            {
                                // When this cmdlet is run standalone, the cache may not exist..
                                WriteObject(new QueueItem()
                                {
                                    Id = failedItemId,
                                    Path = folder.GetPSPath(),
                                    Name = queue.Name,
                                    PathName = queue.GetPSPath()
                                });
                            }
                        }
                    }

                    // Regardless of whether item deletion succeeded or failed, the cache for processed items must be removed.
                    if (itemsPerQueue is not null)
                    {
                        foreach (var entry in payload.queueItems)
                        {
                            if (entry.Id is null) continue;
                            // If processed correctly, it might be enough to just set Status to Deleted instead of removing the cache,
                            // but the RowVersion should have changed..
                            itemsPerQueue.Remove(entry.Id.Value);
                        }
                    }
                    //Thread.Sleep(600); // There is no API call rate limit, but should we add it?
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "DeleteQueueItemError", ErrorCategory.InvalidOperation, queue));
                }
            }
        }
    }
}
