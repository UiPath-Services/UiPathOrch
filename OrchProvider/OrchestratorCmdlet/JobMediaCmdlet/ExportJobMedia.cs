using OrchProvider.JobMediaCmdlet;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.JobId_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Export, "OrchJobMedia", SupportsShouldProcess = true)]
    public class SaveJobMediaCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(JobIdCompleter))]
        public Int64[]? JobId { get; set; }

        [Parameter(Position = 1)]
        [SupportsWildcards]
        public string? Destination { get; set; }

        [Parameter]
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
                var paramJobId = GetParameterValues(commandAst, "JobId", TPositional.Parameters, wordToComplete).Select(id => long.Parse(id));

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // (folderId, media)
                List<(Int64 folderId, ExecutionMedia media)> results = [];
                foreach (var (drive, folder) in drivesFolders)
                {
                    // キャッシュ済みならキャッシュを使う
                    if (drive._dicJobsHavingExecutionMedia != null && drive._dicJobsHavingExecutionMedia.TryGetValue(folder.Id ?? 0, out var jobsHavingMedia))
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

            if (Destination == null)
            {
                Destination = SessionState.Path.CurrentFileSystemLocation.Path;
            }
            if (!Directory.Exists(Destination))
            {
                throw new DirectoryNotFoundException($"Directory {Destination} doesn't exist.");
            }

            #region あらかじめ、非同期で対象の ExecutionMedia を取得しておく
            Parallel.ForEach(drivesFolders, (driveFolder, state, index) =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    drive.GetExecutionMedia(folder);
                }
                catch { }
            });
            #endregion

            #region 合計でいくつのファイルをダウンロードするのか数える
            int totalFileNum = 0;
            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicJobsHavingExecutionMedia == null)
                {
                    continue;
                }

                if (!drive._dicJobsHavingExecutionMedia.TryGetValue(folder.Id ?? 0, out var allMediasInFolder))
                {
                    continue;
                }

                if (allMediasInFolder == null)
                {
                    continue;
                }

                if (JobId == null)
                {
                    totalFileNum += allMediasInFolder.Count();
                }
                else
                {
                    totalFileNum += allMediasInFolder
                        .Where(jobId => JobId.Contains(jobId.JobId ?? 0)).Count();
                }
            }
            #endregion

            string msg = "Saving Media";
            using var reporter = new ProgressReporter(this, 1, totalFileNum, msg, msg);

            int index = 0;
            foreach (var (drive, folder) in drivesFolders)
            {
                string path = folder.GetPSPath();

                if (drive._dicJobsHavingExecutionMedia == null)
                {
                    continue;
                }

                if (!drive._dicJobsHavingExecutionMedia.TryGetValue(folder.Id ?? 0, out var allMediasInFolder))
                {
                    continue;
                }

                if (allMediasInFolder == null)
                {
                    continue;
                }

                List<ExecutionMedia> mediasToBeSaved;
                if (JobId == null)
                {
                    mediasToBeSaved = allMediasInFolder;
                }
                else
                {
                    mediasToBeSaved = allMediasInFolder
                        .Where(jobId => JobId.Contains(jobId.JobId ?? 0))
                        .ToList();
                }

                foreach (var media in mediasToBeSaved!)
                {
                    string target = path + System.IO.Path.DirectorySeparatorChar + media.JobId;

                    if (ShouldProcess(target, "Export JobMedia"))
                    {
                        reporter.WriteProgress(++index, $"{index:D}/{totalFileNum}");

                        try
                        {
                            var (fileName, fileContent) = drive.OrchAPISession.DownloadMediaByJobId(folder.Id ?? 0, media.JobId ?? 0).GetAwaiter().GetResult();
                            string filePath = System.IO.Path.Combine(Destination, JobMediaCommon.MediaFileName(folder.Id ?? 0, media));
                            File.WriteAllBytes(filePath, fileContent);
                            //WriteObject(filePath + " saved.");
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "ExportJobMediaError", ErrorCategory.InvalidOperation, target));
                        }
                    }
                }
            }
        }
    }
}
