using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchApiTrigger", SupportsShouldProcess = true)]
    public class DisableApiTriggerCommand : OrchestratorPSCmdlet
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

        // 有効となっている API trigger だけを列挙するので、これは共通にできない
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

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetHttpTriggers(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var trigger in entities!
                        .Where(t => t.Enabled.GetValueOrDefault()) ////////////////////////////////////////////
                        .Where(t => wp.IsMatch(t.Name))
                        .ExcludeByWildcards(t => t?.Name, wpName))
                    {
                        string tooltip = trigger.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tooltip);
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
                    var triggers = drive.GetHttpTriggers(folder);

                    foreach (var trigger in triggers
                        .Where(t => t.Enabled.GetValueOrDefault())
                        .FilterByWildcards(t => t?.Name, wpName)
                        .OrderBy(t => t.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(trigger.GetPSPath(), "Disable ApiTrigger"))
                        {
                            try
                            {
                                drive.OrchAPISession.EnableHttpTriggers(folder.Id ?? 0, new string[] { trigger.Id! }, false);
                                drive._dicHttpTriggers?.TryRemove(folder.Id ?? 0, out var _);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableApiTriggerError", ErrorCategory.InvalidOperation, trigger));
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
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, drive));
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
        //        df => df.drive.GetHttpTriggers(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var triggers = result.GetResult(cancelHandler.Token);
        //            if (triggers == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var trigger in triggers
        //                .Where(t => t.Enabled.GetValueOrDefault())
        //                .FilterByWildcards(t => t.Name!, wpName)
        //                .OrderBy(t => t.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(trigger.GetPSPath(), "Disable ApiTrigger"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.EnableHttpTriggers(folder.Id ?? 0, new string[] { trigger.Id! }, false);
        //                        drive._dicHttpTriggers?.TryRemove(folder.Id ?? 0, out var _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "EnableApiTriggerError", ErrorCategory.InvalidOperation, trigger));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetApiTriggerError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
