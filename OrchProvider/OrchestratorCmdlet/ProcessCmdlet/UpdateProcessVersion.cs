using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchProcessVersion", SupportsShouldProcess = true, DefaultParameterSetName = "ReleaseName")]
public class UpdateProcessVersionCommand : OrchestratorPSCmdlet
{
    [Parameter (ParameterSetName = "ReleaseName", Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ParameterSetName = "ReleaseId", Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    [SupportsWildcards]
    public Int64[]? Id { get; set; }

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

    // TODO: Generalize as ResettableProcessNameCompleter
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

            // Exclude already-selected package names from candidates
            var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                var (drive, folder) = df;
                var releases = drive.GetReleases(folder)
                    .Where(r => wp.IsMatch(r.Name))
                    .Where(r => r.ProcessType != "TestAutomationProcess")
                    .ExcludeByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name)
                    .ToList();

                // Retrieve packages from the feed corresponding to the target releases
                return ParallelResults.GroupBy(releases, release => drive.GetPackageVersions(folder, release.Name!));
            });

            foreach (var releases in results)
            {
                foreach (var versions in releases)
                {
                    // Processes without multiple package versions cannot be updated/reset
                    // Note that updates to versions other than the latest are also possible
                    if (versions.Take(2).Count() < 2) continue;

                    var release = versions.Source;
                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected package names from candidates
            var wpId = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                var (drive, folder) = df;
                var releases = drive.GetReleases(folder)
                    .Where(r => wp.IsMatch(r.Name))
                    .Where(r => r.ProcessType != "TestAutomationProcess")
                    .ExcludeByWildcards(r => r?.Id.ToString(), wpId)
                    .OrderBy(r => r.Name)
                    .ToList();

                // Retrieve packages from the feed corresponding to the target releases
                return ParallelResults.GroupBy(releases, release => drive.GetPackageVersions(folder, release.Name!));
            });

            foreach (var releases in results)
            {
                foreach (var versions in releases)
                {
                    // Processes without multiple package versions cannot be updated/reset
                    // Note that updates to versions other than the latest are also possible
                    if (versions.Take(2).Count() < 2) continue;

                    var release = versions.Source;
                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult(PathTools.EscapePSText(release.Id.ToString()), release.Id.ToString(), CompletionResultType.ParameterValue, tiphelp);
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

            // Target only the Name selected by parameters
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude already-selected Version from candidates
            var wpVersion = CreateSelfExclusionList(commandAst, "Version", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                var (drive, folder) = df;
                var releases = drive.GetReleases(folder)
                    .Where(r => wp.IsMatch(r.Name))
                    .Where(r => r.ProcessType != "TestAutomationProcess")
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name)
                    .ToList();

                // Retrieve packages from the feed corresponding to the target releases
                return ParallelResults.GroupBy(releases, release => drive.GetPackageVersions(folder, release.Name!)
                    .Where(version => version.Version != release.CurrentVersion!.VersionNumber)
                );
            });

            foreach (var releases in results)
            {
                foreach (var versions in releases)
                {
                    foreach (var version in versions
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
        if (Name is null && Id is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -Name or -Id."), "UpdateProcessVersionError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
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
                if (Id is not null && Id.Length != 0)
                {
                    foreach (var id in Id)
                    {
                        string target = System.IO.Path.Combine(folder.GetPSPath(), id.ToString());
                        if (Version is null)
                        {
                            if (ShouldProcess(System.IO.Path.Combine(folder.GetPSPath(), id.ToString()), "Update Process Version to Latest"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToLatestVersion(folder.Id ?? 0, id);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateProcessError", ErrorCategory.InvalidOperation, folder));
                                }
                            }
                        }
                        else
                        {
                            if (ShouldProcess(target, $"Update Process Version to {Version}"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToSpecificVersion(folder.Id ?? 0, id, Version);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateProcessError", ErrorCategory.InvalidOperation, folder));
                                }
                            }
                        }
                    }
                }

                if (Name is not null && Name.Length != 0)
                {

                    var releases = drive.GetReleases(folder);

                    foreach (var release in releases
                        .Where(r => r.ProcessType != "TestAutomationProcess")
                        .FilterByWildcards(r => r?.Name, wpName)
                        .OrderBy(r => r.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (wpVersion is null)
                        {
                            if (release.IsLatestVersion ?? false)
                                continue;
                            if (ShouldProcess(release.GetPSPath(), "Update Process Version to Latest"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToLatestVersion(folder.Id ?? 0, release.Id ?? 0);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
                                }
                            }
                        }
                        else
                        {
                            // Retrieve packages from the feed corresponding to the target release
                            var packageVersions = drive.GetPackageVersions(folder, release.Name!).Select(p => p.Version!);

                            var toVersion = packageVersions.Where(v => wpVersion.IsMatch(v)).LastOrDefault();
                            if (toVersion is null) continue;
                            if (release.CurrentVersion!.VersionNumber == toVersion) continue;

                            if (ShouldProcess(release.GetPSPath(), $"Update Process Version to {toVersion}"))
                            {
                                try
                                {
                                    drive.OrchAPISession.UpdateReleaseToSpecificVersion(folder.Id ?? 0, release.Id ?? 0, toVersion);
                                    drive._dicReleases?.TryRemove(folder.Id ?? 0, out _);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), ex), "UpdateProcessError", ErrorCategory.InvalidOperation, release));
                                }
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

    // Multi-threaded version
    // Rewritten as single-threaded because it could be slower when HTTP calls are capped
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
    //    var wpName = Name.ConvertToWildcardPatternList();
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
    //            if (releases is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var release in releases
    //                .Where(r => r.ProcessType != "TestAutomationProcess")
    //                .FilterByWildcards(r => r.Name!, wpName))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (wpVersion is null)
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
    //                    // Retrieve packages from the feed corresponding to the target release
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
