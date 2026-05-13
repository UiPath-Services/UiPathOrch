using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Wraps POST /odata/Tasks/.../EditTaskMetadata. Updates Title / Priority / NoteText /
// task catalog association on existing tasks identified by their Int64 Id.
//
// Pipeline-friendly: accepts Task objects via ValueFromPipeline so common patterns work:
//   Get-OrchTask -Status Pending | Set-OrchTask -Priority High
//
// Catalog handling: -TaskCatalog accepts a catalog name and resolves it to TaskCatalogId
// via ActionCatalogs cache. Pass -UnsetTaskCatalog to disassociate any existing catalog.
[Cmdlet(VerbsCommon.Set, "OrchTask", SupportsShouldProcess = true)]
public class SetTaskCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TaskIdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(DontShow = true, ValueFromPipeline = true)]
    public OrchTask? Task { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Title { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Low", "Medium", "High", "Critical")]
    public string? Priority { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NoteText { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ActionCatalogNameCompleter))]
    public string? TaskCatalog { get; set; }

    [Parameter]
    public SwitchParameter UnsetTaskCatalog { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        if (Task is not null)
        {
            // Pipeline input: resolve folder context from the task's Path
            var dfs = SessionState.EnumFolders(new string[] { Task.Path! });
            foreach (var (drive, folder) in dfs)
            {
                EditOne(drive, folder, Task.Id ?? 0);
            }
            return;
        }

        var drivesFolders = SessionState.EnumFolders(Path);
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var taskId in Id!.WithCancellation(cancelHandler.Token))
            {
                EditOne(drive, folder, taskId);
            }
        }
    }

    private void EditOne(OrchDriveInfo drive, Folder folder, Int64 taskId)
    {
        string target = $"{folder.GetPSPath()} Task {taskId}";
        if (!ShouldProcess(target, "Set Task Metadata")) return;

        Int64? catalogId = null;
        if (!string.IsNullOrEmpty(TaskCatalog) && !UnsetTaskCatalog.IsPresent)
        {
            var catalogs = drive.ActionCatalogs.Get(folder);
            var match = catalogs.FirstOrDefault(c => string.Equals(c.Name, TaskCatalog, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Action catalog '{TaskCatalog}' not found in folder '{folder.GetPSPath()}'."),
                    "SetTaskCatalogNotFound", ErrorCategory.ObjectNotFound, TaskCatalog));
                return;
            }
            catalogId = match.Id;
        }

        var request = new EditTaskMetadataRequest
        {
            TaskId = taskId,
            Title = string.IsNullOrEmpty(Title) ? null : Title,
            Priority = string.IsNullOrEmpty(Priority) ? null : Priority,
            NoteText = string.IsNullOrEmpty(NoteText) ? null : NoteText,
            TaskCatalogId = catalogId,
            UnsetTaskCatalog = UnsetTaskCatalog.IsPresent ? true : null,
        };

        try
        {
            drive.OrchAPISession.EditTaskMetadata(folder.Id ?? 0, request);
            drive.Tasks.ClearCache(folder);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "SetTaskError", ErrorCategory.InvalidOperation, taskId));
        }
    }
}
