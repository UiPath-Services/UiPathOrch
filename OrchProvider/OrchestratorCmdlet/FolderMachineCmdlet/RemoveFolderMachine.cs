using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchFolderMachine", SupportsShouldProcess = true)]
public class RemoveFolderMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            try
            {
                var entities = drive.FolderMachinesAssigned.Get(folder);

                var removingMachines = entities.FilterByWildcards(m => m?.Name, wpName);

                List<Int64> machineIdsToRemove = [];
                foreach (var machine in removingMachines.OrderBy(m => m.Name))
                {
                    if (ShouldProcess(machine.GetPSPath(), "Remove FolderMachine"))
                    {
                        machineIdsToRemove.Add(machine.Id!.Value);
                    }
                }

                if (machineIdsToRemove.Count == 0) continue;

                try
                {
                    drive.OrchAPISession.UnassignMachinesFromFolder(folder.Id ?? 0, machineIdsToRemove);
                    drive.FolderMachinesAssigned.ClearCache(folder);
                    drive.FolderMachinesAssignable.ClearCache(folder);
                    drive.MachinesRobots.ClearCache(folder);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveFolderMachineError", ErrorCategory.InvalidOperation, folder));
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
    //            if (entities is null) continue;

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
