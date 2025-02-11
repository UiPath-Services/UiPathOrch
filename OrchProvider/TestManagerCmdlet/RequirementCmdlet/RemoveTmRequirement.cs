using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmRequirement", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.TmRequirement))]
public class RemoveTmRequirementCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var driveProject in drivesProjects)
        {
            var (drive, project) = driveProject;

            try
            {
                var requirements = drive.GetTmRequirements(project);

                foreach (var requirement in requirements
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var target = requirement.GetPSPath();
                    if (ShouldProcess(target, "Remove TmRequirement"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmRequirements(project.id!, requirement.id!);
                            drive._dicTmRequirements?.TryRemove(project.id!, out var _);
                            drive._dicTmRequirementExceptions.ClearCache(project.id);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveTmRequirementError", ErrorCategory.InvalidOperation, requirement));
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
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmRequirementError", ErrorCategory.InvalidOperation, project));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    //foreach (var driveProject in drivesProjects)
    //    //{
    //    //    var (drive, project) = driveProject;
    //    //    try
    //    //    {
    //    //        WriteObject(drive.GetTmRequirements(project), true);
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        throw new OrchException(project.GetPSPath(), ex);
    //    //    }
    //    //}

    //    using var results = OrchThreadPool.RunForEach(drivesProjects,
    //        dp => dp.project.GetPSPath(),
    //        dp => dp.project,
    //        dp => dp.drive.GetTmRequirements(dp.project));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var (drive, project) = result.Source;

    //            foreach (var requirement in entities
    //                .FilterByWildcards(e => e.name!, wpName)
    //                .OrderBy(e => e.objKey!, ObjKeyComparer.Instance))
    //            {
    //                if (ShouldProcess("Remove Requirement", requirement.GetPSPath()))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveTmRequirements(project.id!, requirement.id!);
    //                        drive._dicTmRequirements?.TryRemove(project.id!, out var _);
    //                        drive._dicTmRequirementExceptions.TryRemove(project.id!);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(requirement.GetPSPath(), ex), "RemoveTmRequirementError", ErrorCategory.InvalidOperation, requirement));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetTmRequirementError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
