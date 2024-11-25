using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchLibrary", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.BulkItemDtoOfString))]
    public class CopyLibraryCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(VersionCompleter))]
        public string[]? Version { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        [SupportsWildcards]
        public string? Path { get; set; }

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

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetLibraries());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var library in entities!
                        .Where(l => wp.IsMatch(l.Id))
                        .ExcludeByWildcards(l => l?.Id, wpId)
                        .OrderBy(l => l.Id))
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
                            string tiphelp = TipHelp(version);
                            yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                        }
                    }
                }
            }
        }

        private static bool LibraryExists(OrchDriveInfo drive, LibraryVersion version)
        {
            try
            {
                // Key でも検索できるんだけど、キャッシュが壊れちゃう。
                var dstExistingVersions = drive.GetLibraryVersions(version.Id!);
                if (dstExistingVersions != null)
                {
                    return dstExistingVersions.Any(v => v.Version == version.Version);
                }
            }
            catch { } // この例外は握りつぶす

            return false;
        }

        private bool DownloadLibrary(OrchDriveInfo srcDrive, LibraryVersion version, out string? fileName, out byte[]? fileContent, string target)
        {
            try
            {
                (fileName, fileContent) = srcDrive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
                return true;
            }
            catch (Exception ex)
            {
                fileName = null;
                fileContent = null;
                WriteError(new ErrorRecord(new OrchException(target, ex), "DownloadLibraryError", ErrorCategory.InvalidOperation, target));
            }
            return false;
        }

        private bool UploadLibrary(LibraryVersion version, string? fileName, byte[]? fileContent, OrchDriveInfo dstDrive)
        {
            if (string.IsNullOrEmpty(fileName) || fileContent == null) return false;
            try
            {
                var uploadedLibrary = dstDrive.OrchAPISession.UploadLibrary(fileName!, fileContent!);
                if (uploadedLibrary != null)
                {
                    //uploadedLibrary.Path = dstDrive.NameColonSeparator;
                    //WriteObject(result);
                    dstDrive._dicLibraries = null;
                    dstDrive._dicLibraryVersions = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException($"{version.Id}:{version.Version}", ex), "CopyLibraryError", ErrorCategory.InvalidOperation, version));
            }
            return false;
        }

        protected override void ProcessRecord()
        {
            var srcDrives = OrchDriveInfo.EnumOrchDrives([Path]);
            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            var wpId = Id.ConvertToWildcardPatternList();
            var wpVersion = Version
                .Split1stValueByUnescapedCommas() // CSV から入力されている可能性があるので、カンマで区切る
                .ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var srcDrive in srcDrives)
            {
                var srcLibraries = srcDrive.GetLibraries();

                string msg1 = "Processing libraries...";
                int index1 = 0;
                using var reporter1 = new ProgressReporter(this, 1, srcLibraries.Count, msg1, msg1);
                foreach (var library in srcLibraries)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    reporter1.WriteProgress(++index1, $"{index1:D}/{reporter1.TotalNum}");
                    try
                    {
                        srcDrive._dicLibraryVersions = null;
                        var versions = srcDrive.GetLibraryVersions(library.Id!)
                            .FilterByWildcards(l => l?.Version, wpVersion)
                            //.OrderBy(version => version.Version!, VersionComparer.Instance)
                            .ToList();

                        string msg2 = "Copying versions...    ";
                        using var reporter2 = new ProgressReporter(this, 2, dstDrives.Count * versions.Count, msg2, msg2);
                        int index2 = 0;
                        foreach (var version in versions)
                        {
                            string? fileName = null;
                            byte[]? fileContent = null;
                            foreach (var dstDrive in dstDrives)
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();

                                string key = $"{version.GetPSPath()}:{version.Version}";
                                string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

                                if (srcDrive == dstDrive) continue;

                                // dstDrive に同名のパッケージがあれば、警告を表示してコピーをスキップする
                                if (LibraryExists(dstDrive, version))
                                {
                                    WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                    continue;
                                }

                                if (ShouldProcess(target, $"Copy Library"))
                                {
                                    // 進捗は、実際にコピーするときにだけ表示された方が良い
                                    reporter2.WriteProgress(++index2, $"{index2:D}/{reporter2.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

                                    if (fileName == null)
                                    {
                                        if (!DownloadLibrary(srcDrive, version, out fileName, out fileContent, target)) break;
                                    }
                                    UploadLibrary(version, fileName, fileContent, dstDrive);
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


            // マルチスレッド版。ちゃんと動作しているが、シングルスレッドで書き直すことにした。
            // コピー元ドライブをマルチスレッドにすることは意味がなかった。。
            // GetLibraries() はコストが安いし、各ドライブに対して一度しか実行しない
            // そもそも、コピー元はひとつしか指定できないようにしてあるしな。。
            //using var results = OrchThreadPool.RunForEach(srcDrives,
            //    drive => drive.NameColonSeparator,
            //    drive => drive,
            //    drive => drive.GetLibraries()
            //        .FilterByWildcards(l => l?.Id, wpId)
            //        .OrderBy(version => version.Id)
            //        .ToList());

            //using var cancelHandler = new ConsoleCancelHandler();
            //foreach (var result in results)
            //{
            //    try
            //    {
            //        var entities = result.GetResult(cancelHandler.Token);
            //        var srcDrive = result.Source!;

            //        foreach (var library in entities!)
            //        {
            //            try
            //            {
            //                srcDrive._dicLibraryVersions = null;
            //                var versions = srcDrive.GetLibraryVersions(library.Id!)
            //                    .FilterByWildcards(l => l?.Version, wpVersion)
            //                    //.OrderBy(version => version.Version!, VersionComparer.Instance)
            //                    .ToList();

            //                int index = 0;
            //                reporter.TotalNum = dstDrives.Count * versions.Count;
            //                foreach (var version in versions)
            //                {
            //                    string? fileName = null;
            //                    byte[]? fileContent = null;
            //                    foreach (var dstDrive in dstDrives)
            //                    {
            //                        cancelHandler.Token.ThrowIfCancellationRequested();

            //                        string key = $"{version.GetPSPath()}:{version.Version}";
            //                        string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

            //                        if (srcDrive == dstDrive) continue;

            //                        // dstDrive に同名のパッケージがあれば、警告を表示してコピーをスキップする
            //                        if (LibraryExists(dstDrive, version))
            //                        {
            //                            WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
            //                            continue;
            //                        }

            //                        if (ShouldProcess(target, $"Copy Library"))
            //                        {
            //                            // 進捗は、実際にコピーするときにだけ表示された方が良い
            //                            reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

            //                            if (fileName == null)
            //                            {
            //                                if (!DownloadLibrary(srcDrive, version, out fileName, out fileContent, target)) break;
            //                            }
            //                            UploadLibrary(version, fileName, fileContent, dstDrive);
            //                        }
            //                    }
            //                }
            //            }
            //            catch (OperationCanceledException)
            //            {
            //                throw;
            //            }
            //            catch (Exception ex)
            //            {
            //                WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
            //            }
            //        }
            //    }
            //    catch (OrchException ex)
            //    {
            //        WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            //    }
            //}
        }
    }
}
