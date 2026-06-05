using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// New-OrchActionCatalog -- wraps POST
// /odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog.
//
// External noun is "ActionCatalog" to align with the in-product UI label,
// while the wire entity type is TaskCatalog (legacy). Minimum-viable
// surface: Name + Description + Encrypted + Path. The wrapped server
// endpoint is POST-only; there is no Set-/Update- yet.
[Cmdlet(VerbsCommon.New, "OrchActionCatalog", SupportsShouldProcess = true)]
[OutputType(typeof(TaskCatalog))]
public class NewActionCatalogCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Encrypted { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                var newCatalog = new TaskCatalog
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newCatalog.AssignStringIfNotNullOrEmpty(Description, (c, v) => c.Description = v);
                newCatalog.AssignBoolIfNotNull(Encrypted, (c, v) => c.Encrypted = v);

                if (ShouldProcess(target, "New ActionCatalog"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateTaskCatalog(folder.Id!.Value, newCatalog);
                        drive.ActionCatalogs.ClearCache(folder);
                        if (created is not null)
                        {
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewActionCatalogError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
