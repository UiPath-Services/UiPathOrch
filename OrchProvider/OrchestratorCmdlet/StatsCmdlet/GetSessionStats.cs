using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Last;

namespace UiPath.PowerShell.Commands
{
    // not found が返ってしまう？
    //[Cmdlet(VerbsCommon.Get, "OrchSessionStats")]
    [OutputType(typeof(Entities.CountStats))]
    class GetSessionStatsCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Day_Week_Month_3Month_6Month_Year_3Year>))]
        public string? Last { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                try
                {
                    var stats = drive.OrchAPISession.GetSessionStats();
                    foreach (var stat in stats)
                    {
                        stat.Path = drive.NameColonSeparator;
                        WriteObject(stat);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetSessionStatsError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
