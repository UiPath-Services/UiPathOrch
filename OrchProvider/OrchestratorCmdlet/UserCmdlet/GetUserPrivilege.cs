using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUserPrivilege")]
[OutputType(typeof(Entities.UserPrivilege))]
public class GetUserPrivilegeCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpUserName = UserName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var users = drive.GetUsers();
                var targetUsers = users
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .Where(u => u.Type == "DirectoryUser" || u.Type == "DirectoryGroup")
                    .OrderBy(u => u.UserName)
                    .ToList();

                using var results = OrchThreadPool.RunForEach(targetUsers
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .OrderBy(u => u.UserName),
                    user => user.GetPSPath(),
                    user => user,
                    user => drive.UserPrivileges.Get(user));

                //int index = 0;
                //string msg = "Get users... ";
                //using var reporter = new ProgressReporter(this, 1, targetUsers.Count, msg, msg);
                foreach (var result in results)
                {
                    try
                    {
                        var entities = result.GetResult(cancelHandler.Token);
                        if (entities is null) continue;

                        //reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {detailedUser.GetPSPath()}");

                        WriteObject(entities);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetUserPrivilegesError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
