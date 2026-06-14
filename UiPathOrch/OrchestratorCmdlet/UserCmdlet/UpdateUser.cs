using System.Collections;
using UiPath.PowerShell.Positional;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchUser", SupportsShouldProcess = true)]
public class UpdateUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
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
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    //[SupportsWildcards] // Not worth the effort
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
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class RolesCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Extract the path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drives = SessionState.EnumOrchDrives(paramPath);

            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();
            var wpRoles = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Roles.Get());

            foreach (var result in results)
            {
                foreach (var role in result
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
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        // Split Roles specified in CSV by commas, honoring backtick-escaped commas in a role name --
        // matches Add-OrchUser and the rest of the Roles cmdlets (a raw Split corrupts a role whose
        // name contains a comma and ignores an explicit `, escape).
        var processedRoles = Roles.SplitValuesByUnescapedCommasPreservingEscapes();

        var wpRoles = processedRoles.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            // targetUsers already selects every user matching any -UserName pattern
            // in a single pass, so process the matched set once per drive. (A prior
            // per-name outer loop re-ran this whole block once per requested name —
            // redundant, since the match never depended on the individual name.)
            {
                var users = drive.Users.Get();
                // Match UserName OR EmailAddress, like Get-/Remove-OrchUser: an Azure AD B2B guest's
                // tenant UserName differs from its canonical EmailAddress, so -UserName user@contoso.com
                // must still resolve. (SelectByWildcardsAny keeps the empty -> empty "must specify a
                // name" semantics of the former SelectByWildcards.)
                var targetUsers = users.SelectByNamesAny([u => u?.UserName, u => u?.EmailAddress], UserName).OrderBy(u => u.UserName);

                foreach (var user in targetUsers.WithCancellation(cancelHandler.Token))
                {
                    string target = user.GetPSPath();
                    if (!string.IsNullOrEmpty(user.FullName))
                        target += $" ({user.FullName})";

                    var detailedUser = drive.UsersDetailed.Get(user.Id!.Value);
                    if (detailedUser is null)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "UpdateUserError", ErrorCategory.InvalidOperation, user));
                        continue;
                    }

                    // The Password returned from the server may contain "*****".
                    // Set it to null to prevent accidentally sending the masked value back.
                    if (detailedUser.UnattendedRobot is not null)
                    {
                        detailedUser.UnattendedRobot.Password = null;
                    }

                    var postingUser = OrchCollectionExtensions.DeepCopy(detailedUser);
                    bool dirty = false;

                    dirty |= postingUser.AssignStringIfNotNull(Name, detailedUser, u => u.Name, (u, v) => u.Name = v);
                    dirty |= postingUser.AssignStringIfNotNull(Surname, detailedUser, u => u.Surname, (u, v) => u.Surname = v);

                    dirty |= postingUser.AssignBoolIfNotFalse(IsExternalLicensed, detailedUser, u => u.IsExternalLicensed, (u, v) => u.IsExternalLicensed = v);
                    dirty |= postingUser.AssignBoolIfNotFalse(MayHaveUserSession, detailedUser, u => u.MayHaveUserSession, (u, v) => u.MayHaveUserSession = v);
                    dirty |= postingUser.AssignBoolIfNotFalse(MayHaveRobotSession, detailedUser, u => u.MayHaveRobotSession, (u, v) => u.MayHaveRobotSession = v);
                    dirty |= postingUser.AssignBoolIfNotFalse(MayHavePersonalWorkspace, detailedUser, u => u.MayHavePersonalWorkspace, (u, v) => u.MayHavePersonalWorkspace = v);
                    dirty |= postingUser.AssignBoolIfNotFalse(MayHaveUnattendedSession, detailedUser, u => u.MayHaveUnattendedSession, (u, v) => u.MayHaveUnattendedSession = v);
                    dirty |= postingUser.AssignBoolIfNotFalse(RestrictToPersonalWorkspace, detailedUser, u => u.RestrictToPersonalWorkspace, (u, v) => u.RestrictToPersonalWorkspace = v);

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
                                dirty = true;
                            }
                        }
                        else
                        {
                            postingUser.RolesList = [];
                            dirty = true;
                        }
                    }
                    #endregion

                    // Outer guard: only materialize an UnattendedRobot section in the payload
                    // when at least one UR_* field was actually provided. Inner Assign* helpers
                    // do per-field null/empty checks. UR_Password uses *IfNotNullOrEmpty because
                    // Get-OrchUser exports passwords as "" — accepting "" here would erase the
                    // stored password on every CSV roundtrip.
                    if (!string.IsNullOrEmpty(UR_UserName) ||
                        !string.IsNullOrEmpty(UR_CredentialStore) ||
                        !string.IsNullOrEmpty(UR_Password) ||
                        !string.IsNullOrEmpty(UR_CredentialExternalName) ||
                        !string.IsNullOrEmpty(UR_CredentialType) ||
                        !string.IsNullOrEmpty(UR_LimitConcurrentExecution))
                    {
                        postingUser.UnattendedRobot ??= new();
                        postingUser.UnattendedRobot.AssignStringIfNotNull(UR_UserName, (u, v) => u.UserName = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(UR_Password, (u, v) => u.Password = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNull(UR_CredentialExternalName, (u, v) => u.CredentialExternalName = v);
                        postingUser.UnattendedRobot.AssignStringIfNotNull(UR_CredentialType, (u, v) => u.CredentialType = v);
                        postingUser.UnattendedRobot.AssignBoolIfNotFalse(UR_LimitConcurrentExecution, u => u.LimitConcurrentExecution, (u, v) => u.LimitConcurrentExecution = v);

                        if (!string.IsNullOrEmpty(UR_CredentialStore))
                        {
                            var credentialStores = drive.CredentialStores.Get();
                            var targetCredentialStore = credentialStores.FirstOrDefault(cs => string.Compare(cs.Name, UR_CredentialStore, StringComparison.OrdinalIgnoreCase) == 0);

                            if (targetCredentialStore is null)
                            {
                                WriteWarning($"The specified credential store '{System.IO.Path.Combine(drive.NameColonSeparator, UR_CredentialStore)}' does not exist and will be ignored.");
                            }
                            else
                            {
                                postingUser.UnattendedRobot.CredentialStoreId = targetCredentialStore.Id;
                            }
                        }
                        dirty = true;
                    }

                    if (!string.IsNullOrEmpty(UpdatePolicyType) || !string.IsNullOrEmpty(UpdatePolicyVersion))
                    {
                        postingUser.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);
                        dirty = true;
                    }

                    void UpdateExecutionSettings(ExecutionSettings executionSettings)
                    {
                        executionSettings.AssignStringIfNotNull(ES_TracingLevel, (es, v) => es.TracingLevel = v);
                        executionSettings.AssignBoolIfNotNull(ES_StudioNotifyServer, (es, v) => es.StudioNotifyServer = v);
                        executionSettings.AssignBoolIfNotNull(ES_LoginToConsole, (es, v) => es.LoginToConsole = v);
                        executionSettings.AssignNumberIfNotNull(ES_ResolutionWidth, (es, v) => es.ResolutionWidth = v);
                        executionSettings.AssignNumberIfNotNull(ES_ResolutionHeight, (es, v) => es.ResolutionHeight = v);
                        executionSettings.AssignNumberIfNotNull(ES_ResolutionDepth, (es, v) => es.ResolutionDepth = v);
                        executionSettings.AssignBoolIfNotNull(ES_FontSmoothing, (es, v) => es.FontSmoothing = v);
                        executionSettings.AssignBoolIfNotNull(ES_AutoDownloadProcess, (es, v) => es.AutoDownloadProcess = v);
                    }

                    if (detailedUser.Type != "DirectoryExternalApplication" && (
                        ES_TracingLevel is not null ||
                        ES_StudioNotifyServer is not null ||
                        ES_LoginToConsole is not null ||
                        ES_ResolutionWidth is not null ||
                        ES_ResolutionHeight is not null ||
                        ES_ResolutionDepth is not null ||
                        ES_FontSmoothing is not null ||
                        ES_AutoDownloadProcess is not null))
                    {
                        if (postingUser.RobotProvision is not null)
                        {
                            postingUser.RobotProvision.ExecutionSettings ??= new();
                            UpdateExecutionSettings(postingUser.RobotProvision.ExecutionSettings);
                        }

                        if (postingUser.UnattendedRobot is not null)
                        {
                            postingUser.UnattendedRobot.ExecutionSettings ??= new();
                            UpdateExecutionSettings(postingUser.UnattendedRobot.ExecutionSettings);
                        }
                        dirty = true;
                    }

                    if (!dirty) continue;

                    if (ShouldProcess(target, "Update User"))
                    {
                        try
                        {
                            drive.OrchAPISession.PutUser(postingUser);
                            drive.Users.ClearCache();
                            drive.UsersDetailed.ClearCache();
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
