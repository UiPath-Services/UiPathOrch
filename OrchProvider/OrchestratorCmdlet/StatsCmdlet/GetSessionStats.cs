using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.Emit;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Last;
using DaysItems = UiPath.PowerShell.Positional.Day_Week_Month_3Month_6Month_Year_3Year;

namespace UiPath.PowerShell.Commands
{
    // not found が返ってしまう？
    //[Cmdlet(VerbsCommon.Get, "OrchSessionStats")]
    [OutputType(typeof(Entities.CountStats))]
    class GetSessionStatsCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<DaysItems>))]
        public string? Last { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Last>))]
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
