using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTask", SupportsShouldProcess = true, DefaultParameterSetName = "FromCommandLine")]
public class RemoveTaskCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    [ArgumentCompleter(typeof(TaskIdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(DontShow = true, ValueFromPipeline = true, ParameterSetName = "FromPipeline")]
    public OrchTask? Task { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        if (Task is not null)
        {
            var dfs = SessionState.EnumFolders(new string[] { Task.Path! });
            foreach (var (drive, folder) in dfs.WithCancellation(cancelHandler.Token))
            {
                RemoveOne(drive, folder, Task.Id ?? 0);
            }
            return;
        }

        var drivesFolders = SessionState.EnumFolders(Path);
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var taskId in Id!.WithCancellation(cancelHandler.Token))
            {
                RemoveOne(drive, folder, taskId);
            }
        }
    }

    private void RemoveOne(OrchDriveInfo drive, Folder folder, Int64 taskId)
    {
        string target = $"{folder.GetPSPath()} Task {taskId}";
        if (!ShouldProcess(target, "Remove Task")) return;

        try
        {
            drive.OrchAPISession.RemoveTask(folder.Id ?? 0, taskId);
            drive.Tasks.ClearCache(folder);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveTaskError", ErrorCategory.InvalidOperation, taskId));
        }
    }
}
