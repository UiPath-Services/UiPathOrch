using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Wraps GET /odata/Tasks/.../GetTasksAcrossFolders. Returns tasks from every folder the
// caller has Tasks.View on, in a single API call — useful for tenant-wide reporting and
// dashboards. Distinct from Get-OrchTask -Recurse, which iterates per folder; this one
// hits the dedicated server-side aggregation endpoint.
[Cmdlet(VerbsCommon.Get, "OrchTaskAcrossFolder")]
[OutputType(typeof(OrchTask))]
public class GetTaskAcrossFolderCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Title { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Unassigned", "Pending", "Completed")]
    public string[]? Status { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Low", "Medium", "High", "Critical")]
    public string[]? Priority { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpTitle = Title.ConvertToWildcardPatternList();
        var wpStatus = Status.ConvertToWildcardPatternList();
        var wpPriority = Priority.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive =>
            {
                var tasks = drive.OrchAPISession.GetTasksAcrossFolders().ToList();

                // Resolve each task's actual folder path so Path reflects where the task lives,
                // not the aggregation drive root. Falls back to drive root for orphans.
                var folderById = drive.GetFolders()
                    .Where(f => f.Id is not null)
                    .ToDictionary(f => f.Id!.Value);
                foreach (var task in tasks)
                {
                    task.Path = (task.OrganizationUnitId is long fid && folderById.TryGetValue(fid, out var folder))
                        ? folder.GetPSPath()
                        : drive.NameColonSeparator;
                }
                return tasks;
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var tasks = result.GetResult(cancelHandler.Token);
                if (tasks is null) continue;

                WriteObject(tasks
                    .FilterByWildcards(t => t?.Title, wpTitle)
                    .FilterByWildcards(t => t?.Status, wpStatus)
                    .FilterByWildcards(t => t?.Priority, wpPriority)
                    .OrderByDescending(t => t.CreationTime),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTaskAcrossFolderError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
