using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName_Roles;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchRoleFromUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.User))]
    public class RemoveRoleFromUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FullNameCompleter))]
        public string[]? FullName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("TenantRoles")]
        [ArgumentCompleter(typeof(RolesCompleter))]
        public string[]? Roles { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName_Roles>))]
        public string[]? Path { get; set; }

        private class UserNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.UserName_Roles.Parameters, wordToComplete);

                // パラメータで選択された FullName のみ対象とする
                var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.UserName_Roles.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.UserName))
                        .ExcludeByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.UserName))
                    {
                        string tiphelp = TipHelp2(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private class FullNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択された UserName のみ対象とする
                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.UserName_Roles.Parameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpFullName = CreateWPListFromParameter(commandAst, "FullName", Positional.UserName_Roles.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.FullName))
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .ExcludeByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.FullName))
                    {
                        string tiphelp = TipHelp2(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.FullName), e.FullName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private class RolesCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.UserName_Roles.Parameters);
                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.UserName_Roles.Parameters);
                var wpRoles = CreateWPListFromParameter(commandAst, parameterName, Positional.UserName_Roles.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.UserName))
                    {
                        if (e.UserRoles!= null)
                        {
                            foreach (var role in e.UserRoles
                                .Where(r => wp.IsMatch(r.RoleName))
                                .ExcludeByWildcards(r => r?.RoleName, wpRoles)
                                .OrderBy(r => r.RoleName))
                            {
                                string tiphelp = TipHelp2(e);
                                var ret = new CompletionResult(PathTools.EscapePSText(role.RoleName), role.RoleName, CompletionResultType.ParameterValue, tiphelp);
                                yield return ret;
                            }
                        }
                    }
                }
            }
        }

        //private class TenantRolesCompleter : OrchArgumentCompleter
        //{
        //    public override IEnumerable<CompletionResult> CompleteArgument(
        //        string commandName,
        //        string parameterName,
        //        string wordToComplete,
        //        CommandAst commandAst,
        //        IDictionary fakeBoundParameters)
        //    {
        //        var drives = ResolveDrives(fakeBoundParameters);

        //        var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.Parameters);
        //        var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.Parameters);
        //        var wpRoles = CreateWPListFromParameter(commandAst, "Roles", Positional.Parameters, wordToComplete);

        //        var wp = CreateWPFromWordToComplete(wordToComplete);

        //        var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

        //        foreach (var result in results)
        //        {
        //            if (!result.TryGetValue(out var entities)) continue;

        //            foreach (var e in entities!
        //                .FilterByWildcards(u => u?.UserName, wpUserName)
        //                .FilterByWildcards(u => u?.FullName, wpFullName)
        //                .OrderBy(u => u.UserName))
        //            {
        //                if (e.UserRoles != null)
        //                {
        //                    foreach (var role in e.UserRoles
        //                        .Where(r => wp.IsMatch(r.RoleName))
        //                        .ExcludeByWildcards(r => r?.RoleName, wpRoles)
        //                        .OrderBy(r => r.RoleName))
        //                    {
        //                        string tiphelp = TipHelp(e);
        //                        var ret = new CompletionResult(PathTools.EscapePSText(role.RoleName), role.RoleName, CompletionResultType.ParameterValue, tiphelp);
        //                        yield return ret;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
            if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

            if (UserName == null && FullName == null)
            {
                WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, this));
                return;
            }

            var wpUserName = UserName.ConvertToWildcardPatternList();
            var wpFullName = FullName.ConvertToWildcardPatternList();

            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            Roles = Roles.Split1stValueByUnescapedCommas()?.ToArray();

            var wpRoles = Roles.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                List<Entities.User> users;
                try
                {
                    users = drive.GetUsers()
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
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
                    if (user.RolesList == null || user.RolesList.Length == 0) continue;

                    var newRoles = user.RolesList?.ExcludeByWildcards(r => r, wpRoles).ToArray();
                    if (user.RolesList!.Length == newRoles!.Length) continue;

                    var rolesToRemove = user.RolesList.Except(newRoles);
                    var strRolesToRemove = string.Join(", ", rolesToRemove!.Select(r => "'" + r + "'"));

                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess($"{strRolesToRemove} from {user.GetPSPath()}", "Remove Roles from User"))
                    {
                        var postingUser = drive.GetUser(user);
                        if (postingUser == null) continue;

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
}
