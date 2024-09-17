using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Job = UiPath.PowerShell.Entities.Job;

using Positional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    // Command for Remove-OrchJobVideo
    // これ多分動かないので非公開にしておく。
    //[Cmdlet(VerbsCommon.Remove, "OrchJobVideo")]
    class RemoveJobVideoCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? Id { get; set; }

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
                var paramId = GetParameterValues(commandAst, "Id", Positional.Id.Parameters, wordToComplete).Select(i => long.Parse(i));

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

            //string endpoint = "/orchestrator_/api/VideoRecording/jobs/18eafed4-9980-4ddc-afac-e5fe030db830";
        }
    }
}
