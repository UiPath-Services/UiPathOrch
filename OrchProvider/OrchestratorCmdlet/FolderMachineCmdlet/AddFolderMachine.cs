using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchFolderMachine", SupportsShouldProcess = true)]
    public class AddFolderMachineCommand : OrchestratorPSCmdlet
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

                var combinations = drivesFolders.SelectMany(df => Enumerable.Range(0, 2), (df, i) => (df.drive, df.folder, i));

                Parallel.ForEach(combinations, dfi =>
                {
                    var (drive, folder, index) = dfi;
                    try
                    {
                        switch (index)
                        {
                            case 0: drive.GetMachinesAssignedToFolder(folder); break;
                            case 1: drive.GetMachinesAssignableToFolder(folder); break;
                        }
                    }
                    catch { }
                });

                foreach (var (drive, folder) in drivesFolders)
                {
                    List<MachineFolder> assigneds = null;
                    List<MachineFolder> assignables = null;
                    drive._dicMachinesAssigned?.TryGetValue(folder.Id ?? 0, out assigneds);
                    if (drive._dicMachinesAssignable?.TryGetValue(folder.Id ?? 0, out assignables) ?? false)
                    {
                        foreach (var assignable in assignables!
                            .Where(m => wp.IsMatch(m.Name))
                            .ExcludeByWildcards(m => m?.Name, wpName)
                            .ExcludeByClassValues(m => m?.Name, assigneds?.Select(a => a?.Name!))
                            .OrderBy(m => m.Name))
                        {
                            yield return new CompletionResult(PathTools.EscapePSText(assignable.Name), assignable.Name, CompletionResultType.ParameterValue, TipHelp(assignable));
                        }
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetMachinesAssignableToFolder(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var machines = result.GetResult(cancelHandler.Token);
                    if (machines == null) continue;
                    var (drive, folder) = result.Source;

                    var addingMachines = machines!.FilterByWildcards(m => m?.Name, wpName);
                    string targetFolder = folder.GetPSPath();
                    try
                    {
                        var machineIds = addingMachines.Select(m => m.Id ?? 0);
                        string targetMachines = string.Join(", ", addingMachines.Select(m => m.Name!));

                        string target = $"Item: {targetMachines} Destination: {folder.GetPSPath()}";
                        if (ShouldProcess(target, "Add Folder Machines"))
                        {
                            drive.OrchAPISession.AddMachinesToFolder(folder.Id ?? 0, machineIds);
                            drive._dicMachinesAssigned?.TryRemove(folder.Id ?? 0, out var _);
                            drive._dicAssignedMachines?.TryRemove(folder.Id ?? 0, out var _);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, ex), "AddFolderMachineError", ErrorCategory.InvalidOperation, folder);
                        WriteError(errorRecord);
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
