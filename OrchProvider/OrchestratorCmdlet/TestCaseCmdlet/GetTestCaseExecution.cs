using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name; // パラメータセットがあるときはどう定義すればいいのだろう。。

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseExecution", DefaultParameterSetName = "ByName")]
[OutputType(typeof(Entities.TestCaseExecution))]
public class GetTestCaseExecutionCmdlet : OrchestratorPSCmdlet
{
    // パラメータセット1: 名前指定
    [Parameter(Position = 0, ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(TestSetExecutionNameCompleter))]
    public string? TestSetExecutionName { get; set; }

    // パラメータセット2: Id 指定（パイプ入力用）
    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ById")]
    [Alias("Id")]
    public Int64 TestSetExecutionId { get; set; }

    // 共通パラメータ
    [Parameter]
    [ArgumentCompleter(typeof(TestCaseExecutionEntryPointCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("EntryPointPath")]
    public string[]? Name { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    public ulong? Skip { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    /// <summary>
    /// キャッシュまたは API から TestSetExecution を名前で検索し、Id を返す。
    /// 見つからない場合は null を返す。
    /// </summary>
    private static Int64? ResolveTestSetExecutionId(OrchDriveInfo drive, Folder folder, string name)
    {
        // まずキャッシュから検索
        if (drive._dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
        {
            var found = cached.Values.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found.Id;
            }
        }

        // キャッシュになければ API で名前検索
        var filter = $"&$filter=(Name%20eq%20%27{Uri.EscapeDataString(name)}%27)";
        var results = drive.GetTestSetExecutions(folder, filter, 0, 1);
        var execution = results.FirstOrDefault();
        return execution?.Id;
    }

    private static string MakeFilter(Int64 testSetExecutionId)
    {
        return $"&$filter=(TestSetExecutionId%20eq%20{testSetExecutionId})";
    }

    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case "ByName":
                ProcessByName();
                break;
            case "ById":
                ProcessById();
                break;
        }
    }

    private void ProcessByName()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (TestSetExecutionName is null && Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since TestSetExecutionName/Skip/First were not specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one of these parameters.");

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicTestCaseExecutions?.TryGetValue(folder.Id!.Value, out var cached) ?? false)
                {
                    WriteObject(cached
                        .FilterByWildcards(e => e?.EntryPointPath, wpName)
                        .OrderBy(e => e.TestSetExecutionId)
                        .ThenBy(e => e.EntryPointPath),
                        true);
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // TestSetExecutionName から Id を解決
                Int64? testSetExecutionId = null;
                if (!string.IsNullOrEmpty(TestSetExecutionName))
                {
                    testSetExecutionId = ResolveTestSetExecutionId(drive, folder, TestSetExecutionName);
                    if (testSetExecutionId is null)
                    {
                        WriteWarning($"TestSetExecution '{TestSetExecutionName}' not found in folder '{folder.GetPSPath()}'.");
                        continue;
                    }
                }

                string? filter = testSetExecutionId is not null ? MakeFilter(testSetExecutionId.Value) : null;
                var entities = drive.GetTestCaseExecutions(folder, filter, skip, first);

                WriteObject(entities
                    .FilterByWildcards(e => e?.EntryPointPath, wpName)
                    .OrderBy(e => e.TestSetExecutionId)
                    .ThenBy(e => e.EntryPointPath),
                    true);
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseExecutionError", ErrorCategory.InvalidOperation, target));
            }
        }
    }

    private void ProcessById()
    {
        // Path がパイプでバインドされていなければカレントロケーションを使用
        var path = Path ?? [SessionState.Path.CurrentLocation.Path];
        var (drive, folder) = SessionState.ResolveToSingleFolder(path[0]);
        var wpName = Name.ConvertToWildcardPatternList();

        try
        {
            string filter = MakeFilter(TestSetExecutionId);
            var entities = drive.GetTestCaseExecutions(folder, filter, 0, ulong.MaxValue);

            WriteObject(entities
                .FilterByWildcards(e => e?.EntryPointPath, wpName)
                .OrderBy(e => e.EntryPointPath),
                true);
        }
        catch (Exception ex)
        {
            string target = folder.GetPSPath();
            WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseExecutionError", ErrorCategory.InvalidOperation, target));
        }
    }
}
