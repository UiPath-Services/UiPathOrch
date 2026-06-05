using System.Diagnostics;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Open, "OrchJob")]
public class OpenJobCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(JobIdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
            var dicJobs = drive.Jobs.GetCache(folder);

            foreach (var id in Id!.WithCancellation(cancelHandler.Token))
            {
                Job? job = null;
                dicJobs?.TryGetValue(id, out job);
                if (job is null || string.IsNullOrEmpty(job.Key))
                {
                    try
                    {
                        job = drive.GetJob(folder, id);
                        if (job is null) { continue; }
                    }
                    catch (Exception ex)
                    {
                        string target = folder.GetPSPath();
                        WriteError(new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target));
                        continue;
                    }
                }

                string endPoint = $"{drive.OrchAPISession._base_url}/orchestrator_/jobs(sidepanel:sidepanel/jobs/{job!.Key}/details)?fid={folder!.Id ?? 0}";
                Process.Start(new ProcessStartInfo(endPoint) { UseShellExecute = true });
            }
        }
    }
}
