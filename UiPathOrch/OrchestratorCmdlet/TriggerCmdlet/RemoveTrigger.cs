using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTrigger", SupportsShouldProcess = true)]
public class RemoveTriggerCmdlet : RemoveFolderEntityCmdletBase<ProcessSchedule>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Trigger";
    protected override Func<ProcessSchedule?, string?> GetName => t => t?.Name;
    protected override Func<ProcessSchedule, string> GetPSPath => t => t.GetPSPath();

    protected override IEnumerable<ProcessSchedule> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.GetTriggers(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, ProcessSchedule trigger)
    {
        drive.OrchAPISession.DeleteProcessSchedule(folder.Id ?? 0, trigger.Id ?? 0);
        drive.Triggers.ClearCache(folder);
        drive.TriggersDetailed.ClearCache(folder);
    }
}
