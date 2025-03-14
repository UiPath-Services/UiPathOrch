using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Last;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLicenseStats")]
[OutputType(typeof(Entities.LicenseStatsModel))]
public class GetLicenseStatsCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Day_Week_Month_3Month_6Month_Year_3Year>))]
    public string? Last { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        int days = 7;
        if (Last is not null)
        {
            var last = Last.ToLower() switch
            {
                "day" => DateTime.Today.AddDays(-1),
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                "3months" => DateTime.Today.AddMonths(-3),
                "6months" => DateTime.Today.AddMonths(-6),
                "year" => DateTime.Today.AddYears(-1),
                "3years" => DateTime.Today.AddYears(-3),
                _ => throw new ArgumentException("Invalid Last parameter. Valid values are 'Day', 'Week', 'Month', '3Months', '6Months', 'Year', '3Years'.")
            };
            days = ((int)(DateTime.Today - last).TotalDays);
        }

        // ToList() は遅延評価を抑止し、各スレッド内で問い合わせを行えるようにするために必要
        // まだキャッシュ作ってないから。。
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive =>
            {
                var (tenantId, _) = drive.GetTenantId();
                return drive.OrchAPISession.GetLicenseStats(tenantId ?? 0, days).ToList();
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var drive = result.Source;

                foreach (var stat in entities
                    .OrderBy(s => s.robotType)
                    .ThenBy(s => s.timestamp))
                {
                    stat.Path = drive!.NameColonSeparator;
                    WriteObject(stat);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetJobStatsError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
