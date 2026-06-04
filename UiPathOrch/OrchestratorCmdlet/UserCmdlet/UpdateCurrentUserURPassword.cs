using System.Management.Automation;
using System.Security;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchCurrentUserURPassword", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class UpdateUserURCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Mandatory = true, DontShow = true)]
    public SecureString? Password { get; set; }

    [Parameter(Mandatory = true, DontShow = true)]
    public SecureString? Confirmation { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var user = drive.CurrentUser.Get();
                string target = System.IO.Path.Combine(drive.NameColonSeparator, user!.UserName!);

                User postData = new();

                cancelHandler.Token.ThrowIfCancellationRequested();

                if (ShouldProcess(target, "Update CurrentUser"))
                {
                    try
                    {
                        string password = ConvertToUnsecureString(Password!);
                        string confirmation = ConvertToUnsecureString(Confirmation!);
                        if (password != confirmation)
                            throw new ArgumentException("Password and Confirmation do not match.");
                        drive.OrchAPISession.UpdateCurrentUserURPassword(user.Id ?? 0, password);
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(target, ex), "UpdateUserError", ErrorCategory.InvalidOperation, target);
                        WriteError(errorRecord);
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
