using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchApiTrigger", SupportsShouldProcess = true)]
public class RemoveApiTriggerCommand : RemoveFolderEntityCmdletBase<HttpTrigger>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "ApiTrigger";
    protected override Func<HttpTrigger?, string?> GetName => t => t?.Name;
    protected override Func<HttpTrigger, string> GetPSPath => t => t.GetPSPath();
    protected override ErrorCategory ErrorCategory => ErrorCategory.NotSpecified;

    protected override IEnumerable<HttpTrigger> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.ApiTriggers.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, HttpTrigger trigger)
    {
        drive.OrchAPISession.RemoveHttpTrigger(folder.Id ?? 0, trigger.Id!);
        drive.ApiTriggers.ClearCache(folder);
    }
}
