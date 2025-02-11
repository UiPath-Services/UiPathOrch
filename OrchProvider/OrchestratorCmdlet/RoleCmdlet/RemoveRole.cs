using System.Data;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchRole", SupportsShouldProcess = true)]
public class RemoveRoleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(RoleNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var roles = drive.Roles.Get();
                foreach (var role in roles
                    .Where(r => !r.IsStatic.GetValueOrDefault())
                    .FilterByWildcards(role => role?.Name, wpName)
                    .OrderBy(r => r.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = role.GetPSPath();
                    if (ShouldProcess(target, "Remove Role"))
                    {
                        try
                        {
                            drive.OrchAPISession.DeleteRole(role.Id ?? 0);
                            drive.Roles.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveRoleError", ErrorCategory.InvalidOperation, role));
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
