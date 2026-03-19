using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

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
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

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

    //protected override void ProcessRecord()
    //{
    //    var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
    //    var srcDrivesFolders = OrchDriveInfo.EnumFoldersForRecursiveCopy(Path, Recurse.IsPresent, Depth);

    //    var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(srcDrivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetMachinesAssignedToFolder(df.folder));

    //    string msg = "Copying folder machines...";
    //    using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var dstDriveFolder in dstDrivesFolders)
    //    {
    //        var (dstDrive, dstFolder) = dstDriveFolder;
    //        foreach (var result in results)
    //        {
    //            cancelHandler.Token.ThrowIfCancellationRequested();

    //            try
    //            {
    //                var entities = result.GetResult(cancelHandler.Token);
    //                if (entities is null) continue;

    //                var (srcDrive, srcFolder) = result.Source;

    //                try
    //                {
    //                    Core.OrchProvider.CopyFolderMachines(this,
    //                    srcDrive, srcFolder, wpName,
    //                    dstDrive, dstFolder, reporter, false);
    //                    dstDrive._dicMachinesAssigned?.TryRemove(dstFolder.Id ?? 0, out _);
    //                }
    //                catch (Exception ex)
    //                {
    //                    string target = dstFolder.GetPSPath();
    //                    WriteError(new ErrorRecord(new OrchException(target, ex), "CreateFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                string target = dstFolder.GetPSPath();
    //                WriteError(new ErrorRecord(new OrchException(target, ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
    //            }
    //        }
    //    }
    //}
}
