using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAuditLog")]
[OutputType(typeof(Entities.PmAuditLog))]
public class GetPmAuditLogCommand : OrchestratorPSCmdlet
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

        var drives = SessionState.EnumPmDrives(Path);

        // If no parameters are specified, return the cache contents
        bool bOutCache = (Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");
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

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.GetPmAuditLog(filter, skip, first);
                if (entities is null) continue;

                // Do not sort here
                WriteObject(entities, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmAuditLogError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
