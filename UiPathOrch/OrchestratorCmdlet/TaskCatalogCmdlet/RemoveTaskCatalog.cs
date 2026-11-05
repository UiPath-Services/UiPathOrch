using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTaskCatalog", SupportsShouldProcess = true)]
public class RemoveTaskCatalogCmdlet : RemoveFolderEntityCmdletBase<TaskCatalog>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TaskCatalogNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "TaskCatalog";
    protected override Func<TaskCatalog?, string?> GetName => c => c?.Name;
    protected override Func<TaskCatalog, string> GetPSPath => c => c.GetPSPath();

    protected override IEnumerable<TaskCatalog> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TaskCatalogs.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TaskCatalog catalog)
    {
        drive.OrchAPISession.RemoveTaskCatalog(folder.Id ?? 0, catalog.Id ?? 0);
        drive.TaskCatalogs.ClearCache(folder);
    }
}
