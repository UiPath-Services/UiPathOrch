using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchJobMedia", SupportsShouldProcess = true)]
public class RemoveJobMediaCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(JobIdCompleter))]
    public Int64[]? JobId { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

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

            // Exclude JobIds already selected by the parameter from the candidates
            var paramJobId = GetSelfExclusionValues(commandAst, "JobId", wordToComplete).Select(s => long.Parse(s));
            //var wpJobId = paramJobId.Select(un => new WildcardPattern(un, WildcardOptions.IgnoreCase)).ToList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // (folderId, media)
            List<(Int64 folderId, ExecutionMedia media)> results = new();
            foreach (var (drive, folder) in drivesFolders)
            {
                // Use the cache if available
                var cached = drive.JobsHavingExecutionMedia.GetCache(folder);
                if (cached is not null)
                {
                    foreach (var media in cached.Values)
                    {
                        results.Add((folder.Id ?? 0, media));
                    }
                }
                else // If not cached, fetch from the server
                {
                    foreach (var media in drive.GetExecutionMedia(folder))
                    {
                        results.Add((folder.Id ?? 0, media));
                    }
                }
            }

            foreach (var folderMedia in results
                .Where(fm => wp.IsMatch(fm.media.JobId.ToString()))
                .ExcludeByStructValues<(Int64, ExecutionMedia), Int64>(m => m.Item2.JobId ?? 0, paramJobId))
            {
                string tiphelp = "FileName: " + JobMediaCommon.MediaFileName(folderMedia.Item1, folderMedia.Item2);
                yield return new CompletionResult(folderMedia.Item2.JobId.ToString(), folderMedia.Item2.JobId.ToString(), CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        // Since wildcards are not supported,
        // there is no need to asynchronously pre-fetch the target ExecutionMedia

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                string path = folder.GetPSPath();
                foreach (var jobId in JobId!.WithCancellation(cancelHandler.Token))
                {
                    string target = path + System.IO.Path.DirectorySeparatorChar + jobId;
                    if (ShouldProcess(target, "Remove JobMedia"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveExecutionMedia(folder.Id ?? 0, jobId);
                            drive.JobsHavingExecutionMedia.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveJobMediaError", ErrorCategory.InvalidOperation, target));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveJobMediaError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
