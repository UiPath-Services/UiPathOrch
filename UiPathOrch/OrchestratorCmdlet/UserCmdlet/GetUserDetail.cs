using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUserDetail")]
[OutputType(typeof(User))]
public class GetUserDetailCmdlet : OrchestratorPSCmdlet
{
    // -UserName is Mandatory by design — the detail path makes one API call
    // per matched user, so accidental fan-out from a default "all users" would
    // be expensive on large tenants. Wildcards (including "*") still work; the
    // user just has to type the selector explicitly.
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string[] UserName { get; set; } = default!;

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedUsers.csv";
    internal static readonly string[] CsvHeaders = [
        "Path",
        "UserName",
        "FullName",
        "Type",
        "IsExternalLicensed",
        "MayHaveUserSession",
        "MayHaveRobotSession",
        "MayHaveUnattendedSession",
        "MayHavePersonalWorkspace",
        "RestrictToPersonalWorkspace",
        "UpdatePolicyType",
        "UpdatePolicyVersion",
        "UR_UserName",
        "UR_Password",
        "UR_CredentialStore",
        "UR_CredentialExternalName",
        "UR_CredentialType",
        "UR_LimitConcurrentExecution",
        "ES_TracingLevel",
        "ES_StudioNotifyServer",
        "ES_LoginToConsole",
        "ES_ResolutionWidth",
        "ES_ResolutionHeight",
        "ES_ResolutionDepth",
        "ES_FontSmoothing",
        "ES_AutoDownloadProcess",
        "Roles"
    ];

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        EmitDetailedUsers(this, drives, wpUserName, wpFullName, wpType, writer);

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }

    /// <summary>
    /// Canonical implementation for "fetch each matched user's detail and emit
    /// either to caller.WriteObject or to the supplied CSV writer". Called by
    /// this cmdlet's ProcessRecord, by GetUserCommand's deprecated
    /// -ExpandDetails path, and by GetUserCommand's -ExportCsv path.
    ///
    /// Any of the three wildcard-pattern lists may be null (= no filter on
    /// that field). Phase 1 (drive.GetUsers + filter) and Phase 2
    /// (drive.GetUser per matched user) share a single cap=4 semaphore via
    /// ChainedThreadPool — drive-level fetches can race, and per-drive
    /// progress streams as Phase 2 results arrive without waiting for all
    /// drives' Phase 1 to finish.
    /// </summary>
    internal static void EmitDetailedUsers(
        OrchestratorPSCmdlet caller,
        IEnumerable<OrchDriveInfo> drives,
        List<WildcardPattern>? userNameWildcards,
        List<WildcardPattern>? fullNameWildcards,
        List<WildcardPattern>? typeWildcards,
        StreamWriter? writer)
    {
        using var cancelHandler = new ConsoleCancelHandler();

        using var pool = OrchThreadPool.RunForEachChained(
            drives,
            drive => drive.NameColonSeparator,
            drive => (object)drive,
            drive => drive.Users.Get()
                .FilterByWildcards(u => u?.FullName, fullNameWildcards)
                // Match both UserName (tenant form) and EmailAddress (canonical);
                // see GetUserCommand for the rationale.
                .FilterByWildcardsAny([u => u?.UserName, u => u?.EmailAddress], userNameWildcards)
                .FilterByWildcards(u => u?.Type, typeWildcards)
                .OrderBy(u => u.UserName)
                .Select(u => (drive, user: u)),
            t => t.user.GetPSPath(),
            t => (object)t.user,
            t => t.drive.UsersDetailed.Get(t.user.Id!.Value),
            cancelHandler.Token);

        // Total user count is unknown until all Phase 1 fetches complete,
        // so the progress bar shows the running index without a percentage.
        int index = 0;
        using var reporter = new ProgressReporter(caller, 1, 0, "Get users... ");
        foreach (var task in pool)
        {
            try
            {
                var detailedUser = task.GetResult(cancelHandler.Token);
                if (detailedUser is null) continue;

                reporter.WriteProgress(++index, detailedUser.GetPSPath());

                var (drive, _) = task.Source;
                if (writer is not null) { WriteCsvContent(writer, drive, detailedUser); }
                else { caller.WriteObject(detailedUser); }
            }
            catch (OrchException ex)
            {
                caller.WriteError(new ErrorRecord(ex, "GetUserDetailError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 1 errors (per-drive GetUsers failures). Same ErrorId as
        // Phase 2 to preserve the legacy single-id behavior.
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            caller.WriteError(new ErrorRecord(ex, "GetUserDetailError", ErrorCategory.InvalidOperation, ex.Target));
        }
    }

    private static void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, User? p)
    {
        if (p is null) return;

        string ur_credentialStore = null;
        if (p.UnattendedRobot?.CredentialStoreId is not null)
        {
            var credentialStores = drive.CredentialStores.Get();
            var credentialStore = credentialStores.FirstOrDefault(c => c.Id == p.UnattendedRobot.CredentialStoreId);
            ur_credentialStore = credentialStore?.Name;
        }

        string[] line = [
            EscapeCsvValue(p.Path, true),
            EscapeCsvValue(p.UserName, true),
            EscapeCsvValue(p.FullName),
            EscapeCsvValue(p.Type),
            EscapeCsvValue(p.IsExternalLicensed),
            EscapeCsvValue(p.ExplicitMayHaveUserSession ?? p.MayHaveUserSession),
            EscapeCsvValue(p.ExplicitMayHaveRobotSession ?? p.MayHaveRobotSession),
            EscapeCsvValue(p.MayHaveUnattendedSession),
            EscapeCsvValue(p.ExplicitMayHavePersonalWorkspace ?? p.MayHavePersonalWorkspace),
            EscapeCsvValue(p.ExplicitRestrictToPersonalWorkspace ?? p.RestrictToPersonalWorkspace),
            EscapeCsvValue(p.UpdatePolicy?.Type),
            EscapeCsvValue(p.UpdatePolicy?.SpecificVersion),
            EscapeCsvValue(p.UnattendedRobot?.UserName),
            EscapeCsvValue(""), // p.UnattendedRobot?.Password — server returns "*****", skip to avoid clobbering
            EscapeCsvValue(ur_credentialStore, true),
            EscapeCsvValue(p.UnattendedRobot?.CredentialExternalName),
            EscapeCsvValue(p.UnattendedRobot?.CredentialType),
            EscapeCsvValue(p.UnattendedRobot?.LimitConcurrentExecution),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.TracingLevel),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.StudioNotifyServer),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.LoginToConsole),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionWidth),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionHeight),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionDepth),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.FontSmoothing),
            EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.AutoDownloadProcess),
            EscapeCsvValue(p.RolesList, true)
        ];

        writer.WriteCsvLine(line);
    }
}
