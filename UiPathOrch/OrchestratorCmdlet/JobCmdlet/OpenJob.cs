using System.Diagnostics;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Open, "OrchJob")] //, SupportsPaging = true)]
public class OpenJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(JobIdCompleter))]
    public Int64[]? Id { get; set; }

    //[Parameter]
    //public SwitchParameter Expanded { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);

        foreach (var (drive, folder) in drivesFolders)
        {
            var dicJobs = drive.Jobs.GetCache(folder);

            foreach (var id in Id!)
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
                    }
                }

                string endPoint = $"{drive.OrchAPISession._base_url}/orchestrator_/jobs(sidepanel:sidepanel/jobs/{job!.Key}/details)?fid={folder!.Id ?? 0}";
                //if (Expanded.IsPresent)
                //{
                //    endPoint += "&isExpanded=true";
                //}
                Process.Start(new ProcessStartInfo(endPoint) { UseShellExecute = true });
            }
        }
    }
}
