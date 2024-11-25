using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Export, "OrchLibrary", SupportsShouldProcess = true)]
    public class ExportLibraryCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(IdCompleter))]
        public string[]? Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(VersionCompleter))]
        public string[]? Version { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        public string? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Id_Version_Destination>))]
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
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Id は、候補から除外する
                var wpId = CreateWPListFromParameter(commandAst, "Id", TPositional.Parameters, wordToComplete);

                // パラメータで選択された Version のみ対象とする
                var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", TPositional.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetLibraries());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var library in entities!
                        .Where(l => wp.IsMatch(l.Id))
                        .ExcludeByWildcards(l => l?.Id, wpId)
                        .FilterByWildcards(l => l?.Version, wpVersion))
                    {
                        string tiphelp = TipHelp(library);
                        yield return new CompletionResult(PathTools.EscapePSText(library.Id), library.Id, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private class VersionCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択された Id のみ対象とする
                var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

                // パラメータで選択済みの Version は、候補から除外する
                var wpVersion = CreateWPListFromParameter(commandAst, "Version", TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive =>
                {
                    var libraries = drive.GetLibraries().FilterByWildcards(l => l?.Id, wpId);
                    return ParallelResults.ForEach(libraries, library =>
                        drive.GetLibraryVersions(library.Id!));
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var library in entities!)
                    {
                        if (!library.TryGetValue(out var versions)) continue;

                        foreach (var version in versions!
                            .Where(v => wp.IsMatch(v.Version))
                            .ExcludeByWildcards(v => v?.Version, wpVersion))
                            //.OrderBy(v => v.Version!, VersionComparer.Instance))
                        {
                            yield return new CompletionResult(PathTools.EscapePSText(version.Version));
                        }
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpId = Id.ConvertToWildcardPatternList();
            var wpVersion = Version.ConvertToWildcardPatternList();

            if (Destination == null)
            {
                Destination = SessionState.Path.CurrentFileSystemLocation.Path;
            }
            if (!Directory.Exists(Destination))
            {
                throw new DirectoryNotFoundException($"Directory {Destination} doesn't exist.");
            }


            // 最初にすべてまとめて非同期に API call するバージョン
            // ちゃんと動いているけど、API call の数が多すぎてしまうかも。
#if false
            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => {
                    var libraries = drive.GetLibraries();
                    return OrchThreadPool.RunForEach(libraries
                            .FilterByWildcards(l => l.Id!, wpId)
                            .Select(library => (drive, library)),
                        dl => dl.library.GetPSPath(),
                        dl => dl.library,
                        dl => drive.GetLibraryVersions(dl.library.Id!)
                            .FilterByWildcards(l => l.Version!, wpVersion)
                            .OrderBy(l => l.Version!, new VersionComparer()));
                });

            using var reporter = new ProgressReporter(this, 1, 100, "Export Library", "Export Library");
            foreach (var result in results)
            {
                try
                {
                    using var threads = result.GetResult();

                    foreach (var thread in threads!)
                    {
                        try
                        {
                            var versions = thread.GetResult();
                            var (drive, library) = thread.Source;

                            int index = 0;
                            int totalNum = versions!.Count();

                            reporter.TotalNum = totalNum;
                            foreach (var version in versions!)
                            {
                                reporter.WriteProgress(++index, $"{index:D}/{totalNum} {version.Id}:{version.Version}");
                                if (ShouldProcess(version.GetPSPath(), "Export Library"))
                                {
                                    try
                                    {
                                        var (fileName, fileContent) = drive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
                                        string filePath = System.IO.Path.Combine(Destination, fileName!);
                                        File.WriteAllBytes(filePath, fileContent);
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteError(new ErrorRecord(new OrchException(version.GetPSPath(), ex), "ExportLibraryError", ErrorCategory.InvalidOperation, version));
                                    }
                                }
                            }
                        }
                        catch (OrchException ex)
                        {
                            WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
#endif
            // 最初の GetLibrary() だけ非同期に実行するバージョン
            // GetLibraryVersion() は、ダウンロードの直前に呼び出す。
            // これくらいの方がバランスが良いか。
            // GetLibraryVersion() の実行時間は、さほど長くないし。
#if true
            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetLibraries());

            using var reporter = new ProgressReporter(this, 1, 100, "Export Library", "Export Library");
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var libraries = result.GetResult(cancelHandler.Token);
                    var drive = result.Source!; // Source プロパティを参照するのは、GetResult() の後にする必要がある

                    foreach (var library in libraries!
                        .FilterByWildcards(l => l?.Id, wpId)
                        .OrderBy(library => library.Id))
                    {
                        try
                        {
                            var versions = drive.GetLibraryVersions(library.Id!)
                                .FilterByWildcards(l => l?.Version, wpVersion)
                                //.OrderBy(l => l.Version!, VersionComparer.Instance)
                                .ToList();

                            int index = 0;
                            reporter.TotalNum = versions.Count;
                            foreach (var version in versions)
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();

                                string target = $"{version.Id}.{version.Version}.nupkg";
                                reporter.WriteProgress(++index, $"{index:D}/{versions.Count} {target}");
                                if (ShouldProcess(target, "Export Library"))
                                {
                                    try
                                    {
                                        var (fileName, fileContent) = drive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
                                        string filePath = System.IO.Path.Combine(Destination, fileName!);
                                        File.WriteAllBytes(filePath, fileContent);
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteError(new ErrorRecord(new OrchException(version.GetPSPath(), ex), "ExportLibraryError", ErrorCategory.InvalidOperation, version));
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
                            WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

#endif
        }
    }
}
