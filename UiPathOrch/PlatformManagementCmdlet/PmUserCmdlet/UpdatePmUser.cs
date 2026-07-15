using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "PmUser", SupportsShouldProcess = true)]
public class UpdatePmUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? DisplayName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Surname { get; set; }

    // Sets (or clears) the user's email. -Email / -UserName above selects which user(s)
    // to update; -NewEmail changes the selected user's email. An existing user's email
    // IS changeable via PUT /api/User/{id} -- verified live on on-prem 21.10.4 / 22.10.1
    // / 25.10.2, where adding an email to a userName-only account and changing an
    // existing email both succeed. (An earlier note here wrongly assumed the email could
    // not be changed, which is why this was left out.)
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewEmail { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Password { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? BypassBasicAuthRestriction { get; set; }

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
            try
            {
                var users = drive.PmUsers.Get();
                // -UserName is an alias of -Email; match a pattern against EITHER the
                // userName or the email so a userName that differs from the email still
                // resolves. SelectByWildcardsAny keeps the empty -> empty (no -Email given
                // => no-op) semantics of the former SelectByWildcards, unlike the Any
                // filter used by the read cmdlets.
                var targetUsers = users
                    .SelectByWildcardsAny([u => u?.userName, u => u?.email], wpEmail)
                    .OrderBy(u => u.email)
                    .ToList();

                // -NewEmail sets one address; applying it to several users at once would
                // collide on the destination, so require the selection to resolve to a
                // single user.
                if (!string.IsNullOrEmpty(NewEmail) && targetUsers.Count > 1)
                {
                    WriteError(new ErrorRecord(
                        new PSArgumentException($"-NewEmail sets a single email address, but {targetUsers.Count} users matched on '{drive.NameColonSeparator}'. Narrow -Email/-UserName to one user."),
                        "UpdatePmUserNewEmailMultiMatch", ErrorCategory.InvalidArgument, drive));
                    continue;
                }

                foreach (var user in targetUsers
                    .WithProgressBar(this, $"Updating PmUsers in {drive.NameColonSeparator}", u => u.email)
                    .WithCancellation(cancelHandler.Token))
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
                    dst.AssignStringIfNotNull(NewEmail, (u, v) => u.email = v);
                    dst.AssignStringIfNotNullOrEmpty(Password, (u, v) => u.password = v); // Password cannot be retrieved via API, so leave as-is
                    dst.AssignBoolIfNotNull(BypassBasicAuthRestriction, (u, v) => u.bypassBasicAuthRestriction = v);

                    // Call the API if there are any updates
                    if (src.name != dst.name ||
                        src.displayName != dst.displayName ||
                        src.surname != dst.surname ||
                        src.email != dst.email ||
                        src.bypassBasicAuthRestriction != dst.bypassBasicAuthRestriction ||
                        !string.IsNullOrEmpty(Password))
                    {
                        if (ShouldProcess(user.GetPSPath(drive.NameColonSeparator), "Update PmUser"))
                        {
                            try
                            {
                                drive.OrchAPISession.PutPmUser(user.id!, dst);
                                drive.PmUsers.ClearCache();
                                drive.PmGroups.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(user.GetPSPath(drive.NameColonSeparator), ex), "UpdatePmUserError", ErrorCategory.InvalidOperation, user));
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
