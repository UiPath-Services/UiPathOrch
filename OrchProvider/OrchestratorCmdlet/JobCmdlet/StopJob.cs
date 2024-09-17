using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Entities;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands
{
    class StopJobCommandParameter
    {
        public Int64 Id { set; get; }
        public OrchDriveInfo? Drive { set; get; }
        public Folder? Folder { set; get; }
    }

    [Cmdlet(VerbsLifecycle.Stop, "OrchJob", DefaultParameterSetName = "FromCommandLine", SupportsShouldProcess = true)]
    public class StopJobCommand : OrchestratorPSCmdlet
    {
        private List<StopJobCommandParameter> parameters = new();

        //private static readonly string[] stoppableStates = ["Pending", "Running", "Suspended", "Resumed"];
        //private static readonly string[] killableStates = ["Pending", "Running", "Stopping", "Suspended", "Resumed"];
        private static readonly string[] alreadyStoppedStates = ["Terminating", "Faulted", "Successful", "Stopped"];

        private static readonly string[] positionalParams = ["Id"];

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? Id { get; set; }

        [Parameter(DontShow = true, ValueFromPipeline = true)]
        public Job? Job { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

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
                var paramId = GetParameterValues(commandAst, "Id", positionalParams, wordToComplete).Select(id => Int64.Parse(id));

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = new ConcurrentBag<ReadOnlyCollection<Job>?>();
                Parallel.ForEach(drivesFolders, driveFolder =>
                {
                    var (drive, folder) = driveFolder;

                    try
                    {
                        // キャッシュが残っていればそれを使う
                        if (drive._dicJobs != null &&
                            drive._dicJobs.TryGetValue(folder.Id ?? 0, out var folderJobs))
                        {
                            results.Add(folderJobs!.Values.ToList().AsReadOnly());
                        }
                        else // キャッシュがなければ取得する
                        {
                            results.Add(drive.GetJobs(folder, "&$filter=((ProcessType%20eq%20%27Process%27)%20and%20((State%20eq%20%27Pending%27)%20or%20(State%20eq%20%27Running%27)%20or%20(State%20eq%20%27Stopping%27)%20or%20(State%20eq%20%27Suspended%27)%20or%20(State%20eq%20%27Resumed%27)))"));
                        }
                    }
                    catch { }
                });

                foreach (var job in results
                    .SelectMany(te => te!)
                    .Where(job => wp.IsMatch((job.Id ?? 0).ToString()))
                    .Where(job => !alreadyStoppedStates.Contains(job.State))
                    .ExcludeByStructValues(job => (job.Id ?? 0), paramId))
                {
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

        protected override void ProcessRecord()
        {
            if (Job != null)
            {
                // Get-OrchJob からパイプ入力
                // 停止済みとマークされているジョブは処理しない
                if (alreadyStoppedStates.Contains(Job.State))
                    return;

                // Path を展開した上で、parameters に追加
                var drivesFolders = OrchDriveInfo.EnumFolders(new string[] { Job.Path! });
                foreach (var (drive, folder) in drivesFolders)
                {
                    var parameter = new StopJobCommandParameter()
                    {
                        Drive = drive,
                        Folder = folder,
                        Id = Job.Id ?? 0
                    };
                    parameters.Add(parameter);
                }
            }
            else
            {
                // コマンドラインから入力
                var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

                foreach (var (drive, folder) in drivesFolders)
                {
                    // このフォルダーの Job キャッシュを取得
                    // パラメータごとに drive.GetJobs() を呼ばないように、キャッシュを直接読み取る
                    // (無駄に drive.GetJobs() を呼び出すと、処理が遅くなってしまう)
                    Dictionary<Int64, Job> folderJobs = null;
                    if (drive._dicJobs != null)
                    {
                        drive._dicJobs!.TryGetValue(folder.Id ?? 0, out folderJobs);
                    }

                    foreach (var jobId in Id!)
                    {
                        // キャッシュに停止済みとマークされているジョブは処理しない
                        if (folderJobs != null)
                        {
                            if (folderJobs.TryGetValue(jobId, out var job))
                            {
                                if (alreadyStoppedStates.Contains(job.State))
                                    continue;
                            }
                        }
                        var parameter = new StopJobCommandParameter()
                        {
                            Drive = drive,
                            Folder = folder,
                            Id = jobId
                        };
                        parameters.Add(parameter);
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            string action = Force ? "Kill Job " : "Stop Job ";

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var group in parameters.GroupBy(p => p.Folder))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                OrchDriveInfo drive = group.First().Drive;
                Folder folder = group.Key;
                string targetFolder = group.Key!.GetPSPath();

                IEnumerable<Int64> jobsToStop = group.Select(p => p.Id);
                string strJobsToStop = string.Join(",", jobsToStop.Select(id => id.ToString()));

                if (ShouldProcess(targetFolder, action + strJobsToStop))
                {
                    try
                    {
                        drive!.OrchAPISession.StopJobs(group.Key!.Id ?? 0, jobsToStop, Force);
                        drive._dicJobs?.TryRemove(folder!.Id ?? 0, out var _);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, ex), "StopJobError", ErrorCategory.InvalidOperation, group.Key);
                        WriteError(errorRecord);
                    }
                }
            }
        }
    }
}
