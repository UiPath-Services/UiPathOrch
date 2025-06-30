using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Empty;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAuditLog")]
[OutputType(typeof(Entities.PmAuditLog))]
public class GetPmAuditLogCommand : OrchestratorPSCmdlet
{
    // 試してみたけど、この API はフィルターを指定しても機能しないようだ。。
    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    //public string? Last { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(TimeAfterCompleter))]
    //public DateTime? TimeStampAfter { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(TimeBeforeCompleter))]
    //public DateTime? TimeStampBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter OrderAscending { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private string? MakeFilter()
    {
        //List<string> filter = [];

        // 残念、動かない
        //#region TimeStampAfter
        //if (TimeStampAfter is not null)
        //{
        //    filter.Add($"(CreatedOn ge {TimeStampAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        //}
        //#endregion

        if (OrderAscending)
        {
            return "&sortBy=CreatedOn&sortOrder=asc";
        }
        else
        {
            // ここは return null としても適切に動作するようだが
            // 別の version の Orchestrator でも動作するのか良く分からないので
            // ソート条件を明示しておく
            return "&sortBy=CreatedOn&sortOrder=desc";
        }

        //string ret = filter.CreateAndFilter(f => f);
        //ret = "&$filter=(" + ret + ")";
        //return ret;
    }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drives = SessionState.EnumPmDrives(Path);

        // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");
            foreach (var drive in drives)
            {
                if (OrderAscending)
                {
                    WriteObject(drive._dicPmAuditLogs?.OrderBy(l => l.createdOn), true);
                }
                else
                {
                    WriteObject(drive._dicPmAuditLogs?.OrderByDescending(l => l.createdOn), true);
                }
            }
            return;
        }

        string filter = MakeFilter();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.GetPmAuditLog(filter, skip, first));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                // ここでソートしてはいけない
                WriteObject(entities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmAuditLogError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
