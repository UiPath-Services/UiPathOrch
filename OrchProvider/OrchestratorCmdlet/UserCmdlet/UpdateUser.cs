using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using Positional = UiPath.PowerShell.Positional.UserName;
using BoolCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.True_False>;
using System.Data;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.User))]
    public class UpdateUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TenantUserNameCompleter<UserName>))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("TenantRoles")]
        [ArgumentCompleter(typeof(RolesCompleter))]
        public string[]? Roles { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? MayHaveUserSession { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? MayHaveRobotSession { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? MayHaveUnattendedSession { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? MayHavePersonalWorkspace { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? RestrictToPersonalWorkspace { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? UR_UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(CredentialStoreNameCompleter<UserName>))]
        //[SupportsWildcards] // 面倒なのでいっか
        public string? UR_CredentialStore { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? UR_Password { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<UserCredentialTypeItems>))]
        public string? UR_CredentialType { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? UR_LimitConcurrentExecution { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<UserUpdatePolicyItems>))]
        public string? UpdatePolicyType { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UpdatePolicyVersionCompleter))]
        public string? UpdatePolicyVersion { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<UserName>))]
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
                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drives = OrchDriveInfo.EnumOrchDrives(paramPath);

                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.UserName.Parameters);
                var wpRoles = CreateWPListFromParameter(commandAst, parameterName, Positional.UserName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetRoles());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var role in entities!
                        .Where(r => r.Type != "Folder")
                        .ExcludeByWildcards(r => r?.Name, wpRoles)
                        .OrderBy(r => r.Name))
                    {
                        string tiphelp = TipHelp(role);
                        var ret = new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
                        yield return ret;
                    }
                }
            }
        }

        private class UpdatePolicyVersionCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drives = OrchDriveInfo.EnumOrchDrives(paramPath);

                foreach (var drive in drives)
                {
                    var versions = drive.GetAvailableVersions();

                    foreach (var version in versions ?? [])
                    {
                        string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, version);
                        yield return new CompletionResult(PathTools.EscapePSText(version), version, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private static string? ReplaceLastNumberWithAsterisk(string? input)
        {
            if (input == null) return null;

            // ピリオドで分割
            string[] parts = input.Split('.');

            if (parts.Length >= 3)
            {
                parts[2] = "*";
                return string.Join(".", parts.Take(3));
            }
            return input;
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            // CSV に指定された Roles はカンマで区切る
            Roles = Roles?
                 .SelectMany(name => name.Split(',', StringSplitOptions.RemoveEmptyEntries))
                 .Select(name => name.Trim())
                 .ToArray();

            var wpUserName = UserName!.ConvertToWildcardPatternList();
            var wpRoles = Roles.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                foreach (var identityName in UserName!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var users = drive.GetUsers();
                    var targetUsers = users.SelectByWildcards(u => u?.UserName, wpUserName).OrderBy(u => u.UserName);

                    foreach (var user in targetUsers)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string target = user.GetPSPath();
                        if (!string.IsNullOrEmpty(user.FullName))
                            target += $" ({user.FullName})";

                        var detailedUser = drive.GetUser(user);
                        if (detailedUser == null)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "UpdateUserError", ErrorCategory.InvalidOperation, user));
                            continue;
                        }

                        // サーバーから返る Password には "*****" が入っていたりする。
                        // 間違ってパスワードを "*****" で更新したりしないように、null を入れておく。
                        // 等値性を正しく判断できるようにするためにも有用だ。
                        if (detailedUser.UnattendedRobot != null)
                        {
                            detailedUser.UnattendedRobot.Password = null;
                        }

                        var postingUser = OrchCollectionExtensions.DeepCopy(detailedUser);

                        if (postingUser.UnattendedRobot != null)
                        {
                            postingUser.UnattendedRobot.Password = null;
                        }
                        postingUser.AssignBool2(MayHaveUserSession,          u => u.MayHaveUserSession,          (u, v) => u.MayHaveUserSession = v);
                        postingUser.AssignBool2(MayHaveRobotSession,         u => u.MayHaveRobotSession,         (u, v) => u.MayHaveRobotSession = v);
                        postingUser.AssignBool2(MayHavePersonalWorkspace,    u => u.MayHavePersonalWorkspace,    (u, v) => u.MayHavePersonalWorkspace = v);
                        postingUser.AssignBool2(MayHaveUnattendedSession,    u => u.MayHaveUnattendedSession,    (u, v) => u.MayHaveUnattendedSession = v);
                        postingUser.AssignBool2(RestrictToPersonalWorkspace, u => u.RestrictToPersonalWorkspace, (u, v) => u.RestrictToPersonalWorkspace = v);

                        #region RolesList
                        if (Roles != null && Roles.Any(r => !string.IsNullOrEmpty(r)))
                        {
                            List<Entities.Role> roles = null;
                            try
                            {
                                roles = drive.GetRoles()
                                    .Where(r => r.Type != "Folder")
                                    .SelectByWildcards(r => r?.Name, wpRoles)
                                    .ToList();
                            }
                            catch
                            {
                                WriteError(new ErrorRecord(new OrchException(target, "Failed to retrieve Role. Ignored."), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                            }
                            if (roles != null)
                            {
                                postingUser.RolesList = roles.Select(r => r.Name!).Distinct().ToArray();
                            }
                        }
                        #endregion

                        if (!string.IsNullOrEmpty(UR_UserName) ||
                            !string.IsNullOrEmpty(UR_CredentialStore) ||
                            !string.IsNullOrEmpty(UR_Password) ||
                            !string.IsNullOrEmpty(UR_CredentialType) ||
                            !string.IsNullOrEmpty(UR_LimitConcurrentExecution))
                        {
                            postingUser.UnattendedRobot ??= new();
                            postingUser.UnattendedRobot.AssignString(UR_UserName,       (u, v) => u.UserName = v);
                            postingUser.UnattendedRobot.AssignString(UR_Password,       (u, v) => u.Password = v);
                            postingUser.UnattendedRobot.AssignString(UR_CredentialType, (u, v) => u.CredentialType = v);
                            postingUser.UnattendedRobot.AssignBool2(UR_LimitConcurrentExecution, u => u.LimitConcurrentExecution, (u, v) => u.LimitConcurrentExecution = v);

                            if (!string.IsNullOrEmpty(UR_CredentialStore))
                            {
                                //var wpCredentialStore = new WildcardPattern(UR_CredentialStore, WildcardOptions.IgnoreCase);
                                var credentialStores = drive.GetCredentialStores();
                                var targetCredentialStore = credentialStores.FirstOrDefault(cs => string.Compare(cs.Name, UR_CredentialStore, true) == 0);

                                if (targetCredentialStore == null)
                                {
                                    WriteWarning($"The specified credential store '{System.IO.Path.Combine(drive.NameColonSeparator, UR_CredentialStore)}' does not exist and will be ignored.");
                                }
                                else
                                {
                                    postingUser.UnattendedRobot.CredentialStoreId = targetCredentialStore.Id;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(UpdatePolicyType) ||
                            !string.IsNullOrEmpty(UpdatePolicyVersion))
                        {
                            postingUser.UpdatePolicy ??= new();
                            postingUser.UpdatePolicy.AssignString(UpdatePolicyType, (u, v) => u.Type = v);
                            if (UpdatePolicyType == "None" || UpdatePolicyType == "LatestVersion")
                            {
                                postingUser.UpdatePolicy.SpecificVersion = null;
                            }
                            else
                            {
                                if (UpdatePolicyType == "LatestPatch")
                                {
                                    UpdatePolicyVersion = ReplaceLastNumberWithAsterisk(UpdatePolicyVersion);
                                }
                                postingUser.UpdatePolicy.AssignString(UpdatePolicyVersion, (u, v) => u.SpecificVersion = v);
                            }
                        }

                        // こういうエラー処理は、API 側に任せた方が良いか。。
                        //if (postingUser.UpdatePolicy != null)
                        //{
                        //    if (string.IsNullOrEmpty(postingUser.UpdatePolicy.SpecificVersion) &&
                        //        (postingUser.UpdatePolicy.Type == "LatestPatch" || postingUser.Type == "SpecificVersion"))
                        //    {
                        //        WriteError(new ErrorRecord(new OrchException(target, $"When UpdatePolicyType is \"{postingUser.UpdatePolicy.Type}\", you must specify UpdatePolicyVersion."), "UpdateUserError", ErrorCategory.InvalidOperation, user));
                        //        continue;
                        //    }
                        //}


                        //if (user.type == 3) // robot の場合
                        //{
                        //    postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                        //    postingUser.MayHaveUnattendedSession = true;
                        //    postingUser.UnattendedRobot = new()
                        //    {
                        //        CredentialType = "NoCredential",
                        //        LimitConcurrentExecution = false,
                        //    };
                        //}
                        //else if (user.type == 4) // application の場合
                        //{
                        //    postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                        //}

                        if (postingUser.Equals(detailedUser)) continue;
                        
                        if (ShouldProcess(target, $"Update User"))
                        {
                            try
                            {
                                drive.OrchAPISession.PutUser(postingUser);
                                drive._dicUsers = null;
                                drive._dicUsersDetailed = null;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateUserError", ErrorCategory.InvalidOperation, drive));
                            }
                        }
                    }
                }
            }
        }
    }
}