using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchFolderMachine", SupportsShouldProcess = true)]
public class CopyFolderMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);
        var srcDrivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);

        // Do nothing if source and destination are the same
        if (srcRootFolder == dstRootFolder) return;

        var wpName = Name.ConvertToWildcardPatternList();

        using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, "Copying folder machines...");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // No need to search for dstFolder if there are no entities to copy
                //srcDrive._dicMachinesAssigned?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.FolderMachinesAssigned.Get(srcFolder)
                    .Where(e => e.IsAssignedToFolder.GetValueOrDefault())
                    .FilterByWildcards(e => e?.Name, wpName);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                Core.OrchProvider.CopyFolderMachines(this,
                    srcDrive, srcFolder, wpName,
                    dstDrive, dstFolder, reporter,
                    false, cancelHandler.Token);
                dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);
                dstDrive.FolderMachinesAssignable.ClearCache(dstFolder);
                dstDrive.MachinesRobots.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
