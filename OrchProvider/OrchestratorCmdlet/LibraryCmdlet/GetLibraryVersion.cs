using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;

using Positional = UiPath.PowerShell.Positional.Id_Version;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchLibraryVersion")]
    [OutputType(typeof(Entities.Library))]
    public class GetLibraryVersionCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(IdCompleter))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(VersionCompleter))]
        [SupportsWildcards]
        public string[]? Version { get; set; }

        [Parameter]
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

                // パラメータで選択済みの Id は、候補から除外する
                var wpId = CreateWPListFromParameter(commandAst, "Id", Positional.Id_Version.Parameters, wordToComplete);

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

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpId = Id?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();
            var wpVersion = Version?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => {
                    var libraries = drive.GetLibraries();
                    return OrchThreadPool.RunForEach(libraries.FilterByWildcards(l => l?.Id, wpId),
                        lib => lib.GetPSPath(),
                        lib => lib,
                        lib => drive.GetLibraryVersions(lib.Id!)
                            .FilterByWildcards(l => l?.Version, wpVersion));
                            //.OrderBy(l => l.Version!, VersionComparer.Instance));
                });

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    using var threads = result.GetResult(cancelHandler.Token);

                    foreach (var thread in threads!)
                    {
                        try
                        {
                            var versions = thread.GetResult(cancelHandler.Token);
                            WriteObject(versions, true);
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


#if false
            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetLibraries());

            foreach (var result in results)
            {
                try
                {
                    var libraries = result.GetResult();
                    if (libraries == null) continue;

                    var drive = result.Source;

                    var matchedLibraries = libraries
                        .FilterByWildcards(p => p.Id!, wpId)
                        .OrderBy(l => l.Id!.ToLower()).ToList();

                    using var results2 = OrchThreadPool.RunForEach(matchedLibraries,
                        ml => drive.NameColonSeparator,
                        ml => drive,
                        ml => drive.GetLibraryVersions(ml.Id!));

                    foreach (var result2 in results2)
                    {
                        try
                        {
                            var versions = result2.GetResult();
                            if (versions == null) continue;

                            WriteObject(versions
                                .FilterByWildcards(p => p.Version!, wpVersion)
                                .OrderBy(p => p.Version!, new VersionComparer()),
                                true);
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
        }
    }
}
