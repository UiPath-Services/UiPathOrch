using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

// この API は無効化されているため使えない
[Cmdlet(VerbsCommon.Get, "OrchPmUserLoginAttempt")]
//[OutputType(typeof(Entities.IdUser))]
class GetPmUserLoginAttemptCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();


        foreach (var drive in drives)
        {
            var users = drive.PmUsers.Get().FilterByWildcards(u => u?.email, wpEmail);
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
        //        if (entities is null) continue;

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
