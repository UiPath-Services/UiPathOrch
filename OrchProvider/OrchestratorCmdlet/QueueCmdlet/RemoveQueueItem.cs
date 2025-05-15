using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Id_RowVersion;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchQueueItem", SupportsShouldProcess = true)]
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
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? RowVersion { get; set; }

    //[Parameter]
    private int? ChunkSize { get; set; } = 1000; // ChunkSize は 1000 に固定しておく

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    // キャッシュにある Id を列挙する
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

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // パラメータで選択済みの Id は、候補から除外する
            var ids = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Queues.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;
                var (drive, folder) = result.Source;

                if (drive._dicQueueItems?.TryGetValue(folder.Id!.Value, out var itemsPerFolder) ?? false)
                {
                    foreach (var q in result.Result
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
                                yield return new CompletionResult(PathTools.EscapePSText(id), id, CompletionResultType.ParameterValue, q.GetPSPath());
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _csvLines ??= [];
        var (drive, folder) = OrchDriveInfo.ResolveToSingleFolder(Path);
        var wpName = new string[] { Name! }.ConvertToWildcardPatternList();

        var queues = drive.Queues.Get(folder).FilterByWildcards(q => q?.Name, wpName);
        var cnt = queues.Count();
        if (cnt != 1)
        {
            string queuePath = System.IO.Path.Combine(folder.GetPSPath(), Name!);
            if (cnt == 0) throw new Exception($"\"{queuePath}\" does not match any queue.");
            if (cnt > 1) throw new Exception($"\"{queuePath}\" matches more than one queue.");
        }

        var queue = queues.FirstOrDefault();
        if (queue is null) return;

        foreach (var id in Id!.Distinct() ?? [])
        {
            _csvLines.Add(new(drive, folder, queue, id, RowVersion));
        }
    }

    protected override void EndProcessing()
    {
        //ChunkSize ??= 100;
        if (_csvLines is null || _csvLines.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporterQueue = new(this, 3, int.MaxValue, "Remove items");

        foreach (var groupedLines in _csvLines.GroupBy(l => l.Queue).OrderBy(g => g.Key.Name))
        {
            var queue = groupedLines.Key;
            string target = queue.GetPSPath();
            if (ShouldProcess(target, "Remove Queue Items"))
            {
                foreach (var line in groupedLines)
                {
                    // rowVersion が指定されていない場合
                    if (string.IsNullOrEmpty(line.RowVersion))
                    {
                        // まず、キャッシュの中から探す
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

                        // キャッシュになければ、API で取得する
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

                // ここまでで、削除すべきアイテムの一覧を作成できた。これを ChunkSize 個ずつ削除
                int idxRemove = 0;
                using ProgressReporter reporterRemove = new(this, 5, groupedLines.Count(), "Removing queue item");
                foreach (var chunk in groupedLines.Where(l => !string.IsNullOrEmpty(l.RowVersion)).Chunk(ChunkSize!.Value))
                {
                    var drive = chunk.First().Drive;
                    var folder = chunk.First().Folder;

                    reporterRemove.WriteProgress(idxRemove++ * ChunkSize!.Value + chunk.Length);
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
                        drive._dicQueueItems = null;
                        if (result?.FailedItems is not null && result.FailedItems.Length > 0)
                        {
                            WriteWarning($"\"{queue.GetPSPath()}\" Failed to remove items: {result.Message} Items: {string.Join(", ", result?.FailedItems?.Select(id => id.ToString()) ?? [])}");
                            var hash = chunk.ToDictionary(i => i.Id);
                            foreach (var failedItemId in result!.FailedItems)
                            {
                                if (hash.TryGetValue(failedItemId, out var item))
                                {
                                    WriteObject(item);
                                }
                                else // おそらくここが実行されることはないはずだが、
                                {
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
                        Thread.Sleep(600); // API call rate limit はないのだけど、入れた方が動作が安定するような気がする
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(queue.GetPSPath(), ex), "DeleteQueueItemError", ErrorCategory.InvalidOperation, queue));
                    }
                }
            }
        }
    }
}
