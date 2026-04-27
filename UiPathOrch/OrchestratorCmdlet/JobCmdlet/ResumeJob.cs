using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Resume, "OrchJob", DefaultParameterSetName = "FromCommandLine", SupportsShouldProcess = true)]
[OutputType(typeof(Job))]
public class ResumeJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    [ArgumentCompleter(typeof(KeyCompleter))]
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

    // Tab-completion of -Key offers Suspended jobs in the current/target folder context.
    // Resume only applies to Suspended state.
    private class KeyCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var paramKey = GetSelfExclusionValues(commandAst, "Key", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = new ConcurrentBag<ReadOnlyCollection<Job>?>();
            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    var folderJobs = drive.Jobs.GetCache(folder);
                    if (folderJobs is not null)
                    {
                        results.Add(folderJobs.Values.ToList().AsReadOnly());
                    }
                    else
                    {
                        results.Add(drive.Jobs.Fetch(folder, "&$filter=(State%20eq%20%27Suspended%27)"));
                    }
                }
                catch { }
            });

            foreach (var job in results
                .SelectMany(te => te!)
                .Where(job => !string.IsNullOrEmpty(job.Key))
                .Where(job => wp.IsMatch(job.Key))
                .Where(job => string.Equals(job.State, "Suspended", StringComparison.OrdinalIgnoreCase))
                .ExcludeByClassValues(job => job!.Key, paramKey))
            {
                yield return new CompletionResult(job.Key!, job.Key!, CompletionResultType.ParameterValue, job.FormatTooltip());
            }
        }
    }

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
