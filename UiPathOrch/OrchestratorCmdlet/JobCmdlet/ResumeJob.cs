using System.Management.Automation;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Resume, "OrchJob", DefaultParameterSetName = "FromCommandLine", SupportsShouldProcess = true)]
[OutputType(typeof(Job))]
public class ResumeJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    public string[]? Key { get; set; }

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
            var drivesFolders = SessionState.EnumFolders(new string[] { Job.Path! });
            foreach (var (drive, folder) in drivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                ResumeOne(drive, folder, Job.Key ?? "");
            }
            return;
        }

        var dfs = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        foreach (var (drive, folder) in dfs)
        {
            foreach (var jobKey in Key!)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                ResumeOne(drive, folder, jobKey);
            }
        }
    }

    private void ResumeOne(OrchDriveInfo drive, Entities.Folder folder, string jobKey)
    {
        string target = $"{folder.GetPSPath()} Job {jobKey}";
        if (!ShouldProcess(target, "Resume Job")) return;

        try
        {
            var resumed = drive.OrchAPISession.ResumeJob(folder.Id ?? 0, jobKey);
            drive.Jobs.ClearCache(folder);
            if (resumed is not null)
            {
                resumed.Path = folder.GetPSPath();
                WriteObject(resumed);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "ResumeJobError", ErrorCategory.InvalidOperation, jobKey));
        }
    }
}
