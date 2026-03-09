using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.UserName_Roles;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchRoleFromUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class RemoveRoleFromUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter<TPositional>))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter<TPositional>))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("TenantRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class RolesCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);
            var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.GetUsers());

            foreach (var result in results)
            {
                foreach (var user in result
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.FullName, wpFullName)
                    .OrderBy(u => u.UserName))
                {
                    if (user.UserRoles is not null)
                    {
                        foreach (var role in user.UserRoles
                            .Where(r => wp.IsMatch(r.RoleName))
                            .ExcludeByWildcards(r => r?.RoleName, wpRoles)
                            .OrderBy(r => r.RoleName))
                        {
                            string tiphelp = TipHelp2(user);
                            var ret = new CompletionResult(PathTools.EscapePSText(role.RoleName), role.RoleName, CompletionResultType.ParameterValue, tiphelp);
                            yield return ret;
                        }
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        // The first element may have been input from CSV, so split it by commas
        var processedRoles = Roles.Split1stValueByUnescapedCommas();

        var wpRoles = processedRoles.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            List<Entities.User> users;
            try
            {
                users = drive.GetUsers()
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.Type, wpType)
                    .OrderBy(u => u.UserName)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var user in users)
            {
                if (user.RolesList is null || user.RolesList.Length == 0) continue;

                var newRoles = user.RolesList?.ExcludeByWildcards(r => r, wpRoles).ToArray();
                if (user.RolesList!.Length == newRoles!.Length) continue;

                var rolesToRemove = user.RolesList.Except(newRoles);
                var strRolesToRemove = string.Join(", ", rolesToRemove!.Select(r => "'" + r + "'"));

                cancelHandler.Token.ThrowIfCancellationRequested();

                if (ShouldProcess($"{strRolesToRemove} from {user.GetPSPath()}", "Remove Roles from User"))
                {
                    var postingUser = drive.GetUser(user);
                    if (postingUser is null) continue;

                    postingUser.LoginProviders = null;
                    postingUser.CreatorUserId = null;
                    postingUser.UserRoles = null;
                    postingUser.RolesList = newRoles;

                    try
                    {
                        drive.OrchAPISession.PutUser(postingUser);
                        drive._dicUsers = null;
                        drive._dicUsersDetailed = null;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "UpdateUserError", ErrorCategory.InvalidOperation, user));
                    }
                }
            }
        }
    }
}
