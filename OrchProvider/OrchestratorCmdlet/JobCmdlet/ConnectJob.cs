using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Reflection.PortableExecutable;
using Job = UiPath.PowerShell.Entities.Job;
using System.Diagnostics;
using System.Net;

namespace UiPath.PowerShell.Commands;

#region 実装方法を探し中..

#if false

// Command for Connect-OrchMachine
[Cmdlet(VerbsCommunications.Connect, "OrchJob")]
public class ConnectMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(JobIdCompleter))]
    public Int64? JobId { get; set; }

    private class JobIdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (OrchDriveInfo is null)
                yield break;

            Folder folder = OrchDriveInfo.GetCurrentFolder();
            if (folder is null)
                yield break;

            if (OrchDriveInfo.JobsCache.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job>? jobCache))
            {
                string? state = fakeBoundParameters["State"]?.ToString();

                foreach (var job in jobCache)
                {
                    if (state is not null && job.Value.State != state)
                        continue;

                    string jobId = job.Key.ToString();
                    string tiphelp = $"{jobId} C{job.Value?.CreationTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
                    if (job.Value?.StartTime is not null)
                        tiphelp += $"  S{job.Value?.StartTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                    else
                        tiphelp += $"                      ";
                    if (job.Value?.EndTime is not null)
                        tiphelp += $"  E{job.Value?.EndTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                    else
                        tiphelp += $"                      ";
                    tiphelp += $" {job.Value?.State,11} {job.Value!.ReleaseName}";

                    yield return new CompletionResult(jobId, jobId, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        // job から key を取得
        EnsureLocationInOrchestratorFolder();
        Folder folder = OrchDriveInfo.GetCurrentFolder();
        var job = OrchAPI.GetJob(folder!.Id ?? 0, JobId ?? 0);

        string uri = OrchAPI.StartRemoteControl(folder!.Id ?? 0, job.Key!);
        WriteWarning(uri);
        //Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}

#endif

#endregion
