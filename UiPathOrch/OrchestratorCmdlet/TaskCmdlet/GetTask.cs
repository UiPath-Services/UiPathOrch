using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTask")]
[OutputType(typeof(OrchTask))]
public class GetTaskCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TaskTitleCompleter))]
    [SupportsWildcards]
    public string[]? Title { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Unassigned", "Pending", "Completed")]
    public string[]? Status { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Low", "Medium", "High", "Critical")]
    public string[]? Priority { get; set; }

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
        var wpTitle = Title.ConvertToWildcardPatternList();
        var wpStatus = Status.ConvertToWildcardPatternList();
        var wpPriority = Priority.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Tasks.Get(df.folder));

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
                WriteError(new ErrorRecord(ex, "GetTaskError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
