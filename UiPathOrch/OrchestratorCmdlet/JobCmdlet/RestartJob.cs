using System.Management.Automation;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Restart, "OrchJob", DefaultParameterSetName = "FromCommandLine", SupportsShouldProcess = true)]
[OutputType(typeof(Job))]
public class RestartJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    public Int64[]? Id { get; set; }

    [Parameter(DontShow = true, ValueFromPipeline = true, ParameterSetName = "FromPipeline")]
    public Job? Job { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        if (Job is not null)
        {
            // Pipeline input from Get-OrchJob
            var drivesFolders = SessionState.EnumFolders(new string[] { Job.Path! });
            foreach (var (drive, folder) in drivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                RestartOne(drive, folder, Job.Id ?? 0);
            }
            return;
        }

        var dfs = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        foreach (var (drive, folder) in dfs)
        {
            foreach (var jobId in Id!)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                RestartOne(drive, folder, jobId);
            }
        }
    }

    private void RestartOne(OrchDriveInfo drive, Entities.Folder folder, Int64 jobId)
    {
        string target = $"{folder.GetPSPath()} Job {jobId}";
        if (!ShouldProcess(target, "Restart Job")) return;

        try
        {
            var restarted = drive.OrchAPISession.RestartJob(folder.Id ?? 0, jobId);
            drive.Jobs.ClearCache(folder);
            if (restarted is not null)
            {
                restarted.Path = folder.GetPSPath();
                WriteObject(restarted);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "RestartJobError", ErrorCategory.InvalidOperation, jobId));
        }
    }
}
