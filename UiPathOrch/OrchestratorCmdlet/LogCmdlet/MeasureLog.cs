using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

public class LogCount
{
    public string? Path { get; set; }
    public long Count { get; set; }
}

// Counts the robot logs Get-OrchLog would return for the same filter, one row per folder,
// without fetching them. Its own cmdlet rather than a Get-OrchLog switch: a cmdlet name is
// easier to find than a switch (user's call, 2026-10-08).
[Cmdlet(VerbsDiagnostic.Measure, "OrchLog")]
[OutputType(typeof(LogCount))]
public class MeasureLogCmdlet : RobotLogFilterCmdlet
{
    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        using ProgressReporter reporter = new(this, drivesFolders.Count, "Counting logs");
        int index = 0;
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
            reporter.WriteProgress(++index, folder.GetPSPath());

            string query = MakeFilter(drive, folder);
            if (query == "null") continue;

            try
            {
                long count = drive.OrchAPISession.GetRobotLogsTotalCount(folder.Id ?? 0, query);
                WriteObject(new LogCount { Path = folder.GetPSPath(), Count = count });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "MeasureLogError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
