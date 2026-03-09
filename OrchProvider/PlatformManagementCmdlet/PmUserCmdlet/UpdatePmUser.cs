using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "PmUser", SupportsShouldProcess = true)]
public class UpdatePmUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? DisplayName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Surname { get; set; }

    // It seems that the Email of an existing user cannot be changed via the API.
    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public string? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Password { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? BypassBasicAuthRestriction { get; set; }

    // TODO: Would it be useful to also allow specifying group membership?
    // We would need to include groupIDsToAdd and groupIDsToRemove in the payload, which is a bit tedious.
    // Since this can be done with other cmdlets, leaving it for now.

    // The following do not need to be included in the payload

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public bool? isActive { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public bool? invitationAccepted { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public Dictionary<string, string>? extensionUserAttributesToAddOrUpdate { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public string[]? extensionUserAttributesToRemove { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            try
            {
                var users = drive.PmUsers.Get();
                var targetUsers = users.SelectByWildcards(u => u?.email, wpEmail);
                foreach (var user in targetUsers.OrderBy(u => u.email))
                {
                    UiPath.PowerShell.Entities.UpdateUserCommand src = new()
                    {
                        name = user.name,
                        displayName = user.displayName,
                        surname = user.surname,
                        email = user.email,
                        bypassBasicAuthRestriction = user.bypassBasicAuthRestriction
                    };

                    UiPath.PowerShell.Entities.UpdateUserCommand dst = new()
                    {
                        name = user.name,
                        displayName = user.displayName,
                        surname = user.surname,
                        email = user.email,
                        bypassBasicAuthRestriction = user.bypassBasicAuthRestriction
                    };

                    dst.AssignStringIfNotNull(Name, (u, v) => u.name = v);
                    dst.AssignStringIfNotNull(DisplayName, (u, v) => u.displayName = v);
                    dst.AssignStringIfNotNull(Surname, (u, v) => u.surname = v);
                    //dst.AssignStringIfNotNull(Email, (u, v) => u.email = v);
                    dst.AssignStringIfNotNullOrEmpty(Password, (u, v) => u.password = v); // Password cannot be retrieved via API, so leave as-is
                    dst.AssignBoolIfNotNull(BypassBasicAuthRestriction, (u, v) => u.bypassBasicAuthRestriction = v);

                    // Call the API if there are any updates
                    if (src.name != dst.name ||
                        src.displayName != dst.displayName ||
                        src.surname != dst.surname ||
                        //src.email != dst.email ||
                        src.bypassBasicAuthRestriction != dst.bypassBasicAuthRestriction ||
                        !string.IsNullOrEmpty(Password))
                    {
                        if (ShouldProcess(user.GetPSPath(), "Update PmUser"))
                        {
                            try
                            {
                                drive.OrchAPISession.PutPmUser(user.id!, dst);
                                drive.PmUsers.ClearCache();
                                drive.PmGroups.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "UpdatePmUserError", ErrorCategory.InvalidOperation, user));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
