using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Commands.CsvHelper;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class AddUserCmdlet : OrchestratorPSCmdlet
{
    // Needed for case-insensitive comparison of UserName
    // Should this be moved to OrchComparer.cs?
    internal class CsvLineComparer : IEqualityComparer<(OrchDriveInfo drive, int type, string userName)>
    {
        public bool Equals((OrchDriveInfo drive, int type, string userName) x, (OrchDriveInfo drive, int type, string userName) y)
        {
            return EqualityComparer<OrchDriveInfo>.Default.Equals(x.drive, y.drive) &&
                   x.type == y.type &&
                   string.Equals(x.userName, y.userName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((OrchDriveInfo drive, int type, string userName) obj)
        {
            return HashCode.Combine(
                EqualityComparer<OrchDriveInfo>.Default.GetHashCode(obj.drive),
                obj.type,
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.userName)
            );
        }
    }

    Dictionary<(OrchDriveInfo drive, int type, string userName), CsvLine>? _csvLines = null;

    private class CsvLine : CsvLineBase
    {
        public bool? IsExternalLicensed { get; set; }
        public bool? MayHaveUserSession { get; set; }
        public bool? MayHaveRobotSession { get; set; }
        public bool? MayHaveUnattendedSession { get; set; }
        public bool? MayHavePersonalWorkspace { get; set; }
        public bool? RestrictToPersonalWorkspace { get; set; }

        public string? UpdatePolicyType { get; set; }
        public string? UpdatePolicyVersion { get; set; }

        public string? UR_UserName { get; set; }
        public string? UR_CredentialStore { get; set; }
        public string? UR_Password { get; set; }
        public string? UR_CredentialExternalName { get; set; }
        public string? UR_CredentialType { get; set; }
        public bool? UR_LimitConcurrentExecution { get; set; }

        public string? ES_TracingLevel { get; set; }
        public bool? ES_StudioNotifyServer { get; set; }
        public bool? ES_LoginToConsole { get; set; }
        public int? ES_ResolutionWidth { get; set; }
        public int? ES_ResolutionHeight { get; set; }
        public int? ES_ResolutionDepth { get; set; }
        public bool? ES_FontSmoothing { get; set; }
        public bool? ES_AutoDownloadProcess { get; set; }

        public HashSet<string>? Roles { get; set; }

        public CsvLine(AddUserCmdlet cmdlet)
        {
            IsExternalLicensed = cmdlet.IsExternalLicensed.ToNullableBool() ?? false;
            MayHaveRobotSession = cmdlet.MayHaveRobotSession.ToNullableBool() ?? false;
            MayHaveUnattendedSession = cmdlet.MayHaveUnattendedSession.ToNullableBool() ?? false;
            MayHavePersonalWorkspace = cmdlet.MayHavePersonalWorkspace.ToNullableBool() ?? false;
            MayHaveUserSession = cmdlet.MayHaveUserSession.ToNullableBool() ?? false;
            RestrictToPersonalWorkspace = cmdlet.RestrictToPersonalWorkspace.ToNullableBool() ?? false;

            UpdatePolicyType = cmdlet.UpdatePolicyType;
            UpdatePolicyVersion = cmdlet.UpdatePolicyVersion;

            UR_UserName = cmdlet.UR_UserName;
            UR_CredentialStore = cmdlet.UR_CredentialStore;
            UR_Password = cmdlet.UR_Password;
            UR_CredentialExternalName = cmdlet.UR_CredentialExternalName;
            UR_CredentialType = cmdlet.UR_CredentialType;
            UR_LimitConcurrentExecution = cmdlet.UR_LimitConcurrentExecution.ToNullableBool();

            ES_TracingLevel = cmdlet.ES_TracingLevel;
            ES_StudioNotifyServer = cmdlet.ES_StudioNotifyServer.ToNullableBool();
            ES_LoginToConsole = cmdlet.ES_LoginToConsole.ToNullableBool();
            ES_ResolutionWidth = cmdlet.ES_ResolutionWidth;
            ES_ResolutionHeight = cmdlet.ES_ResolutionHeight;
            ES_ResolutionDepth = cmdlet.ES_ResolutionDepth;
            ES_FontSmoothing = cmdlet.ES_FontSmoothing.ToNullableBool();
            ES_AutoDownloadProcess = cmdlet.ES_AutoDownloadProcess.ToNullableBool();

            Roles = new HashSet<string>(cmdlet.Roles?.Where(r => !string.IsNullOrEmpty(r)) ?? []);
        }

        public void Update(AddUserCmdlet cmdl, OrchDriveInfo drive, string identityName)
        {
            AssignBoolValue(cmdl, drive.Name, identityName,
                this.IsExternalLicensed,
                cmdl.IsExternalLicensed, v =>
                this.IsExternalLicensed = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.MayHaveUserSession,
                cmdl.MayHaveUserSession, v =>
                this.MayHaveUserSession = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.MayHaveRobotSession,
                cmdl.MayHaveRobotSession, v =>
                this.MayHaveRobotSession = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.MayHaveUnattendedSession,
                cmdl.MayHaveUnattendedSession, v =>
                this.MayHaveUnattendedSession = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.MayHavePersonalWorkspace,
                cmdl.MayHavePersonalWorkspace, v =>
                this.MayHavePersonalWorkspace = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.RestrictToPersonalWorkspace,
                cmdl.RestrictToPersonalWorkspace, v =>
                this.RestrictToPersonalWorkspace = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UpdatePolicyType,
                cmdl.UpdatePolicyType, v =>
                this.UpdatePolicyType = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UpdatePolicyVersion,
                cmdl.UpdatePolicyVersion, v =>
                this.UpdatePolicyVersion = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UR_UserName,
                cmdl.UR_UserName, v =>
                this.UR_UserName = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UR_CredentialStore,
                cmdl.UR_CredentialStore, v =>
                this.UR_CredentialStore = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UR_Password,
                cmdl.UR_Password, v =>
                this.UR_Password = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UR_CredentialExternalName,
                cmdl.UR_CredentialExternalName, v =>
                this.UR_CredentialExternalName = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.UR_CredentialType,
                cmdl.UR_CredentialType, v =>
                this.UR_CredentialType = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.UR_LimitConcurrentExecution,
                cmdl.UR_LimitConcurrentExecution, v =>
                this.UR_LimitConcurrentExecution = v);

            AssignStringValue(cmdl, drive.Name, identityName,
                this.ES_TracingLevel,
                cmdl.ES_TracingLevel, v =>
                this.ES_TracingLevel = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.ES_StudioNotifyServer,
                cmdl.ES_StudioNotifyServer, v =>
                this.ES_StudioNotifyServer = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.ES_LoginToConsole,
                cmdl.ES_LoginToConsole, v =>
                this.ES_LoginToConsole = v);

            AssignIntValue(cmdl, drive.Name, identityName,
                this.ES_ResolutionWidth,
                cmdl.ES_ResolutionWidth, v =>
                this.ES_ResolutionWidth = v);

            AssignIntValue(cmdl, drive.Name, identityName,
                this.ES_ResolutionHeight,
                cmdl.ES_ResolutionHeight, v =>
                this.ES_ResolutionHeight = v);

            AssignIntValue(cmdl, drive.Name, identityName,
                this.ES_ResolutionDepth,
                cmdl.ES_ResolutionDepth, v =>
                this.ES_ResolutionDepth = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.ES_FontSmoothing,
                cmdl.ES_FontSmoothing, v =>
                this.ES_FontSmoothing = v);

            AssignBoolValue(cmdl, drive.Name, identityName,
                this.ES_AutoDownloadProcess,
                cmdl.ES_AutoDownloadProcess, v =>
                this.ES_AutoDownloadProcess = v);

            this.Roles ??= [];
            this.Roles.UnionWith(cmdl.Roles?.Where(r => !string.IsNullOrEmpty(r)) ?? []);
        }
    }

    // Key: DirectoryObject.type;  Value: Type within the entity
    private static readonly Dictionary<int, string> Types = new() {
        { 0, "DirectoryUser" },
        { 1, "DirectoryGroup" },
        { 3, "DirectoryRobot" },
        { 4, "DirectoryExternalApplication" }
    };

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
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
    [ArgumentCompleter(typeof(StaticTextsCompleter<UserCredentialTypeItems>))]
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
    public int? ES_ResolutionWidth { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ES_ResolutionHeight { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ES_ResolutionDepth { get; set; }

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

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude user names that have already been selected via parameters
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);
            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var specifiedTypes = Types.SelectByWildcards(t => t.Value, wpType).Select(t => t.Key);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var existingTenantUser = drive.Users.Get();
                var users = drive.SearchDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    .FilterByStructValues(u => u.type ?? -1, specifiedTypes)
                    .ExcludeByClassValues(u => u?.identityName?.ToLower(), existingTenantUser.Select(u => u.UserName?.ToLower()))
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .OrderBy(e => e.identityName))
                {
                    bFound = true;
                    string tiphelp = TipHelp(user);
                    yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{wordToComplete}')""");
            }
        }
    }

    private class RolesCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();
            var wpRoles = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Roles.Get());

            foreach (var result in results)
            {
                foreach (var role in result
                    .Where(r => wp.IsMatch(r.Name))
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

    protected override void ProcessRecord()
    {
        _csvLines ??= new Dictionary<(OrchDriveInfo drive, int type, string userName), CsvLine>(new CsvLineComparer());

        // The first element may have been input from CSV, so split it by commas
        Roles = Roles.SplitValuesByUnescapedCommasPreservingEscapes()?.ToArray();

        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpType = Type.ConvertToWildcardPatternList();
        var specifiedTypes = Types.SelectByWildcards(t => t.Value, wpType).Select(t => t.Key);

        foreach (var drive in drives)
        {
            foreach (var specifiedType in specifiedTypes)
            {
                foreach (var userName in UserName!)
                {
                    if (!_csvLines.TryGetValue((drive, specifiedType, userName), out var line))
                    {
                        _csvLines[(drive, specifiedType, userName)] = new CsvLine(this);
                    }
                    else
                    {
                        line.Update(this, drive, userName);
                    }
                }
            }
        }
    }

    private IEnumerable<string> ExcludeFolderRoles(OrchDriveInfo drive, IEnumerable<string>? roles, HashSet<(OrchDriveInfo drive, string roleName)> warnedNoMatchingRoles)
    {
        if (roles is null) return [];
        try
        {
            var existingRoles = drive.Roles.Get();

            foreach (var specifiedRole in roles)
            {
                #region Warn if a non-existent role is specified
                var wpRole = new WildcardPattern(specifiedRole, WildcardOptions.IgnoreCase);
                if (!existingRoles.Any(r => wpRole.IsMatch(r.Name)))
                {
                    warnedNoMatchingRoles ??= [];
                    if (warnedNoMatchingRoles.Add((drive, specifiedRole.ToLower())))
                    {
                        WriteError(new ErrorRecord(
                            new OrchException(drive.NameColonSeparator, $"No matching role found for '{specifiedRole}'. Ignored."),
                            "NoMatchedRoleError",
                            ErrorCategory.ObjectNotFound,
                            drive));
                    }
                }
                #endregion

                #region Warn if a folder role is specified
                foreach (var role in existingRoles
                    .Where(r => r.Type == "Folder" && wpRole.IsMatch(r.Name))
                    .OrderBy(r => r.Name))
                {
                    WriteWarning($"\"{role.GetPSPath()}\": Folder role detected. Ignored.");
                }
                #endregion
            }

            var wpRoles = roles.ConvertToWildcardPatternList();
            return existingRoles
                .Where(r => r.Type != "Folder")
                .SelectByWildcards(r => r?.Name, wpRoles)
                .Select(r => r.Name!)
                .Distinct();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
        }
        return [];
    }

    private bool UserAlreadyExists(Dictionary<OrchDriveInfo, Dictionary<string, Entities.User>> existingUsersPerDrive, OrchDriveInfo drive, string userName)
    {
        // Create the cache if it hasn't been created yet
        if (!existingUsersPerDrive.TryGetValue(drive, out var existingUsers))
        {
            var eusers = drive.Users.Get();
            existingUsers = eusers
                .Where(u => !string.IsNullOrEmpty(u.UserName))
                .ToDictionary(u => u.UserName!, u => u, StringComparer.OrdinalIgnoreCase);

            existingUsersPerDrive[drive] = existingUsers;
        }

        // Search the cache
        if (existingUsers.TryGetValue(userName, out var existingUser))
        {
            // If this user is already added to the tenant, warn and skip
            string existingUserName = $"{existingUser.UserName}";
            if (!string.IsNullOrEmpty(existingUser.FullName))
            {
                existingUserName += $" ({existingUser.FullName})";
            }
            WriteWarning($"'{drive.NameColonSeparator}{existingUserName}' already exists in this tenant.");
            return true;
        }

        return false;
    }

    protected override void EndProcessing()
    {
        if (_csvLines is null) return;

        using var cancelHandler = new ConsoleCancelHandler();

        HashSet<(OrchDriveInfo drive, string roleName)> warnedNoMatchingRoles = [];
        //HashSet<(OrchDriveInfo drive, string userName)> warnedNoExistingUsers = [];

        Dictionary<OrchDriveInfo, Dictionary<string, Entities.User>> existingUsersPerDrive = [];

        int index = 0;
        using var reporter = new ProgressReporter(this, 1, _csvLines.Count, "Add users... ");
        foreach (var key_line in _csvLines
            .OrderBy(kl => kl.Key.drive.NameColon)
            .ThenBy(kl => kl.Key.userName)
            .ThenBy(kl => kl.Key.type))
        {
            var drive = key_line.Key.drive;
            var type = key_line.Key.type;
            var userName = key_line.Key.userName;
            var line = key_line.Value;

            var targetRoles = ExcludeFolderRoles(drive, line.Roles, warnedNoMatchingRoles);

            //var existingUsers = drive.Users.Get();

            DirectoryObject? user = null;
            try
            {
                user = ResolveDirectoryName(this, drive, userName, type);
            }
            catch (Exception ex)
            {
                string t = drive.NameColonSeparator + userName;
                WriteError(new ErrorRecord(new OrchException(t, "Failed to search directory", ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (user is null)
            {
                // Ensure the progress bar advances even when the user is not found
                reporter.WriteProgress(++index, System.IO.Path.Combine(drive.NameColonSeparator, userName));
                continue;
            }

            cancelHandler.Token.ThrowIfCancellationRequested();

            string target = System.IO.Path.Combine(drive.NameColonSeparator, user.identityName ?? user.identifier!);
            if (!string.IsNullOrEmpty(user.displayName))
                target += $" ({user.displayName})";

            reporter.WriteProgress(++index, target);

            if (UserAlreadyExists(existingUsersPerDrive, drive, userName))
            {
                continue;
            }

            if (ShouldProcess(target, $"Add {Types[type]}"))
            {
                Entities.User postingUser = new()
                {
                    DirectoryIdentifier = user.identifier!,
                    Domain = "autogen",
                    //FullName = "",
                    Type = Types[user.type ?? 0],
                    NotificationSubscription = new()
                    {
                        Queues = true,
                        QueueItems = true,
                        Robots = true,
                        Jobs = true,
                        Tasks = true,
                        Schedules = true,
                        Insights = true,
                        CloudRobots = true,
                        Export = true,
                        RateLimitsDaily = true,
                        RateLimitsRealTime = false
                    },
                    RolesList = targetRoles.ToArray()
                };

                //postingUser.RolesList ??= [];

                postingUser.AssignBoolIfNotNull(line.MayHaveUserSession, (u, v) => u.MayHaveUserSession = v);
                postingUser.AssignBoolIfNotNull(line.MayHaveRobotSession, (u, v) => u.MayHaveRobotSession = v);
                postingUser.AssignBoolIfNotNull(line.MayHaveUnattendedSession, (u, v) => u.MayHaveUnattendedSession = v);
                postingUser.AssignBoolIfNotNull(line.MayHavePersonalWorkspace, (u, v) => u.MayHavePersonalWorkspace = v);

                // Use the per-row CsvLine value (like the other capability flags above), NOT the
                // bare cmdlet parameter: in CSV/pipeline batch mode the parameter holds only the
                // last row's value, so reading it here applied one row's IsExternalLicensed to every
                // user. line.IsExternalLicensed is per-row and (via the CsvLine ctor) non-null, so it
                // is sent consistently with MayHave*/RestrictToPersonalWorkspace.
                postingUser.AssignBoolIfNotNull(line.IsExternalLicensed, (u, v) => u.IsExternalLicensed = v);
                postingUser.AssignBoolIfNotNull(line.RestrictToPersonalWorkspace, (u, v) => u.RestrictToPersonalWorkspace = v);

                postingUser.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);

                if (!string.IsNullOrEmpty(line.UR_UserName) ||
                    !string.IsNullOrEmpty(line.UR_CredentialStore) ||
                    !string.IsNullOrEmpty(line.UR_Password) ||
                    !string.IsNullOrEmpty(line.UR_CredentialExternalName) ||
                    !string.IsNullOrEmpty(line.UR_CredentialType) ||
                    line.UR_LimitConcurrentExecution is not null)
                {
                    postingUser.UnattendedRobot = new();
                    postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(line.UR_UserName, (u, v) => u.UserName = v);
                    postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(line.UR_Password, (u, v) => u.Password = v);
                    postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(line.UR_CredentialExternalName, (u, v) => u.CredentialExternalName = v);
                    postingUser.UnattendedRobot.AssignStringIfNotNullOrEmpty(line.UR_CredentialType, (u, v) => u.CredentialType = v);
                    postingUser.UnattendedRobot.AssignBoolIfNotNull(line.UR_LimitConcurrentExecution, (u, v) => u.LimitConcurrentExecution = v);

                    if (line.UR_CredentialStore is not null)
                    {
                        var credentialStores = drive.CredentialStores.Get();
                        var targetStore = credentialStores.FirstOrDefault(cs => string.Compare(cs.Name, line.UR_CredentialStore, StringComparison.OrdinalIgnoreCase) == 0);
                        postingUser.UnattendedRobot.CredentialStoreId = targetStore?.Id;
                    }

                    // If a password is specified, populate the CredentialStoreId
                    if (postingUser.UnattendedRobot.Password is not null && postingUser.UnattendedRobot.CredentialStoreId is null)
                    {
                        var credentialStores = drive.CredentialStores.Get();
                        var defaultStore = credentialStores.FirstOrDefault(cs => string.Compare(cs.Name, "Orchestrator Database", StringComparison.OrdinalIgnoreCase) == 0);
                        postingUser.UnattendedRobot.CredentialStoreId = defaultStore?.Id;
                    }
                }

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

                if (!string.IsNullOrEmpty(line.ES_TracingLevel) ||
                    line.ES_StudioNotifyServer is not null ||
                    line.ES_LoginToConsole is not null ||
                    (line.ES_ResolutionWidth is not null && line.ES_ResolutionWidth != 0) ||
                    (line.ES_ResolutionHeight is not null && line.ES_ResolutionHeight != 0) ||
                    (line.ES_ResolutionDepth is not null && line.ES_ResolutionDepth != 0) ||
                    line.ES_FontSmoothing is not null ||
                    line.ES_AutoDownloadProcess is not null)
                {
                    //  0: User, 1: Group, 2: Machine, 3: Robot, 4: ExternalApplication
                    // UnattendedRobot can be configured for User and Robot.
                    if (user.type == 0 || user.type == 3)
                    {
                        postingUser.UnattendedRobot ??= new();
                        postingUser.UnattendedRobot.ExecutionSettings ??= new();
                        UpdateExecutionSettings(postingUser.UnattendedRobot.ExecutionSettings);
                    }

                    // RobotProvision is Attended, so it can only be configured for User.
                    if (user.type == 0)
                    {
                        postingUser.RobotProvision ??= new();
                        postingUser.RobotProvision.ExecutionSettings ??= new();
                        UpdateExecutionSettings(postingUser.RobotProvision.ExecutionSettings);
                    }
                }

                if (user.type == 3) // For robot type
                {
                    postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                    postingUser.MayHaveUnattendedSession = true; // Must be true.
                    postingUser.MayHavePersonalWorkspace = false; // Always false is fine.
                    postingUser.MayHaveRobotSession = false; // Required or it will fail

                    postingUser.UnattendedRobot ??= new();

                    postingUser.UnattendedRobot.LimitConcurrentExecution ??= false;

                    postingUser.RestrictToPersonalWorkspace ??= false;
                    //postingUser.UnattendedRobot.ExecutionSettings ??= new();

                    //postingUser.UpdatePolicy ??= new();
                    //postingUser.UpdatePolicy.Type ??= "None";
                }
                else if (user.type == 4) // For application type
                {
                    postingUser.MayHaveUserSession ??= false; // prohibit 'Standard Interface'
                }

                if (user.type == 0 || user.type == 3) // For user or robot type
                {
                    if (postingUser.UnattendedRobot is not null)
                    {
                        if (string.IsNullOrEmpty(postingUser.UnattendedRobot.Password))
                        {
                            postingUser.UnattendedRobot.CredentialType ??= "NoCredential";
                        }
                        else
                        {
                            postingUser.UnattendedRobot.CredentialType ??= "Default";
                        }
                    }
                }

                try
                {
                    var createdUser = drive.OrchAPISession.PostUser(postingUser);
                    if (createdUser is not null)
                    {
                        createdUser.Path = drive.NameColonSeparator;
                        WriteObject(createdUser);
                    }
                    drive.Users.ClearCache();
                    drive.UsersDetailed.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddUserError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
