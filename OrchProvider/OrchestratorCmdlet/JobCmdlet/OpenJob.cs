using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Job = UiPath.PowerShell.Entities.Job;
using JobState = UiPath.PowerShell.Entities.JobState;

using Positional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Open, "OrchJob")] //, SupportsPaging = true)]
    public class OpenJobCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? Id { get; set; }

        //[Parameter]
        //public SwitchParameter Expanded { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        private class IdCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Id は、候補から除外する
                var paramId = GetParameterValues(commandAst, "Id", Positional.Id.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var (drive, folder) in drivesFolders)
                {
                    if (drive._dicJobs == null)
                    {
                        continue;
                    }

                    if (!drive._dicJobs.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job>? dicJobs))
                    {
                        continue;
                    }

                    foreach (var job in dicJobs.Values.ExcludeByClassValues(j => (j?.Id ?? 0).ToString(), paramId))
                    {
                        if (!wp.IsMatch((job.Id ?? 0).ToString()))
                            continue;

                        string tiphelp = $"{job.Id} C{job.CreationTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
                        if (job.StartTime != null)
                            tiphelp += $"  S{job.StartTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                        else
                            tiphelp += $"                      ";
                        if (job.EndTime != null)
                            tiphelp += $"  E{job.EndTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                        else
                            tiphelp += $"                      ";
                        tiphelp += $" {job.State,11} {job.ReleaseName}";

                        yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path);

            foreach (var (drive, folder) in drivesFolders)
            {
                Dictionary<Int64, Job> dicJobs = null;
                if (drive._dicJobs != null)
                {
                    drive._dicJobs.TryGetValue(folder.Id ?? 0, out dicJobs);
                }

                foreach (var id in Id!)
                {
                    Job job = null;
                    if (dicJobs != null)
                    {
                        dicJobs.TryGetValue(id, out job);
                    }
                    if (job == null || string.IsNullOrEmpty(job.Key))
                    {
                        try
                        {
                            job = drive.GetJob(folder, id);
                            if (job == null) { continue; }
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
}
