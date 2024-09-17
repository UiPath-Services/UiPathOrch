using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id_Version_Destination;
using System.Globalization;
using System;
using UiPath.PowerShell.Entities;
using System.Text;

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
        [ArgumentCompleter(typeof(DestinationCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Id_Version_Destination>))]
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
                var wpId = CreateWPListFromParameter(commandAst, "Id", Positional.Id_Version_Destination.Parameters, wordToComplete);

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
                var wpId = CreateWPListFromOtherParameters(commandAst, "Id", Positional.Id_Version_Destination.Parameters);

                // パラメータで選択済みの Version は、候補から除外する
                var wpVersion = CreateWPListFromParameter(commandAst, "Version", Positional.Id_Version_Destination.Parameters, wordToComplete);

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

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // パラメータで選択済みの Path は、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.Id_Version_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みの Destination は、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.Id_Version_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
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

        protected override void ProcessRecord()
        {
            var srcDrives = OrchDriveInfo.EnumOrchDrives([Path]);
            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            var wpId = Id.ConvertToWildcardPatternList();
            var wpVersion = Version
                .Split1stValueByUnescapedCommas() // CSV から入力されている可能性があるので、カンマで区切る
                .ConvertToWildcardPatternList();

            string msg = "Copying libraries";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            foreach (var srcDrive in srcDrives)
            {
                srcDrive._dicLibraries = null;
                srcDrive._dicLibraries_Exceptions.ClearCache();
            }

            using var results = OrchThreadPool.RunForEach(srcDrives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetLibraries()
                    .FilterByWildcards(l => l?.Id, wpId)
                    .OrderBy(version => version.Id)
                    .ToList());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    var srcDrive = result.Source!;

                    foreach (var library in entities!)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        try
                        {
                            srcDrive._dicLibraryVersions = null;
                            var versions = srcDrive.GetLibraryVersions(library.Id!)
                                .FilterByWildcards(l => l?.Version, wpVersion)
                                //.OrderBy(version => version.Version!, VersionComparer.Instance)
                                .ToList();

                            int index = 0;
                            reporter.TotalNum = dstDrives.Count * versions.Count;
                            foreach (var version in versions)
                            {
                                string? fileName = null;
                                byte[]? fileContent = null;
                                foreach (var dstDrive in dstDrives)
                                {
                                    if (srcDrive == dstDrive) continue;

                                    string key = $"{version.GetPSPath()}:{version.Version}";
                                    string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

                                    reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

                                    if (ShouldProcess(target, $"Copy Library"))
                                    {
                                        // dstDrive に同名のパッケージがあれば、警告を表示してコピーをスキップする
                                        if (LibraryExists(dstDrive, version))
                                        {
                                            WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                            continue;
                                        }

                                        try
                                        {
                                            if (string.IsNullOrEmpty(fileName)) // fileName は、ダウンロード済みか否かのフラグとして機能している
                                            {
                                                (fileName, fileContent) = srcDrive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            WriteError(new ErrorRecord(new OrchException(target, ex), "DownloadLibraryError", ErrorCategory.InvalidOperation, target));
                                            break;
                                        }

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
                                        }
                                        catch (Exception ex)
                                        {
                                            WriteError(new ErrorRecord(new OrchException($"{version.Id}:{version.Version}", ex), "CopyLibraryError", ErrorCategory.InvalidOperation, version));
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
