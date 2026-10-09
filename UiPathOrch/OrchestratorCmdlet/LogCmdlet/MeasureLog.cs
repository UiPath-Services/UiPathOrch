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

            try
            {
                // A bad -JobKey is the same for every folder: it ends the command (ArgumentException).
                // A folder whose releases or machines cannot be read fails only that folder.
                string? query = MakeFilter(drive, folder);

                // "null": nothing in the folder can match (the -ProcessName, -Machine or
                // -WindowsIdentity is not there), so the count is 0 -- written, so that a folder
                // with none shows as 0 rather than not at all.
                long count = query == "null" ? 0 : drive.OrchAPISession.GetRobotLogsTotalCount(folder.Id ?? 0, query);
                WriteObject(new LogCount { Path = folder.GetPSPath(), Count = count });
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not PipelineStoppedException)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "MeasureLogError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
