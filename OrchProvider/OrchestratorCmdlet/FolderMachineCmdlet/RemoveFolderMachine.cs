using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchFolderMachine", SupportsShouldProcess = true)]
    public class RemoveFolderMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

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

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetMachinesAssignedToFolder(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(m => wp.IsMatch(m.Name))
                        .ExcludeByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, TipHelp(e));
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
                cancelHandler.Token.ThrowIfCancellationRequested();
                try
                {
                    var entities = drive.GetMachinesAssignedToFolder(folder);

                    var removingMachines = entities.FilterByWildcards(m => m?.Name, wpName);
                    if (!removingMachines.Any()) continue;

                    var machineIds = removingMachines.Select(m => m.Id ?? 0);

                    string targetMachines = string.Join(", ", removingMachines.Select(m => m.Name!));
                    if (ShouldProcess(folder.GetPSPath(), "Remove Folder Machines " + targetMachines))
                    {
                        try
                        {
                            drive.OrchAPISession.UnassignMachinesFromFolder(folder.Id ?? 0, machineIds);
                            drive._dicMachinesAssigned?.TryRemove(folder.Id ?? 0, out var _);
                            drive._dicAssignedMachines?.TryRemove(folder.Id ?? 0, out var _);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveFolderMachineError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, folder));
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
        //        df => df.drive.GetMachinesAssignedToFolder(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        cancelHandler.Token.ThrowIfCancellationRequested();

        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            var removingMachines = entities.FilterByWildcards(m => m.Name!, wpName);
        //            if (!removingMachines.Any()) continue;

        //            var machineIds = removingMachines.Select(m => m.Id ?? 0);

        //            string targetMachines = string.Join(", ", removingMachines.Select(m => m.Name!));
        //            if (ShouldProcess(folder.GetPSPath(), "Remove Folder Machines " + targetMachines))
        //            {
        //                try
        //                {
        //                    drive.OrchAPISession.UnassignMachinesFromFolder(folder.Id ?? 0, machineIds);
        //                    drive._dicMachinesAssigned?.TryRemove(folder.Id.Value, out List<MachineFolder>? _);
        //                }
        //                catch (Exception ex)
        //                {
        //                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveFolderMachineError", ErrorCategory.InvalidOperation, folder));
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
