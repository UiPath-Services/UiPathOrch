using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchFolderMachine", SupportsShouldProcess = true)]
public class RemoveFolderMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
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
}
