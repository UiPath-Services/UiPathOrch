using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestDataQueueItem")]
[OutputType(typeof(Entities.TestDataQueueItem))]
public class GetTestDataQueueItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
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
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    var results = drive.GetTestDataQueues(folder).FilterByWildcards(e => e.Name!, wpName);
        //    foreach (var testDataQueue in results)
        //    {
        //        var items = drive.GetTestDataQueueItems(folder, testDataQueue);
        //        WriteObject(items, true);
        //    }
        //}

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var testDataQueues = df.drive.TestDataQueues.Get(df.folder);
                return OrchThreadPool.RunForEach(testDataQueues.FilterByWildcards(e => e?.Name, wpName),
                    queue => queue.GetPSPath(),
                    queue => queue,
                    queue => df.drive.TestDataQueueItems.Get(df.folder, queue));
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult(cancelHandler.Token);

                foreach (var thread in threads!)
                {
                    try
                    {
                        var entities = thread.GetResult(cancelHandler.Token);
                        WriteObject(entities, true);

                        // JSON を展開して出力する
                        // いちおう動くが、コンソール側で Format-List にリダイレクトしないと
                        // きれいな結果が得られない。。
                        //if (entities!.Count == 0) continue;
                        //var list = new List<PSObject>();
                        //foreach (var entity in entities!)
                        //{
                        //    var psobject = CreatePsObjectFromTestDataQueueItem(entity);
                        //    list.Add(psobject);
                        //}
                        //WriteObject(list, true);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetTestDataQueueItemError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
