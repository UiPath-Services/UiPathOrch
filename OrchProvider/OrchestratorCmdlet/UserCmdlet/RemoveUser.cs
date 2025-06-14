using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class RemoveUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter<TPositional>))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter<TPositional>))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter<TPositional>))]
    public string[]? Type { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName_FullName>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        if (UserName is not null && UserName.All(u => string.IsNullOrEmpty(u))) UserName = null;
        if (FullName is not null && FullName.All(f => string.IsNullOrEmpty(f))) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var users = drive.GetUsers();
                var targetUsers = users
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.Type, wpType)
                    .ToList();

                if (NoMatchWarning.IsPresent && targetUsers.Count == 0)
                {
                    WriteWarning($"No match found for UserName '{UserName?[0]}' and FullName '{FullName?[0]}'.");
                    continue;
                }

                foreach (var user in targetUsers.OrderBy(u => u.UserName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = user.GetPSPath();
                    if (!string.IsNullOrEmpty(user.FullName))
                    {
                        target += $" ({user.FullName})";
                    }

                    if (ShouldProcess(target, "Remove User"))
                    {
                        try
                        {
                            drive.OrchAPISession.DeleteUser(user.Id ?? 0);
                            drive._dicUsers = null;
                            drive._dicUsersDetailed = null;
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemoveUserError", ErrorCategory.InvalidOperation, user);
                            WriteError(errorRecord);
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
                var errorRecord = new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive);
                WriteError(errorRecord);
            }
        }
    }
}
