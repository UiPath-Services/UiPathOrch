using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchActionCatalog", SupportsShouldProcess = true)]
public class RemoveActionCatalogCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ActionCatalogNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var entities = drive.ActionCatalogs.Get(folder);

                foreach (var catalog in entities
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name))
                {
                    if (ShouldProcess(catalog.GetPSPath(), "Remove ActionCatalog"))
                    {
                        try
                        {
                            drive!.OrchAPISession.RemoveTaskCatalog(folder.Id ?? 0, catalog.Id ?? 0);
                            drive.ActionCatalogs.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(catalog.GetPSPath(), ex), "RemoveActionCatalogError", ErrorCategory.InvalidOperation, catalog));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetActionCatalogError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
