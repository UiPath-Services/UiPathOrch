using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestDataQueueItem")]
[OutputType(typeof(Entities.TestDataQueueItem))]
public class GetTestDataQueueItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

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
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Phase 1 = list TestDataQueues per (drive, folder); fanout to
        // (drive, folder, queue). Phase 2 = TestDataQueueItems.Get per queue.
        // Cap=4 shared across both phases via ChainedThreadPool — the
        // previous nested OrchThreadPool stacked to cap=4×4=16 against a
        // single Orchestrator.
        using var pool = OrchThreadPool.RunForEachChained(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => (object)df.folder,
            df => df.drive.TestDataQueues.Get(df.folder)
                .FilterByWildcards(e => e?.Name, wpName)
                .Select(q => (df.drive, df.folder, queue: q)),
            t => t.queue.GetPSPath(),
            t => (object)t.queue,
            t => t.drive.TestDataQueueItems.Get(t.folder, t.queue));

        foreach (var task in pool)
        {
            try
            {
                var entities = task.GetResult(cancelHandler.Token);
                WriteObject(entities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestDataQueueItemError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 1 errors (per-folder TestDataQueues list failures) — distinct
        // ErrorId to preserve the legacy split between "couldn't list queues"
        // and "couldn't get items of a specific queue".
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            WriteError(new ErrorRecord(ex, "GetTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
        }
    }
}
