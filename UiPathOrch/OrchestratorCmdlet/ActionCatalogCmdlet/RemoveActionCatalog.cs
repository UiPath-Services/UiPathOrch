using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchActionCatalog", SupportsShouldProcess = true)]
public class RemoveActionCatalogCmdlet : RemoveFolderEntityCmdletBase<TaskCatalog>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ActionCatalogNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "ActionCatalog";
    protected override Func<TaskCatalog?, string?> GetName => c => c?.Name;
    protected override Func<TaskCatalog, string> GetPSPath => c => c.GetPSPath();

    protected override IEnumerable<TaskCatalog> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.ActionCatalogs.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TaskCatalog catalog)
    {
        drive.OrchAPISession.RemoveTaskCatalog(folder.Id ?? 0, catalog.Id ?? 0);
        drive.ActionCatalogs.ClearCache(folder);
    }
}
