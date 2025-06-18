using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Reset, "OrchProcessVersion", SupportsShouldProcess = true)]
public class ResetProcessVersionCommand: OrchestratorPSCmdlet
{
    [Parameter (Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.GetReleases(df.folder));

            foreach (var release in results
                .Select(r => r.Item)
                .Where(p => p.ProcessType != "TestAutomationProcess")
                .Where(p => p.ReleaseVersions is not null && p.ReleaseVersions.Length >= 2)
                .Where(p => wp.IsMatch(p.Name))
                .ExcludeByWildcards(p => p?.Name, wpName)
                .OrderBy(p => p.Name))
            {
                string tiphelp = TipHelp(release);
                yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var releases = drive.GetReleases(folder);

                foreach (var release in releases
                    .Where(e => e.ProcessType != "TestAutomationProcess")
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    if (release.ReleaseVersions is null || release.ReleaseVersions.Length <= 1)
                    {
                        continue;
                    }

                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = release.GetPSPath();
                    if (ShouldProcess(target, "Reset Process Version"))
                    {
                        try
                        {
                            drive.OrchAPISession.RollbackReleaseVersion(folder.Id!.Value, release.Id!.Value);
                            drive._dicReleases?.TryRemove(folder.Id.Value, out _);
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(target, ex), "ResetProcessVersionError", ErrorCategory.InvalidOperation, target);
                            WriteError(errorRecord);
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
    //    var wpName = Name.ConvertToWildcardPatternList();

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
    //                .Where(e => e.ProcessType != "TestAutomationProcess")
    //                .FilterByWildcards(e => e.Name!, wpName)
    //                .OrderBy(e => e.Name))
    //            {
    //                if (release.ReleaseVersions is null || release.ReleaseVersions.Length <= 1)
    //                {
    //                    continue;
    //                }

    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                string target = release.GetPSPath();
    //                if (ShouldProcess(target, "Reset Process Version"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RollbackReleaseVersion(folder.Id!.Value, release.Id!.Value);
    //                        drive._dicReleases?.TryRemove(folder.Id.Value, out List<Release>? _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        var errorRecord = new ErrorRecord(new OrchException(target, ex), "ResetProcessVersionError", ErrorCategory.InvalidOperation, target);
    //                        WriteError(errorRecord);
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
