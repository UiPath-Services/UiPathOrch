using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;


namespace UiPath.PowerShell.Commands;

// All filter-building parameters have been removed; changed to fetch and cache everything per folder.
[Cmdlet(VerbsCommon.Get, "OrchMachineSession")]
[OutputType(typeof(Entities.MachineSessionRuntime))]
public class GetMachineSessionCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    //[ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    //public string? Last { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeAfterCompleter))]
    //public DateTime? ReportingTimeAfter { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeBeforeCompleter))]
    //public DateTime? ReportingTimeBefore { get; set; }

    //static readonly string[] StatusList = [
    //    "Available", "Busy", "Disconnected", "Unknown"
    //];

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<MachineSessionStatusItems>))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    //[Parameter]
    //public ulong? Skip { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    //public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    //private string? MakeFilter()
    //{
    //    var filter = new List<string>();

    //    #region Last
    //    if (Last is not null)
    //    {
    //        var last = Last.ToLower() switch
    //        {
    //            "hour" => DateTime.UtcNow.AddHours(-1),
    //            "day" => DateTime.UtcNow.AddDays(-1),
    //            "week" => DateTime.UtcNow.AddDays(-7),
    //            "month" => DateTime.UtcNow.AddMonths(-1),
    //            "3months" => DateTime.UtcNow.AddMonths(-3),
    //            "6months" => DateTime.UtcNow.AddMonths(-6),
    //            "year" => DateTime.UtcNow.AddYears(-1),
    //            "3years" => DateTime.UtcNow.AddYears(-3),
    //            _ => throw new ArgumentException("Invalid Last parameter. Valid values are 'Hour', 'Day', 'Week', 'Month', '3Months', '6Months', 'Year', '3Years'.")
    //        };
    //        filter.Add($"(ReportingTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
    //    }
    //    #endregion

    //    #region ReportingTimeAfter
    //    if (ReportingTimeAfter is not null)
    //    {
    //        filter.Add($"(ReportingTime%20ge%20{ReportingTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
    //    }
    //    #endregion

    //    #region ReportingTimeBefore
    //    if (ReportingTimeBefore is not null)
    //    {
    //        filter.Add($"(ReportingTime%20lt%20{ReportingTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
    //    }
    //    #endregion

    //    #region Status
    //    if (Status is not null && Status.Length > 0)
    //    {
    //        var status = new List<string>();
    //        var wpStatus = Status.Select(st => new WildcardPattern(st, WildcardOptions.IgnoreCase)).ToList();
    //        foreach (var s in StatusList.FilterByWildcards(st => st, wpStatus))
    //        {
    //            status.Add($"(Status%20eq%20%27{s}%27)");
    //        }
    //        if (status.Count != 0)
    //            filter.Add("(" + string.Join("%20or%20", status) + ")");
    //    }
    //    #endregion

    //    if (filter.Count > 0)
    //    {
    //        string ret = string.Join("%20and%20", filter);
    //        ret = "&$filter=(" + ret + ")&$orderby=ReportingTime%20asc";
    //        return ret;
    //    }
    //    return "&$orderby=ReportingTime%20asc";
    //}

    protected override void ProcessRecord()
    {
        //ulong skip = Skip ?? 0;
        //ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpStatus = Status.ConvertToWildcardPatternList();

        //string filter = MakeFilter();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.MachineSessionRuntimesByFolder.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var (drive, folder) = result.Source;

                WriteObject(entities
                    .FilterByWildcards(s => s?.Status, wpStatus), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetMachineSessionPerFolderError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    try
        //    {
        //        var sessions = drive.OrchAPISession.GetMachineSessionRuntimesByFolderId(folder.Id ?? 0);
        //        foreach (var session in sessions)
        //        {
        //            session.Path = folder.GetPSPath();
        //            WriteObject(session);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "UpdateUserError", ErrorCategory.InvalidOperation, folder));
        //    }
        //}



    }
}
