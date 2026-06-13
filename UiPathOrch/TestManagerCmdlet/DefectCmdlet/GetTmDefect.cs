using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// For some reason the API returns forbidden. Keeping this as private (internal) for now.
[Cmdlet(VerbsCommon.Get, "TmDefect")]
[OutputType(typeof(Entities.TmDefect))]
class GetTmDefectCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);

        foreach (var driveProject in drivesProjects)
        {
            var (drive, project) = driveProject;
            try
            {
                WriteObject(drive.OrchAPISession.GetTmDefects(project.id!), true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmDefectError", ErrorCategory.InvalidOperation, project));
            }
        }

        //using var results = OrchThreadPool.RunForEach(drivesProjects,
        //    dp => dp.project.GetPSPath(),
        //    dp => dp.project,
        //    dp => dp.drive.GetTmRequirements(dp.project));

        //using var cancelHandler = new ConsoleCancelHandler();
        //foreach (var result in results)
        //{
        //    try
        //    {
        //        var entity = result.GetResult(cancelHandler.Token);
        //        if (entity is null) continue;

        //        WriteObject(entity
        //            .FilterByNames(e => e.name!, Name)
        //            .OrderBy(e => e.objKey!, ObjKeyComparer.Instance),
        //            true);
        //    }
        //    catch (OrchException ex)
        //    {
        //        WriteError(new ErrorRecord(ex, "GetTmDefectError", ErrorCategory.InvalidOperation, ex.Target));
        //    }
        //}
    }
}
