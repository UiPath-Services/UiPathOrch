using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "PmUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.PmUser))]
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
    public string? Surname { get; set; }

    // 既存ユーザーの Email は、API で変更できないようだ。
    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public string? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Password { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? BypassBasicAuthRestriction { get; set; }

    // TODO: グループの所属も指定できた方が便利なのか？
    // groupIDsToAdd と groupIDsToRemove を payload に含めないといけないが、ちと面倒。
    // ほかの cmdlet で可能な操作だから、一旦良いことにする。

    // 以下は payload に含める必要がない

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //public string? displayName { get; set; }

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
        var drives = OrchDriveInfo.EnumPmDrives(Path);
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
                        surname = user.surname,
                        email = user.email,
                        bypassBasicAuthRestriction = user.bypassBasicAuthRestriction
                    };

                    UiPath.PowerShell.Entities.UpdateUserCommand dst = new()
                    {
                        name = user.name,
                        surname = user.surname,
                        email = user.email,
                        bypassBasicAuthRestriction = user.bypassBasicAuthRestriction
                    };

                    dst.AssignStringIfNotNullOrEmpty(Name, (u, v) => u.name = v);
                    dst.AssignStringIfNotNullOrEmpty(Surname, (u, v) => u.surname = v);
                    //dst.AssignStringIfNotNullOrEmpty(Email, (u, v) => u.email = v);
                    dst.AssignStringIfNotNullOrEmpty(Password, (u, v) => u.password = v);
                    dst.AssignBoolIfNotNull(BypassBasicAuthRestriction, (u, v) => u.bypassBasicAuthRestriction = v);

                    // 更新があれば API を call する
                    if (src.name != dst.name ||
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
                                drive._dicPmGroups = null;
                                drive._dicPmGroups_Exception.ClearCache();
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
