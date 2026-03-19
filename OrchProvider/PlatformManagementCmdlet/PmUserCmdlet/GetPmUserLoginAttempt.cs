using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

// This API is disabled and cannot be used
//[Cmdlet(VerbsCommon.Get, "PmUserLoginAttempt")]
//[OutputType(typeof(Entities.IdUser))]
class GetPmUserLoginAttemptCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
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
