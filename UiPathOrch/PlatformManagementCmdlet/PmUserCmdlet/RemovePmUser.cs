using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmUser", SupportsShouldProcess = true)]
public class RemovePmUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            User? currentUser = null;
            try
            {
                // Ideally we should check if OR.User.Read scope is available before calling, but...
                currentUser = drive.GetCurrentUser();
            }
            catch { }

            try
            {
                var users = drive.PmUsers.Get();

                var partitionGlobalId = drive!.GetPartitionGlobalId();

                var targetUsers = users.FilterByWildcards(u => u?.email, wpEmail);

                if (NoMatchWarning.IsPresent && !targetUsers.Any())
                {
                    // This implementation is a bit rough, but it works properly during CSV import, so it should be sufficient.
                    // A proper implementation would need to process the UserName array one element at a time from the beginning.
                    WriteWarning($"No match found for UserName '{Email![0]}'.");
                    continue;
                }

                foreach (var user in targetUsers.OrderBy(u => u.email))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (user.id == currentUser?.Key) continue;

                    string target = user.GetPSPath();
                    if (ShouldProcess(target, "Remove PmUser"))
                    {
                        try
                        {
                            if (drive.OrchAPISession.PmApiDeprecated)
                            {
                                try
                                {
                                    // If we're calling this, it would be better to use a bulk call, but this is fine for now.
                                    drive.OrchAPISession.RemovePmUser(partitionGlobalId!, user.id!);
                                }
                                catch
                                {
                                    drive.OrchAPISession.RemovePmUserDeprecated(user.id!);
                                    // If RemovePmUserDeprecated() succeeded, the API is not yet deprecated
                                    drive.OrchAPISession.PmApiDeprecated = false;
                                }
                            }
                            else
                            {
                                drive.OrchAPISession.RemovePmUserDeprecated(user.id!);
                            }
                            drive.PmUsers.ClearCache();
                            drive.PmGroups.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmUserError", ErrorCategory.InvalidOperation, user));
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
