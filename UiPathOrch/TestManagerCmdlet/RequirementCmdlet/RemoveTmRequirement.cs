using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmRequirement", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmRequirementCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var driveProject in drivesProjects.WithCancellation(cancelHandler.Token))
        {
            var (drive, project) = driveProject;

            try
            {
                var requirements = drive.TmRequirements.Get(project);

                foreach (var requirement in requirements
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance).WithCancellation(cancelHandler.Token))
                {
                    var target = requirement.GetPSPath();
                    if (ShouldProcess(target, "Remove TmRequirement"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmRequirements(project.id!, requirement.id!);
                            drive.TmRequirements.ClearCache();
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
}
