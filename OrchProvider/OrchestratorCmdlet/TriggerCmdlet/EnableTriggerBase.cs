using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using TPositional = UiPath.PowerShell.Positional.Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    public class EnableTriggerCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
    {
        public virtual string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        internal class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTriggers(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(t => Enable.Value
                            ? !t.Enabled.GetValueOrDefault()
                            : t.Enabled.GetValueOrDefault())
                        .Where(t => wp.IsMatch(t.Name))
                        .ExcludeByWildcards(t => t?.Name, wpName)
                        .OrderBy(t => t.Name))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.Text, tiphelp);
                    }
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
                    var schedules = drive.GetTriggers(folder);

                    foreach (var trigger in schedules
                        .Where(t => Enable.Value
                            ? !t.Enabled.GetValueOrDefault()
                            : t.Enabled.GetValueOrDefault())
                        .FilterByWildcards(t => t?.Name, wpName)
                        .OrderBy(t => t.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string action = $"{(Enable.Value ? "Enable" : "Disable")} Trigger";

                        if (ShouldProcess(trigger.GetPSPath(), action))
                        {
                            try
                            {
                                drive.OrchAPISession.EnableProcessSchedule(folder.Id ?? 0, [trigger.Id ?? 0], Enable.Value);
                                drive._dicTriggers?.TryRemove(folder.Id ?? 0, out _);
                                drive._dicTriggers_Exceptions.ClearCache();
                                drive._dicTriggersDetailed?.TryRemove(folder.Id ?? 0, out _);
                                drive._dicTriggersDetailed_Exceptions.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                string errorId = $"{(Enable.Value ? "Enable" : "Disable")}TriggerError";
                                WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableTriggerError", ErrorCategory.InvalidOperation, trigger));
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
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
                    continue;
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
        //        df => df.drive.GetProcessSchedule(df.folder)
        //    );

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var trigger in entities
        //                .Where(t => !t.Enabled.GetValueOrDefault())
        //                .FilterByWildcards(t => t.Name!, wpName)
        //                .OrderBy(t => t.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(trigger.GetPSPath(), "Enable Trigger"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.EnableProcessSchedule(folder.Id ?? 0, [trigger.Id ?? 0], true);
        //                        drive._dicProcessSchedule?.TryRemove(folder.Id ?? 0, out List<ProcessSchedule>? _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableTriggerError", ErrorCategory.InvalidOperation, trigger));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetTriggerError", ErrorCategory.InvalidOperation, ex.Target));
        //            continue;
        //        }
        //    }
        //}
    }
}
