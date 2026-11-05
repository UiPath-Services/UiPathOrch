using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Wraps GET /tasks/GenericTasks/GetTaskDataById: the task with its data (the form values,
// attachments among them) and the action taken, which Get-OrchTask (OData Tasks) does not carry.
//   Get-OrchTask -Status Completed | Get-OrchTaskData | Select-Object Title, Action, Data
[Cmdlet(VerbsCommon.Get, "OrchTaskData", DefaultParameterSetName = "FromCommandLine")]
[OutputType(typeof(OrchTaskData))]
public class GetTaskDataCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "FromCommandLine")]
    [ArgumentCompleter(typeof(TaskIdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(DontShow = true, ValueFromPipeline = true, ParameterSetName = "FromPipeline")]
    public OrchTask? Task { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        if (Task is not null)
        {
            // The task's own folder, taken literally (a folder name may hold [ or ]).
            foreach (var (drive, folder) in SessionState.EnumFolders([WildcardPattern.Escape(Task.Path!)]).WithCancellation(cancelHandler.Token))
            {
                GetOne(drive, folder, Task.Id ?? 0);
            }
            return;
        }

        foreach (var (drive, folder) in SessionState.EnumFolders(EffectivePath(Path, LiteralPath)))
        {
            foreach (var taskId in Id!
                .WithProgressBar(this, $"Getting task data in {folder.GetPSPath()}", id => $"Task {id}")
                .WithCancellation(cancelHandler.Token))
            {
                GetOne(drive, folder, taskId);
            }
        }
    }

    private void GetOne(OrchDriveInfo drive, Folder folder, Int64 taskId)
    {
        string target = $"{folder.GetPSPath()} Task {taskId}";
        try
        {
            var data = drive.OrchAPISession.GetTaskData(folder.Id ?? 0, taskId);
            if (data is null) return;

            data.Path = folder.GetPSPath();
            data.Data = data.DataJson is { } json ? DfJsonTools.RecordToPSObject(json) : null;
            WriteObject(data);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "GetTaskDataError", ErrorCategory.InvalidOperation, taskId));
        }
    }
}
