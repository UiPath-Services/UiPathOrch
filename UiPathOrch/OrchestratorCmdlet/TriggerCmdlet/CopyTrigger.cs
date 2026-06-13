using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchTrigger", SupportsShouldProcess = true)]
public class CopyTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(EffectivePath(Path, LiteralPath));
        var srcDrivesFolders = SessionState.EnumFolders([EffectivePath(Path, LiteralPath)], Recurse.IsPresent, Depth, false);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);
        var dstFolderCache = new Dictionary<string, Folder?>();

        // If source and destination are the same, do nothing
        if (srcRootFolder == dstRootFolder) return;

        using var reporterTriggers = new ProgressReporter(this, 800, Int32.MaxValue, "Copying triggers...");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // If there are no entities to copy, there is no need to look up the dstFolder
                //srcDrive._dicTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.GetTriggers(srcFolder).FilterByNames(e => e?.Name, Name);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, createIfMissing: true, createCache: dstFolderCache);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            // Clear the cache beforehand
            dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

            try
            {
                Core.OrchProvider.CopyTriggers(this,
                    srcDrive, srcFolder, Name,
                    dstDrive, dstFolder, reporterTriggers,
                    false, cancelHandler.Token);
                dstDrive.Triggers.ClearCache(dstFolder);
                dstDrive.TriggersDetailed.ClearCache(dstFolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyTriggerError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
