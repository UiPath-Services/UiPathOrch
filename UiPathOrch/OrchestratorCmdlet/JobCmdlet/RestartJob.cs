using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Restart, "OrchJob", DefaultParameterSetName = "FromCommandLine", SupportsShouldProcess = true)]
[OutputType(typeof(Job))]
public class RestartJobCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    [ArgumentCompleter(typeof(IdCompleter))]
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

    // Tab-completion of -Id offers Faulted jobs in the current/target folder context.
    // Restart only succeeds against Faulted jobs, so showing other states would just
    // suggest invalid choices.
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
            var paramId = GetSelfExclusionValues(commandAst, "Id", wordToComplete).Select(id => Int64.Parse(id));
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = new ConcurrentBag<List<Job>>();
            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    results.Add(drive.FaultedJobs.Get(folder));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FaultedJobs prefetch failed for '{folder.GetPSPath()}': {ex.Message}");
                }
            });

            foreach (var job in results
                .SelectMany(te => te)
                .Where(job => wp.IsMatch(job.Id.ToString()))
                .ExcludeByStructValues(job => job!.Id.GetValueOrDefault(), paramId))
            {
                yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, job.FormatTooltip());
            }
        }
    }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        if (Job is not null)
        {
            // Pipeline input from Get-OrchJob
            var drivesFolders = SessionState.EnumFolders(new string[] { Job.Path! });
            foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
            {
                RestartOne(drive, folder, Job.Id ?? 0);
            }
            return;
        }

        var dfs = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        foreach (var (drive, folder) in dfs)
        {
            foreach (var jobId in Id!.WithCancellation(cancelHandler.Token))
            {
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
            drive.ClearJobCompleterCaches(folder);
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
