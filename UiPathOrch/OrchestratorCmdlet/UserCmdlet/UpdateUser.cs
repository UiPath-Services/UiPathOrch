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

        var wpUserName = UserName!.ConvertToWildcardPatternList();
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
                var targetUsers = users.SelectByWildcardsAny([u => u?.UserName, u => u?.EmailAddress], wpUserName).OrderBy(u => u.UserName);

                foreach (var user in targetUsers
                    .WithProgressBar(this, $"Updating users in {drive.NameColonSeparator}", u => u.UserName)
                    .WithCancellation(cancelHandler.Token))
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

                    // Resolve everything that needs an API round-trip (role names, credential-store
                    // name -> id) up front, then hand the resolved values to the pure change-detection
                    // step (ComputeUserUpdate). Keeping the decision API-free makes it unit-testable
                    // and guarantees a no-op request skips the PUT (and the audit-log churn it causes).
                    #region RolesList resolution
                    string[]? resolvedRoleNames = null;
                    if (Roles is not null)
                    {
                        if (Roles.Any(r => !string.IsNullOrEmpty(r)))
                        {
                            try
                            {
                                resolvedRoleNames = drive.Roles.Get()
                                    .Where(r => r.Type != "Folder")
                                    .SelectByWildcards(r => r?.Name, wpRoles)
                                    .Select(r => r.Name!)
                                    .Distinct()
                                    .Order()
                                    .ToArray();
                            }
                            catch
                            {
                                WriteError(new ErrorRecord(new OrchException(target, "Failed to retrieve Role. Ignored."), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                                resolvedRoleNames = null; // resolution failed: leave RolesList untouched
                            }
                        }
                        else
                        {
                            resolvedRoleNames = []; // -Roles '' clears every role
                        }
                    }
                    #endregion

                    // Resolve -UR_CredentialStore (name -> id). A supplied-but-unresolved store still
                    // materializes the UnattendedRobot section (so other UR_* fields apply) but leaves
                    // CredentialStoreId untouched, matching the previous warn-and-ignore behavior.
                    bool credentialStoreSpecified = !string.IsNullOrEmpty(UR_CredentialStore);
                    long? resolvedCredentialStoreId = null;
                    if (credentialStoreSpecified)
                    {
                        var targetCredentialStore = drive.CredentialStores.Get()
                            .FirstOrDefault(cs => string.Compare(cs.Name, UR_CredentialStore, StringComparison.OrdinalIgnoreCase) == 0);
                        if (targetCredentialStore is null)
                            WriteWarning($"The specified credential store '{System.IO.Path.Combine(drive.NameColonSeparator, UR_CredentialStore!)}' does not exist and will be ignored.");
                        else
                            resolvedCredentialStoreId = targetCredentialStore.Id;
                    }

                    var postingUser = OrchCollectionExtensions.DeepCopy(detailedUser);
                    var inputs = new UserUpdateInputs
                    {
                        Name = Name,
                        Surname = Surname,
                        IsExternalLicensed = IsExternalLicensed,
                        MayHaveUserSession = MayHaveUserSession,
                        MayHaveRobotSession = MayHaveRobotSession,
                        MayHaveUnattendedSession = MayHaveUnattendedSession,
                        MayHavePersonalWorkspace = MayHavePersonalWorkspace,
                        RestrictToPersonalWorkspace = RestrictToPersonalWorkspace,
                        UR_UserName = UR_UserName,
                        UR_Password = UR_Password,
                        UR_CredentialExternalName = UR_CredentialExternalName,
                        UR_CredentialType = UR_CredentialType,
                        UR_LimitConcurrentExecution = UR_LimitConcurrentExecution,
                        UR_CredentialStoreSpecified = credentialStoreSpecified,
                        UR_ResolvedCredentialStoreId = resolvedCredentialStoreId,
                        UpdatePolicyType = UpdatePolicyType,
                        UpdatePolicyVersion = UpdatePolicyVersion,
                        ES_TracingLevel = ES_TracingLevel,
                        ES_StudioNotifyServer = ES_StudioNotifyServer,
                        ES_LoginToConsole = ES_LoginToConsole,
                        ES_ResolutionWidth = ES_ResolutionWidth,
                        ES_ResolutionHeight = ES_ResolutionHeight,
                        ES_ResolutionDepth = ES_ResolutionDepth,
                        ES_FontSmoothing = ES_FontSmoothing,
                        ES_AutoDownloadProcess = ES_AutoDownloadProcess,
                        RolesSpecified = Roles is not null,
                        ResolvedRoleNames = resolvedRoleNames,
                    };

                    bool dirty = ComputeUserUpdate(postingUser, detailedUser, inputs);

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

    /// <summary>
    /// Pure inputs for <see cref="ComputeUserUpdate"/>. Everything that needs an API
    /// round-trip (role wildcard resolution, credential-store name -> id) is resolved by
    /// the cmdlet first and passed in here, so change detection is fully testable without a
    /// live Orchestrator.
    /// </summary>
    internal sealed class UserUpdateInputs
    {
        public string? Name { get; init; }
        public string? Surname { get; init; }
        public string? IsExternalLicensed { get; init; }
        public string? MayHaveUserSession { get; init; }
        public string? MayHaveRobotSession { get; init; }
        public string? MayHaveUnattendedSession { get; init; }
        public string? MayHavePersonalWorkspace { get; init; }
        public string? RestrictToPersonalWorkspace { get; init; }

        public string? UR_UserName { get; init; }
        public string? UR_Password { get; init; }
        public string? UR_CredentialExternalName { get; init; }
        public string? UR_CredentialType { get; init; }
        public string? UR_LimitConcurrentExecution { get; init; }
        /// <summary>True when -UR_CredentialStore was supplied (even if it did not resolve), so the UR block still materializes.</summary>
        public bool UR_CredentialStoreSpecified { get; init; }
        /// <summary>Resolved credential-store id, or null to leave CredentialStoreId untouched (unspecified or not found).</summary>
        public long? UR_ResolvedCredentialStoreId { get; init; }

        public string? UpdatePolicyType { get; init; }
        public string? UpdatePolicyVersion { get; init; }

        public string? ES_TracingLevel { get; init; }
        public string? ES_StudioNotifyServer { get; init; }
        public string? ES_LoginToConsole { get; init; }
        public string? ES_ResolutionWidth { get; init; }
        public string? ES_ResolutionHeight { get; init; }
        public string? ES_ResolutionDepth { get; init; }
        public string? ES_FontSmoothing { get; init; }
        public string? ES_AutoDownloadProcess { get; init; }

        /// <summary>True when -Roles was bound at all.</summary>
        public bool RolesSpecified { get; init; }
        /// <summary>
        /// Role names to set (already wildcard-resolved, distinct, ordered). Null means -Roles was
        /// not specified, or resolution failed and the roles block should be skipped. An empty array
        /// means "clear all roles" (-Roles '').
        /// </summary>
        public string[]? ResolvedRoleNames { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="postingUser"/> (a deep copy of
    /// <paramref name="detailedUser"/>) and returns whether anything actually changed. Only a
    /// real difference from the current value flips the result true, so the caller can skip the
    /// PUT — and the full-record audit entry it produces — when the request is a no-op. No API
    /// access, so this is unit-testable in isolation.
    /// </summary>
    internal static bool ComputeUserUpdate(User postingUser, User detailedUser, UserUpdateInputs input)
    {
        bool dirty = false;

        dirty |= postingUser.AssignStringIfNotNull(input.Name, detailedUser, u => u.Name, (u, v) => u.Name = v);
        dirty |= postingUser.AssignStringIfNotNull(input.Surname, detailedUser, u => u.Surname, (u, v) => u.Surname = v);

        dirty |= postingUser.AssignBoolIfNotFalse(input.IsExternalLicensed, detailedUser, u => u.IsExternalLicensed, (u, v) => u.IsExternalLicensed = v);
        dirty |= postingUser.AssignBoolIfNotFalse(input.MayHaveUserSession, detailedUser, u => u.MayHaveUserSession, (u, v) => u.MayHaveUserSession = v);
        dirty |= postingUser.AssignBoolIfNotFalse(input.MayHaveRobotSession, detailedUser, u => u.MayHaveRobotSession, (u, v) => u.MayHaveRobotSession = v);
        dirty |= postingUser.AssignBoolIfNotFalse(input.MayHavePersonalWorkspace, detailedUser, u => u.MayHavePersonalWorkspace, (u, v) => u.MayHavePersonalWorkspace = v);
        dirty |= postingUser.AssignBoolIfNotFalse(input.MayHaveUnattendedSession, detailedUser, u => u.MayHaveUnattendedSession, (u, v) => u.MayHaveUnattendedSession = v);
        dirty |= postingUser.AssignBoolIfNotFalse(input.RestrictToPersonalWorkspace, detailedUser, u => u.RestrictToPersonalWorkspace, (u, v) => u.RestrictToPersonalWorkspace = v);

        // Roles: replace the set only when the resolved set actually differs from the current one
        // (order-insensitive). A raw "specified => dirty" would re-PUT identical role lists.
        if (input.RolesSpecified && input.ResolvedRoleNames is not null)
        {
            if (!OrchStringExtensions.UnorderedEquals(detailedUser.RolesList, input.ResolvedRoleNames, s => s ?? string.Empty))
            {
                postingUser.RolesList = input.ResolvedRoleNames;
                dirty = true;
            }
        }

        // Unattended robot: materialize the section when any UR_* field was supplied, then diff each
        // field against the current value. UR_Password can't be diffed (the server returns it masked
        // / nulled), so a supplied password always counts as a change.
        bool urSpecified =
            !string.IsNullOrEmpty(input.UR_UserName) ||
            input.UR_CredentialStoreSpecified ||
            !string.IsNullOrEmpty(input.UR_Password) ||
            !string.IsNullOrEmpty(input.UR_CredentialExternalName) ||
            !string.IsNullOrEmpty(input.UR_CredentialType) ||
            !string.IsNullOrEmpty(input.UR_LimitConcurrentExecution);
        if (urSpecified)
        {
            var urSource = detailedUser.UnattendedRobot ?? new UnattendedRobot();
            postingUser.UnattendedRobot ??= new();
            var ur = postingUser.UnattendedRobot;

            dirty |= ur.AssignStringIfNotNull(input.UR_UserName, urSource, u => u.UserName, (u, v) => u.UserName = v);
            if (!string.IsNullOrEmpty(input.UR_Password)) { ur.Password = input.UR_Password; dirty = true; }
            dirty |= ur.AssignStringIfNotNull(input.UR_CredentialExternalName, urSource, u => u.CredentialExternalName, (u, v) => u.CredentialExternalName = v);
            dirty |= ur.AssignStringIfNotNull(input.UR_CredentialType, urSource, u => u.CredentialType, (u, v) => u.CredentialType = v);
            dirty |= ur.AssignBoolIfNotFalse(input.UR_LimitConcurrentExecution, urSource, u => u.LimitConcurrentExecution, (u, v) => u.LimitConcurrentExecution = v);
            if (input.UR_ResolvedCredentialStoreId is not null)
                dirty |= ur.AssignNumberIfNotNull(input.UR_ResolvedCredentialStoreId, urSource, u => u.CredentialStoreId, (u, v) => u.CredentialStoreId = v);
        }

        // Update policy: apply, then compare (Type, SpecificVersion) against the source policy.
        if (!string.IsNullOrEmpty(input.UpdatePolicyType) || !string.IsNullOrEmpty(input.UpdatePolicyVersion))
        {
            postingUser.AssignUpdatePolicy(input.UpdatePolicyType, input.UpdatePolicyVersion);
            if (!OrchStringExtensions.UpdatePolicyEquals(detailedUser.UpdatePolicy, postingUser.UpdatePolicy))
                dirty = true;
        }

        // Execution settings (the originally-reported defect): diff each ES_* against the current
        // value instead of flipping dirty whenever any ES_* is merely present.
        if (detailedUser.Type != "DirectoryExternalApplication" && (
            input.ES_TracingLevel is not null ||
            input.ES_StudioNotifyServer is not null ||
            input.ES_LoginToConsole is not null ||
            input.ES_ResolutionWidth is not null ||
            input.ES_ResolutionHeight is not null ||
            input.ES_ResolutionDepth is not null ||
            input.ES_FontSmoothing is not null ||
            input.ES_AutoDownloadProcess is not null))
        {
            bool ApplyExecutionSettings(ExecutionSettings target, ExecutionSettings source)
            {
                bool changed = false;
                changed |= target.AssignStringIfNotNull(input.ES_TracingLevel, source, es => es.TracingLevel, (es, v) => es.TracingLevel = v);
                changed |= target.AssignBoolIfNotNull(input.ES_StudioNotifyServer, source, es => es.StudioNotifyServer, (es, v) => es.StudioNotifyServer = v);
                changed |= target.AssignBoolIfNotNull(input.ES_LoginToConsole, source, es => es.LoginToConsole, (es, v) => es.LoginToConsole = v);
                changed |= target.AssignNumberIfNotNull(input.ES_ResolutionWidth, source, es => es.ResolutionWidth, (es, v) => es.ResolutionWidth = v);
                changed |= target.AssignNumberIfNotNull(input.ES_ResolutionHeight, source, es => es.ResolutionHeight, (es, v) => es.ResolutionHeight = v);
                changed |= target.AssignNumberIfNotNull(input.ES_ResolutionDepth, source, es => es.ResolutionDepth, (es, v) => es.ResolutionDepth = v);
                changed |= target.AssignBoolIfNotNull(input.ES_FontSmoothing, source, es => es.FontSmoothing, (es, v) => es.FontSmoothing = v);
                changed |= target.AssignBoolIfNotNull(input.ES_AutoDownloadProcess, source, es => es.AutoDownloadProcess, (es, v) => es.AutoDownloadProcess = v);
                return changed;
            }

            // postingUser is a DeepCopy of detailedUser, so the detailedUser section's ExecutionSettings
            // holds the unmodified current values to diff against. A null source (section had no
            // ExecutionSettings) compares as all-null, so a specified value is correctly a change.
            if (postingUser.RobotProvision is not null)
            {
                postingUser.RobotProvision.ExecutionSettings ??= new();
                dirty |= ApplyExecutionSettings(
                    postingUser.RobotProvision.ExecutionSettings,
                    detailedUser.RobotProvision?.ExecutionSettings ?? new());
            }

            if (postingUser.UnattendedRobot is not null)
            {
                postingUser.UnattendedRobot.ExecutionSettings ??= new();
                dirty |= ApplyExecutionSettings(
                    postingUser.UnattendedRobot.ExecutionSettings,
                    detailedUser.UnattendedRobot?.ExecutionSettings ?? new());
            }
        }

        return dirty;
    }
}
