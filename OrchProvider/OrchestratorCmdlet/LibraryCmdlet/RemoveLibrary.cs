using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id_Version;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchLibrary", SupportsShouldProcess = true)]
    public class RemoveLibraryCommand : OrchestratorPSCmdlet//, IDynamicParameters
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(VersionCompleter))]
        [SupportsWildcards]
        public string[]? Version { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Id_Version>))]
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

                // パラメータで選択済みのパッケージ名は、候補から除外する
                var wpId = CreateWPListFromParameter(commandAst, "Id", Positional.Id_Version.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetLibraries());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(l => wp.IsMatch(l.Id))
                        .ExcludeByWildcards(l => l?.Id, wpId)
                        .OrderBy(l => l.Id))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.Id), e.Id, CompletionResultType.ParameterValue, tiphelp);
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

                // パラメータで選択済みの Id は、候補から除外する
                var paramId = GetFakeBoundParameters(fakeBoundParameters, "Id");
                var wpId = paramId.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みの Version は、候補から除外する
                var wpVersion = CreateWPListFromParameter(commandAst, "Version", Positional.Id_Version.Parameters, wordToComplete);

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

        // もともとマルチスレッドになってなかった
        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpId = Id?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();
            var wpVersion = Version?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var libraries = drive.GetLibraries()
                        .FilterByWildcards(l => l?.Id, wpId!)
                        .OrderBy(l => l.Id!.ToLower());

                    foreach (var library in libraries)
                    {
                        try
                        {
                            var matchingVersions = drive.GetLibraryVersions(library.Id!)
                                .FilterByWildcards(v => v?.Version, wpVersion);
                                //.OrderBy(v => v.Version!, VersionComparer.Instance);

                            foreach (var matchingVersion in matchingVersions)
                            {
                                cancelHandler.Token.ThrowIfCancellationRequested();

                                string target = $"{drive.NameColonSeparator}{matchingVersion.Id}:{matchingVersion.Version}";
                                if (ShouldProcess(target, "Remove Library"))
                                {
                                    try
                                    {
                                        drive.OrchAPISession.RemoveLibrary(matchingVersion.Id!, matchingVersion.Version!);
                                        drive._dicLibraries = null;
                                        drive._dicLibraryVersions?.TryRemove(matchingVersion.Id!, out List<LibraryVersion>? _);
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveLibraryError", ErrorCategory.InvalidOperation, matchingVersion));
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
                            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, drive));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetLibraryError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
