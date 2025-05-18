using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class UpdateUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("FirstName")]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("LastName")]
    public string? Surname { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("TenantRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? IsExternalLicensed { get; set; }

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
    [ArgumentCompleter(typeof(StaticTextsCompleter<UserUpdatePolicyItems>))]
    public string? UpdatePolicyType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UpdatePolicyVersionCompleter))]
    public string? UpdatePolicyVersion { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? UR_UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
    //[SupportsWildcards] // 面倒なのでいっか
    public string? UR_CredentialStore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? UR_Password { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? UR_CredentialExternalName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<UserCredentialTypeItems>))]
    public string? UR_CredentialType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? UR_LimitConcurrentExecution { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<ExecutionSettingsTraceLevelItems>))]
    public string? ES_TracingLevel { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ES_StudioNotifyServer { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ES_LoginToConsole { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ES_ResolutionWidth { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ES_ResolutionHeight { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ES_ResolutionDepth { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ES_FontSmoothing { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ES_AutoDownloadProcess { get; set; }

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
            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drives = OrchDriveInfo.EnumOrchDrives(paramPath);

            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);
            var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drives, drive => drive.Roles.Get());

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                var drive = result.Source;

                foreach (var role in result.Result
                    .Where(r => r.Type != "Folder")
                    .Where(r => wp.IsMatch(r.Name))
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
                    if (detailedUser is null)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "UpdateUserError", ErrorCategory.InvalidOperation, user));
                        continue;
                    }

                    // あとで正しく dirty かどうかを確認できるように、RolesList をソートしておく
                    detailedUser.RolesList = detailedUser.RolesList?.Order().ToArray();

                    // サーバーから返る Password には "*****" が入っていたりする。
                    // 間違ってパスワードを "*****" で更新したりしないように、null を入れておく。
                    // 等値性を正しく判断できるようにするためにも有用だ。
                    if (detailedUser.UnattendedRobot is not null)
                    {
                        detailedUser.UnattendedRobot.Password = null;
                    }

                    var postingUser = OrchCollectionExtensions.DeepCopy(detailedUser);

                    if (postingUser.UnattendedRobot is not null)
                    {
                        postingUser.UnattendedRobot.Password = null;
                    }

                    //postingUser.AssignStringIfNotNull(FullName,                   (u, v) => u.FullName = v);

                    postingUser.AssignStringIfNotNullOrEmpty(Name,    (u, v) => u.Name = v);
                    postingUser.AssignStringIfNotNullOrEmpty(Surname, (u, v) => u.Surname = v);

                    postingUser.AssignBoolIfNotFalse(IsExternalLicensed,          u => u.IsExternalLicensed,          (u, v) => u.IsExternalLicensed = v);
                    postingUser.AssignBoolIfNotFalse(MayHaveUserSession,          u => u.MayHaveUserSession,          (u, v) => u.MayHaveUserSession = v);
                    postingUser.AssignBoolIfNotFalse(MayHaveRobotSession,         u => u.MayHaveRobotSession,         (u, v) => u.MayHaveRobotSession = v);
                    postingUser.AssignBoolIfNotFalse(MayHavePersonalWorkspace,    u => u.MayHavePersonalWorkspace,    (u, v) => u.MayHavePersonalWorkspace = v);
                    postingUser.AssignBoolIfNotFalse(MayHaveUnattendedSession,    u => u.MayHaveUnattendedSession,    (u, v) => u.MayHaveUnattendedSession = v);
                    postingUser.AssignBoolIfNotFalse(RestrictToPersonalWorkspace, u => u.RestrictToPersonalWorkspace, (u, v) => u.RestrictToPersonalWorkspace = v);

                    #region RolesList
                    if (Roles is not null)
                    {
                        if (Roles.Any(r => !string.IsNullOrEmpty(r)))
                        {
                            List<Entities.Role> roles = null;
                            try
                            {
                                roles = drive.Roles.Get()
                                    .Where(r => r.Type != "Folder")
                                    .SelectByWildcards(r => r?.Name, wpRoles)
                                    .ToList();
                            }
                            catch
                            {
                                WriteError(new ErrorRecord(new OrchException(target, "Failed to retrieve Role. Ignored."), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                            }
                            if (roles is not null)
                            {
                                postingUser.RolesList = roles.Select(r => r.Name!)
                                    .Distinct()
                                    .Order()
                                    .ToArray();
                            }
                        }
                        else
                        {
                            postingUser.RolesList = [];
                        }
                    }
                    #endregion

                    if (!string.IsNullOrEmpty(UR_UserName) ||
                        !string.IsNullOrEmpty(UR_CredentialStore) ||
                        !string.IsNullOrEmpty(UR_Password) ||
                        !string.IsNullOrEmpty(UR_CredentialExternalName) ||
                        !string.IsNullOrEmpty(UR_CredentialType) ||
                        !string.IsNullOrEmpty(UR_LimitConcurrentExecution))
                    {
                        postingUser.UnattendedRobot ??= new();
                        postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(UR_UserName,               (u, v) => u.UserName = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(UR_Password,               (u, v) => u.Password = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(UR_CredentialExternalName, (u, v) => u.CredentialExternalName = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(UR_CredentialType,         (u, v) => u.CredentialType = v);
                        postingUser.UnattendedRobot.AssignBoolIfNotFalse(UR_LimitConcurrentExecution, u => u.LimitConcurrentExecution, (u, v) => u.LimitConcurrentExecution = v);

                        if (!string.IsNullOrEmpty(UR_CredentialStore))
                        {
                            //var wpCredentialStore = new WildcardPattern(UR_CredentialStore, WildcardOptions.IgnoreCase);
                            var credentialStores = drive.CredentialStores.Get();
                            var targetCredentialStore = credentialStores.FirstOrDefault(cs => string.Compare(cs.Name, UR_CredentialStore, true) == 0);

                            if (targetCredentialStore is null)
                            {
                                WriteWarning($"The specified credential store '{System.IO.Path.Combine(drive.NameColonSeparator, UR_CredentialStore)}' does not exist and will be ignored.");
                            }
                            else
                            {
                                postingUser.UnattendedRobot.CredentialStoreId = targetCredentialStore.Id;
                            }
                        }
                    }

                    postingUser.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);

                    void UpdateExecutionSettings(ExecutionSettings executionSettings)
                    {
                        executionSettings.AssignStringIfNotNullOrEmpty(
                            ES_TracingLevel, (es, v) =>
                            es.TracingLevel = v);

                        executionSettings.AssignBoolIfNotNull(
                            ES_StudioNotifyServer, (es, v) =>
                            es.StudioNotifyServer = v);

                        executionSettings.AssignBoolIfNotNull(
                            ES_LoginToConsole, (es, v) =>
                            es.LoginToConsole = v);

                        executionSettings.AssignNumberIfNotNull(
                            ES_ResolutionWidth, (es, v) =>
                            es.ResolutionWidth = v);

                        executionSettings.AssignNumberIfNotNull(
                            ES_ResolutionHeight, (es, v) =>
                            es.ResolutionHeight = v);

                        executionSettings.AssignNumberIfNotNull(
                            ES_ResolutionDepth, (es, v) =>
                            es.ResolutionDepth = v);

                        executionSettings.AssignBoolIfNotNull(
                            ES_FontSmoothing, (es, v) =>
                            es.FontSmoothing = v);

                        executionSettings.AssignBoolIfNotNull(
                            ES_AutoDownloadProcess, (es, v) =>
                            es.AutoDownloadProcess = v);
                    }

                    if (postingUser.Type != "DirectoryExternalApplication" && (
                        ES_TracingLevel is not null ||
                        ES_StudioNotifyServer is not null ||
                        ES_LoginToConsole is not null ||
                        ES_ResolutionWidth is not null ||
                        ES_ResolutionHeight is not null ||
                        ES_ResolutionDepth is not null ||
                        ES_FontSmoothing is not null ||
                        ES_AutoDownloadProcess is not null))
                    {
                        // UnattendedRobot.ExecutionSetting と合わせて、
                        // RobotProvision.ExecutionSettings も同じように更新するように修正した。
                        postingUser.RobotProvision ??= new();
                        postingUser.RobotProvision.ExecutionSettings ??= new();
                        UpdateExecutionSettings(postingUser.RobotProvision.ExecutionSettings);

                        postingUser.UnattendedRobot ??= new();
                        postingUser.UnattendedRobot.ExecutionSettings ??= new();
                        UpdateExecutionSettings(postingUser.UnattendedRobot.ExecutionSettings);

                        //if (postingUser.type == 3) // robot の場合
                        //{

                        //}
                    }

                    // こういうエラー処理は、API 側に任せた方が良いか。。
                    //if (postingUser.UpdatePolicy is not null)
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

                    // 次のプロパティは、web interface からは POST されていないようだ。
                    // null を入れておくべきか？
                    //postingUser.DirectoryIdentifier = null;
                    //postingUser.Domain = null;
                    //postingUser.Name = null;
                    //postingUser.Surname = null;
                    //postingUser.LastModificationTime = null;
                    //postingUser.LastModifierUserId = null;
                    //postingUser.LicenseType = null;
                    //postingUser.OrganizationUnits = null;
                    //postingUser.LoginProviders = null;

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