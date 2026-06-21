using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestDataQueueItem")]
[OutputType(typeof(Entities.TestDataQueueItem))]
public class GetTestDataQueueItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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

    public static Dictionary<string, JsonElement>? DeserializeJson(string jsonString)
    {
        //var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
        var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        return result;
    }

    public static PSObject CreatePsObjectFromTestDataQueueItem(TestDataQueueItem item)
    {
        PSObject psObject = new();
        psObject.Properties.Add(new PSNoteProperty("Path", item.Path));
        psObject.Properties.Add(new PSNoteProperty("Id", item.Id));
        psObject.Properties.Add(new PSNoteProperty("IsConsumed", item.IsConsumed));

        var dic = DeserializeJson(item.ContentJson!);
        foreach (var keyValuePair in dic!)
        {
            psObject.Properties.Add(new PSNoteProperty(keyValuePair.Key, keyValuePair.Value));
        }
        return psObject;
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Two sequential phases so each progress bar has a known denominator. Phase 1 lists
        // test data queues per folder (parallel, cap=4); Phase 2 fetches items per queue.
        using var queuePool = OrchThreadPool.RunForEach(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => (object)df.folder,
            df => df.drive.TestDataQueues.Get(df.folder)
                .FilterByWildcards(e => e?.Name, wpName)
                .Select(q => (df.drive, df.folder, queue: q))
                .ToList());

        var queues = new List<(OrchDriveInfo drive, Folder folder, TestDataQueue queue)>();
        using (var reporter = new ProgressReporter(this, 1, queuePool.Count, "Listing test data queues"))
        {
            foreach (var task in queuePool)
            {
                try
                {
                    var found = queuePool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                    if (found is not null) queues.AddRange(found);
                }
                catch (OrchException ex)
                {
                    // Distinct ErrorId ("couldn't list queues") vs the item error below.
                    WriteError(new ErrorRecord(ex, "GetTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        using var itemPool = OrchThreadPool.RunForEach(
            queues,
            t => t.queue.GetPSPath(),
            t => (object)t.queue,
            t => t.drive.TestDataQueueItems.Get(t.folder, t.queue));

        using var itemReporter = new ProgressReporter(this, 1, itemPool.Count, "Getting test data queue items");
        foreach (var task in itemPool)
        {
            try
            {
                var entities = itemPool.GetResultWithProgress(task, itemReporter, cancelHandler.Token);
                WriteObject(entities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestDataQueueItemError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
