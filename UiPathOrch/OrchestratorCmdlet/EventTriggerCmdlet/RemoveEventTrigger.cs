using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchEventTrigger", SupportsShouldProcess = true)]
public class RemoveEventTriggerCmdlet : RemoveFolderEntityCmdletBase<ApiTrigger>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EventTriggerNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "EventTrigger";
    protected override Func<ApiTrigger?, string?> GetName => t => t?.Name;
    protected override Func<ApiTrigger, string> GetPSPath => t => t.GetPSPath();
    protected override ErrorCategory ErrorCategory => ErrorCategory.NotSpecified;

    protected override IEnumerable<ApiTrigger> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.EventTriggers.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, ApiTrigger trigger)
    {
        drive.OrchAPISession.RemoveEventTrigger(folder.Id ?? 0, trigger.Id!);
        drive.EventTriggers.ClearCache(folder);
    }
}
