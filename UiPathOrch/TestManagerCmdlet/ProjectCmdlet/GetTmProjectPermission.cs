using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmProjectPermission")]
[OutputType(typeof(Entities.TmProjectPermission))]
public class GetTmProjectPermissionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(Path, Recurse.IsPresent);

        //foreach (var driveProject in drivesProjects)
        //{
        //    var (drive, project) = driveProject;
        //    WriteObject(drive.GetTmProjectPermission(project), true);
        //}

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.TmProjectPermissions.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmProjectPermissionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
