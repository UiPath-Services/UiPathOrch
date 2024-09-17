using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using Job = UiPath.PowerShell.Entities.Job;

using Positional = UiPath.PowerShell.Positional.JobId;

namespace UiPath.PowerShell.Commands
{
    // Command for Open-OrchJobVideo
    // URL の前方が不一致だからか、ブラウザが Bearer Token ヘッダを付与してくれないので動かない。。
    // 非公開にしておく
    // [Cmdlet(VerbsCommon.Open, "OrchJobVideo")]
    class OpenJobVideoCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(JobIdCompleter))]
        public Int64[]? JobId { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        private class JobIdCompleter : OrchArgumentCompleter
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
                var paramId = GetParameterValues(commandAst, "JobId", Positional.JobId.Parameters, wordToComplete).Select(i => long.Parse(i));

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

                    foreach (var job in dicJobs.Values
                        .Where(j => j.HasVideoRecorded.GetValueOrDefault())
                        .ExcludeByStructValues<Job, Int64>(j => j.Id ?? 0, paramId))
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

            // あらかじめ、非同期で対象のプロセスを取得しておく
            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;

                foreach (var id in JobId!)
                {
                    drive.GetJob(folder, id);
                }
            });

            foreach (var (drive, folder) in drivesFolders)
            {
                var jobs = drive._dicJobs;
                if (jobs == null || !jobs.TryGetValue(folder.Id ?? 0, out var folderJobs))
                {
                    continue;
                }

                foreach (var id in JobId!)
                {
                    if (folderJobs.TryGetValue(id, out var job))
                    {
                        string endpoint = $"{drive.OrchAPISession._base_url}/orchestrator_/api/VideoRecording/jobs/{job.Key}/read";
                        Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
                    }
                }
            }

        }
    }
}
