using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchTestSetSchedule", SupportsShouldProcess = true)]
    public class DisableTestSetScheduleCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
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

        // この completer は、有効なスケジュールだけを表示するので共通化できない
        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var recurse = GetSwitchParameterValue(commandAst, "Recurse");
                var paramDepth = GetParameterValue(commandAst, "Depth");
                uint.TryParse(paramDepth, out uint depth);

                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTestSetSchedules(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var entity in entities!
                        .Where(e => e.Enabled.GetValueOrDefault()) /////////////////////////////////////////
                        .Where(e => wp.IsMatch(e.Name!))
                        .ExcludeByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                    {
                        string tiphelp = TipHelp(entity);
                        yield return new CompletionResult(PathTools.EscapePSText(entity.Name), entity.Name, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
   
        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                try
                {
                    var schedules = drive.GetTestSetSchedules(folder);

                    foreach (var schedule in schedules
                        .FilterByWildcards(ts => ts?.Name, wpName)
                        .OrderBy(ts => ts.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        var target = schedule.GetPSPath();
                        if (ShouldProcess(target, "Disable TestSetSchedule"))
                        {
                            try
                            {
                                drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, false, [schedule.Id ?? 0]);
                                drive._dicTestSetSchedules?.TryRemove(folder.Id ?? 0, out _);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "DisableTestSetScheduleError", ErrorCategory.InvalidOperation, schedule));
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
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        //    var wpName = Name.ConvertToWildcardPatternList();

        //    using var results = OrchThreadPool.RunForEach(drivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetTestSetSchedules(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var entity in entities
        //                .FilterByWildcards(ts => ts.Name!, wpName)
        //                .OrderBy(ts => ts.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(entity.GetPSPath(), "Disable TestSetSchedule"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, false, [entity.Id ?? 0]);
        //                        drive._dicTestSetSchedules?.TryRemove(folder.Id.Value, out _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(entity.GetPSPath(), ex), "DisableTestSetScheduleError", ErrorCategory.InvalidOperation, entity));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetTestSetScheduleError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
