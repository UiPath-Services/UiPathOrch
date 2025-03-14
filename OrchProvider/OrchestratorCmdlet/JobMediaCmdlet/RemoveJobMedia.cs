using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.JobId;

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

            // パラメータで選択済みの JobId は、候補から除外する
            var paramJobId = GetParameterValues(commandAst, "JobId", TPositional.Parameters, wordToComplete).Select(s => long.Parse(s));
            //var wpJobId = paramJobId.Select(un => new WildcardPattern(un, WildcardOptions.IgnoreCase)).ToList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // (folderId, media)
            List<(Int64 folderId, ExecutionMedia media)> results = new();
            foreach (var (drive, folder) in drivesFolders)
            {
                // キャッシュ済みならキャッシュを使う
                if (drive._dicJobsHavingExecutionMedia is not null && drive._dicJobsHavingExecutionMedia.TryGetValue(folder.Id ?? 0, out var jobsHavingMedia))
                {
                    foreach (var media in jobsHavingMedia)
                    {
                        results.Add((folder.Id ?? 0, media));
                    }
                }
                else // 未キャッシュなら取得する
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
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

        // ワイルドカードをサポートしないため、
        // あらかじめ非同期で対象の ExecutionMedia を取得しておくことは不要

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                string path = folder.GetPSPath();
                foreach (var jobId in JobId!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = path + System.IO.Path.DirectorySeparatorChar + jobId;
                    if (ShouldProcess(target, "Remove JobMedia"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveExecutionMedia(folder.Id ?? 0, jobId);
                            drive._dicJobsHavingExecutionMedia?.TryRemove(folder.Id ?? 0, out var _);
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
