using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using BoolCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.True_False>;

using Positional = UiPath.PowerShell.Positional.UserName;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchPmUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.PmUser))]
    public class UpdatePmUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmUserNameCompleter<Positional.UserName>))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Surname { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Email { get; set; }

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

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpUserName = UserName.ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                try
                {
                    var users = drive.GetPmUsers().Values;
                    var targetUsers = users.SelectByWildcards(u => u?.userName, wpUserName);
                    foreach (var user in targetUsers.OrderBy(u => u.userName))
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

                        dst.AssignString(Name, (u, v) => u.name = v);
                        dst.AssignString(Surname, (u, v) => u.surname = v);
                        dst.AssignString(Email, (u, v) => u.email = v);
                        dst.AssignString(Password, (u, v) => u.password = v);
                        dst.AssignBool(BypassBasicAuthRestriction, (u, v) => u.bypassBasicAuthRestriction = v);

                        // 更新があれば API を call する
                        if (src.name != dst.name ||
                            src.surname != dst.surname ||
                            src.email != dst.email ||
                            src.bypassBasicAuthRestriction != dst.bypassBasicAuthRestriction ||
                            !string.IsNullOrEmpty(Password))
                        {
                            if (ShouldProcess(user.GetPSPath(), "Update PmUser"))
                            {
                                try
                                {
                                    drive.OrchAPISession.PutPmUser(user.id!, dst);
                                    drive._dicPmUsers = null;
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
}
