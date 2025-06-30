using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmProjectSetting")]
[OutputType(typeof(Entities.TmProjectSettings))]
public class GetTmProjectSettingCommand : OrchestratorPSCmdlet
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
        //    WriteObject(drive.GetTmProjectSettings(project.id!));
        //}

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.GetTmProjectSettings(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmServerInfoError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
