using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchQueue", SupportsShouldProcess = true)]
public class RemoveQueueCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
    [SupportsWildcards]
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
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            string targetFolder = folder.GetPSPath();
            try
            {
                var queues = drive.Queues.Get(folder);
                foreach (var queue in queues.FilterByWildcards(q => q?.Name, wpName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = queue.Name + " on " + targetFolder;
                    try
                    {
                        if (ShouldProcess(folder.GetPSPath(), "Remove Queue " + queue.Name))
                        {
                            drive.OrchAPISession.RemoveQueue(folder.Id ?? 0, queue.Id ?? 0);
                            drive.Queues.ClearCache(folder);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemoveQueueError", ErrorCategory.InvalidOperation, target);
                        WriteError(errorRecord);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(targetFolder, ex), "RemoveQueueError", ErrorCategory.InvalidOperation, targetFolder);
                WriteError(errorRecord);
            }
        }
    }
}
