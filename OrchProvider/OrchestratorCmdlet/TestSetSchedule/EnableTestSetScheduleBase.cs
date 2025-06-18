using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

public class EnableTestSetScheduleCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // この completer は、無効なスケジュールだけを表示するので共通化できない
    internal class NameCompleter : OrchArgumentCompleter
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
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestSetSchedules.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var entity in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
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

        string action = $"{(Enable.Value ? "Enable" : "Disable")} TestSetSchedule";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var schedules = drive.TestSetSchedules.Get(folder);

                foreach (var schedule in schedules
                    .FilterByWildcards(ts => ts?.Name, wpName)
                    .OrderBy(ts => ts.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var target = schedule.GetPSPath();

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, Enable.Value, [schedule.Id ?? 0]);
                            drive.TestSetSchedules.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}TestSetScheduleError";
                            WriteError(new ErrorRecord(new OrchException(target, ex), errorId, ErrorCategory.InvalidOperation, schedule));
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
    //            if (entities is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var entity in entities
    //                .FilterByWildcards(ts => ts.Name!, wpName)
    //                .OrderBy(ts => ts.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(entity.GetPSPath(), "Enable TestSetSchedule"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, true, [entity.Id ?? 0]);
    //                        drive._dicTestSetSchedules?.TryRemove(folder.Id.Value, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(entity.GetPSPath(), ex), "EnableTestSetScheduleError", ErrorCategory.InvalidOperation, entity));
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
