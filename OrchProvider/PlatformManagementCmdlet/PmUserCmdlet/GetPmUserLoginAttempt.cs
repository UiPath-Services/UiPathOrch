using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands
{
    // この API は無効化されているため使えない
    [Cmdlet(VerbsCommon.Get, "OrchPmUserLoginAttempt")]
    //[OutputType(typeof(Entities.IdUser))]
    class GetPmUserLoginAttemptCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmUserEmailCompleter<Positional.Email>))]
        [SupportsWildcards]
        public string[]? Email { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Email>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpEmail = Email.ConvertToWildcardPatternList();


            foreach (var drive in drives)
            {
                var users = drive.GetPmUsers().Values.FilterByWildcards(u => u?.email, wpEmail);
                foreach (var user in users)
                {
                    drive.OrchAPISession.GetPmUserLoginAttempts(user.id!);
                }
            }

            //using var results = OrchThreadPool.RunForEach(drives,
            //    drive => drive.NameColonSeparator,
            //    drive => drive,
            //    drive => drive.GetIdentityUsers().Values);

            //using var cancelHandler = new ConsoleCancelHandler();
            //foreach (var result in results)
            //{
            //    try
            //    {
            //        var entities = result.GetResult(cancelHandler.Token);
            //        if (entities == null) continue;

            //        WriteObject(entities
            //            .FilterByWildcards(u => u.userName!, wpUserName)
            //            .OrderBy(u => u.userName),
            //            true);
            //    }
            //    catch (OrchException ex)
            //    {
            //        WriteError(new ErrorRecord(ex, "GetIdUserError", ErrorCategory.InvalidOperation, ex.Target));
            //    }
            //}
        }
    }
}
