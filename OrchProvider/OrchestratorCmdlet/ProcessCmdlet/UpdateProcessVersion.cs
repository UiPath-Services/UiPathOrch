using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Version;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchProcessVersion", SupportsShouldProcess = true)]
    public class UpdateProcessVersionCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(VersionCompleter))]
        [SupportsWildcards]
        public string? Version { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        // TODO: ResettableProcessNameCompleter として共通化する
        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みのパッケージ名は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name_Version.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df =>
                {
                    var (drive, folder) = df;
                    var releases = drive.GetReleases(folder)
                        .Where(r => wp.IsMatch(r.Name))
                        .Where(r => r.ProcessType != "TestAutomationProcess")
                        .ExcludeByWildcards(r => r?.Name, wpName)
                        .OrderBy(r => r.Name)
                        .ToList();

                    // 対象のリリースに対応するパッケージをフィードから取り出す
                    return ParallelResults.ForEach(releases, release => drive.GetPackageVersions(folder, release.Name!));
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var releasesPackages)) continue;

                    foreach (var releasePackages in releasesPackages!)
                    {
                        if (releasePackages.TryGetValue(out var packages))
                        {
                            // パッケージが複数ないプロセスは、update/reset できない
                            // 最新バージョン以外のバージョンにアップデートする場合もあることに注意
                            if (packages!.Count <= 1) continue;
                        }

                        var release = releasePackages.Source;
                        string tiphelp = TipHelp(release);
                        yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.ParameterValue, tiphelp);
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
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択された Name のみ対象とする
                var wpName = CreateWPListFromOtherParameters(commandAst, "Name", Positional.Name_Version.Parameters);

                // パラメータで選択済みの Version 候補から除外する
                var wpVersion = CreateWPListFromParameter(commandAst, "Version", Positional.Name_Version.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df =>
                {
                    var (drive, folder) = df;
                    var releases = drive.GetReleases(folder)
                        .Where(r => wp.IsMatch(r.Name))
                        .Where(r => r.ProcessType != "TestAutomationProcess")
                        .FilterByWildcards(r => r?.Name, wpName)
                        .ToList();

                    // 対象のリリースに対応するパッケージをフィードから取り出す
                    return ParallelResults.ForEach(releases, release => drive.GetPackageVersions(folder, release.Name!)
                        .Where(version => version.Version != release.CurrentVersion!.VersionNumber)
                    );
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var releasesPackages)) continue;

                    foreach (var releasePackages in releasesPackages!)
                    {
                        if (!releasePackages.TryGetValue(out var packages)) continue;

                        foreach (var version in packages!
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
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();
            WildcardPattern wpVersion = null;
            if (!string.IsNullOrEmpty(Version))
            {
                wpVersion = new WildcardPattern(Version, WildcardOptions.IgnoreCase);
            }

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                try
                {
                    var releases = drive.GetReleases(folder);

                    foreach (var release in releases
                        .Where(r => r.ProcessType != "TestAutomationProcess")
                        .FilterByWildcards(r => r?.Name, wpName))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (wpVersion == null)
                        {
                            if (release.IsLatestVersion ?? false)
                                continue;
                            if (ShouldProcess(release.GetPSPath(), "Update Process Version to Latest"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToLatestVersion(folder.Id ?? 0, release.Id ?? 0);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out List<Release>? _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
                                }
                            }
                        }
                        else
                        {
                            // 対象のリリースに対応するパッケージをフィードから取り出す
                            var packageVersions = drive.GetPackageVersions(folder, release.Name!).Select(p => p.Version!);

                            var toVersion = packageVersions.Where(v => wpVersion.IsMatch(v)).LastOrDefault();
                            if (toVersion == null) continue;
                            if (release.CurrentVersion!.VersionNumber == toVersion) continue;

                            if (ShouldProcess(release.GetPSPath(), $"Update Process Version to {toVersion}"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToSpecificVersion(folder.Id ?? 0, release.Id ?? 0, toVersion);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out List<Release>? _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
                                }
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
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        //    var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();
        //    WildcardPattern wpVersion = null;
        //    if (!string.IsNullOrEmpty(Version))
        //    {
        //        wpVersion = new WildcardPattern(Version, WildcardOptions.IgnoreCase);
        //    }

        //    using var results = OrchThreadPool.RunForEach(drivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetReleases(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var releases = result.GetResult(cancelHandler.Token);
        //            if (releases == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var release in releases
        //                .Where(r => r.ProcessType != "TestAutomationProcess")
        //                .FilterByWildcards(r => r.Name!, wpName))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (wpVersion == null)
        //                {
        //                    if (release.IsLatestVersion ?? false)
        //                        continue;
        //                    if (ShouldProcess(release.GetPSPath(), "Update Process Version to Latest"))
        //                    {
        //                        try
        //                        {
        //                            drive.OrchAPISession.UpdateReleaseToLatestVersion(folder.Id ?? 0, release.Id ?? 0);
        //                            drive._dicReleases?.TryRemove(folder.Id.Value, out List<Release>? _);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    // 対象のリリースに対応するパッケージをフィードから取り出す
        //                    var packageVersions = drive.GetPackageVersions(folder, release.Name!).Select(p => p.Version!);

        //                    var toVersions = packageVersions.Where(v => wpVersion.IsMatch(v));
        //                    if (toVersions.Count() == 0)
        //                    {
        //                        continue;
        //                    }
        //                    else if (toVersions.Count() > 1)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), "There are multiple matching versions"), "UpdateProcessVersionToLatestError", ErrorCategory.InvalidOperation, release));
        //                        continue;
        //                    }
        //                    string toVersion = toVersions.First();

        //                    if (release.CurrentVersion!.VersionNumber == toVersion)
        //                        continue;

        //                    if (ShouldProcess(release.GetPSPath(), $"Update Process Version to {toVersion}"))
        //                    {
        //                        try
        //                        {
        //                            drive.OrchAPISession.UpdateReleaseToSpecificVersion(folder.Id ?? 0, release.Id ?? 0, toVersion);
        //                            drive._dicReleases?.TryRemove(folder.Id.Value, out List<Release>? _);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetProcessError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
