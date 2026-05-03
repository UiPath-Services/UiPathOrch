using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

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

    private static readonly string[] alreadyStoppedStates = ["Terminating", "Faulted", "Successful", "Stopped"];

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

            // Exclude Ids that have already been selected via parameters
            // TODO: Should we support wildcards for input here?
            var paramId = GetSelfExclusionValues(commandAst, "Id", wordToComplete).Select(id => Int64.Parse(id));

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = new ConcurrentBag<List<Job>>();
            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    results.Add(drive.StoppableJobs.Get(folder));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"StoppableJobs prefetch failed for '{folder.GetPSPath()}': {ex.Message}");
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
        if (Job is not null)
        {
            // Pipe input from Get-OrchJob
            // Skip jobs that are already marked as stopped
            if (alreadyStoppedStates.Contains(Job.State))
                return;

            // Resolve Path, then add to the parameters list
            var drivesFolders = SessionState.EnumFolders(new string[] { Job.Path! });
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
            // Input from command line
            var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

            foreach (var (drive, folder) in drivesFolders)
            {
                // Get the Job cache for this folder
                // Read the cache directly to avoid calling drive.Jobs.Fetch() for each parameter
                // (unnecessary calls to drive.Jobs.Fetch() slow down processing)
                var folderJobs = drive.Jobs.GetCache(folder);
                if (folderJobs is not null)
                {
                    foreach (var jobId in Id!)
                    {
                        // Skip jobs that are marked as stopped in the cache
                        if (folderJobs.TryGetValue(jobId, out var job))
                        {
                            if (alreadyStoppedStates.Contains(job.State))
                                continue;
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
    }

    //private class StopJobOutput(string? path, long? id)
    //{
    //    public string? Path { get; set; } = path;
    //    public long? Id { get; set; } = id;
    //}

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
                    //WriteObject(jobsToStop.Select(id => new StopJobOutput(folder?.GetPSPath(), id)), true);
                    drive.Jobs.ClearCache(folder!);
                    drive.ClearJobCompleterCaches(folder!);
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
