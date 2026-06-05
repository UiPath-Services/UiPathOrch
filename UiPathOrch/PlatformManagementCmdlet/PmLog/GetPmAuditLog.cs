using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAuditLog")]
[OutputType(typeof(Entities.PmAuditLog))]
public class GetPmAuditLogCmdlet : OrchestratorPSCmdlet
{
    // Tried it, but this API doesn't seem to work even when specifying filters.
    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    //[ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
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
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private string? MakeFilter()
    {
        //List<string> filter = [];

        // Unfortunately, this doesn't work
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
            // Returning null here seems to work properly, but
            // it's unclear whether it works on other Orchestrator versions,
            // so explicitly specify the sort condition
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

        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        // If no parameters are specified, return the cache contents
        bool bOutCache = (Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");
            foreach (var drive in drives)
            {
                var cached = drive.PmAuditLogs.GetCache()?.Values;
                if (cached is null) continue;
                if (OrderAscending)
                {
                    WriteObject(cached.OrderBy(l => l.createdOn), true);
                }
                else
                {
                    WriteObject(cached.OrderByDescending(l => l.createdOn), true);
                }
            }
            return;
        }

        string filter = MakeFilter();

        // Parallelize the per-drive fetch. PmAuditLogs is a per-tenant cache
        // (one instance per drive), so concurrent drives touch independent
        // caches. WriteObject stays on the pipeline thread. The cache-dump
        // branch above is pure in-memory and stays sequential.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmAuditLogs.Fetch(filter, skip, first));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                // Do not sort here
                WriteObject(entities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmAuditLogError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
