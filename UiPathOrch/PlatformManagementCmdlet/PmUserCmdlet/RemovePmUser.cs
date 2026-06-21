using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmUser", SupportsShouldProcess = true)]
public class RemovePmUserCmdlet : OrchestratorPSCmdlet
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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpEmail = Email.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            User? currentUser = null;
            try
            {
                // Ideally we should check if OR.User.Read scope is available before calling, but...
                currentUser = drive.CurrentUser.Get();
            }
            catch
            {
                // Swallow: resolving the current user is best-effort (used only to
                // skip self-removal below). If OR.User.Read scope is missing the
                // Get() throws — proceed with currentUser == null rather than abort.
            }

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

                foreach (var user in targetUsers.OrderBy(u => u.email)
                    .WithProgressBar(this, $"Removing PmUsers in {drive.NameColonSeparator}", u => u.email)
                    .WithCancellation(cancelHandler.Token))
                {
                    if (user.id == currentUser?.Key) continue;

                    string target = user.GetPSPath(drive.NameColonSeparator);
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
