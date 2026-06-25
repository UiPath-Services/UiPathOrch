using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmUser", SupportsShouldProcess = true)]
public class CopyPmUserCmdlet : OrchestratorPSCmdlet
{
    // Key: (drive, groupIds) Value: Dictionary<email, csvLine>
    Dictionary<(OrchDriveInfo drive, string[] groupIds), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(EffectivePath(Path, LiteralPath));
        var dstDrives = SessionState.EnumPmDrives(Destination);
        var wpEmail = Email.ConvertToWildcardPatternList();

        var userMapping = dstDrives.Count == 1
            ? SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrives[0], UserMappingCsv)
            : null;

        #region Build list of users to copy
        var targetUsers = new Dictionary<List<string>, List<PmUser>>(new ListStringComparer());
        try
        {
            var srcAllUsers = srcDrive.PmUsers.Get();

            var srcUsers = srcDrive.PmUsers.Get()
                .FilterByWildcards(u => u?.email, wpEmail)
                .OrderBy(u => u.name);

            foreach (var srcUser in srcUsers)
            {
                if (ShouldProcess(srcUser.GetPSPath(srcDrive.NameColonSeparator), "Copy PmUser"))
                {
                    List<string> groupIds = srcUser.groupIDs?.Order().ToList() ?? [];
                    if (!targetUsers.TryGetValue(groupIds, out var users))
                    {
                        users = [];
                        targetUsers[groupIds] = users;
                    }
                    users.Add(srcUser);
                }
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, "Failed to get PmUsers", ex), "GetPmUserError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        #endregion

        if (targetUsers.Count == 0)
        {
            return;
        }

        #region Get group list from srcDrive
        IEnumerable<PmGroup> srcGroups;
        try
        {
            srcGroups = srcDrive.PmGroups.Get();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, "Failed to get PmGroups", ex), "GetPmUserError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        #endregion

        foreach (var dstDrive in dstDrives)
        {
            if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId())
            {
                WriteWarning($"The drives '{srcDrive.NameColonSeparator}' and '{dstDrive.NameColonSeparator}' belong to the same organization, so this operation will be skipped.");
                continue;
            }

            #region Get group list from dstDrive
            Dictionary<string, PmGroup> dstGroups;
            try
            {
                dstGroups = dstDrive.PmGroups.Get().ToDictionary(g => g.name!, g => g, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, "Failed to get PmGroups", ex), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                continue;
            }
            #endregion

            foreach (var groupedUsers in targetUsers)
            {
                var srcGroupIds = groupedUsers.Key;
                var srcUsers = groupedUsers.Value; // List of srcUsers belonging to the same srcGroups


                #region Build the payload
                var payload = new CreateUsersCommand()
                {
                    users = [],
                    partitionGlobalId = dstDrive.GetPartitionGlobalId(),
                    groupIDs = Core.OrchProvider.FindDstPmGroups(
                        this, srcDrive, srcGroupIds,
                        dstDrive, "Copying PmUser")?.Select(group => group.id!).ToArray()
                };
                #endregion

                #region Get user list from dstDrive
                Dictionary<string, PmUser> dstUsers = null;
                try
                {
                    // Build the "already taken" lookup defensively: a plain
                    // ToDictionary(u => u.email) throws "an item with the same key
                    // has already been added" when the destination has two+ users
                    // sharing a key — in practice empty-email entries (key ""),
                    // which collide. That exception was swallowed into a warning,
                    // leaving dstUsers null and the duplicate check silently
                    // disabled. Drop empty emails (they can't be a meaningful dup
                    // target for an email-keyed create) and collapse duplicates so
                    // the lookup is built reliably.
                    dstUsers = dstDrive.PmUsers.Get()
                        .Where(u => !string.IsNullOrEmpty(u.email))
                        .GroupBy(u => u.email!, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    WriteWarning($"{dstDrive.NameColonSeparator}: Failed to get PmUsers: {ex.Message}");
                }
                #endregion

                #region Add srcUsers belonging to the same srcGroups into a single payload
                foreach (var srcUser in srcUsers)
                {
                    #region Skip users without an email address
                    // Destination orgs key local users by email and reject creation
                    // without one. On-prem MSI sources often have local users with
                    // an alphabetic userName and an EMPTY email (observed: "" not
                    // null). Previously these were sent through with email="" and,
                    // because the BulkCreate response is not inspected, were silently
                    // dropped by the server — no error, no warning, user not told.
                    // Skip them explicitly with an actionable warning instead;
                    // recreate via Get-PmUser -ExportCsv -> set the email -> New-PmUser.
                    if (string.IsNullOrEmpty(srcUser.email))
                    {
                        WriteWarning($"{dstDrive.NameColonSeparator}: Skipping user '{srcUser.userName}' because it has no email address. Local users without an email cannot be created at the destination; recreate them manually (Get-PmUser -ExportCsv -> set the email -> New-PmUser).");
                        continue;
                    }
                    #endregion

                    #region Skip if a user with the same name already exists in dstDrive
                    if (dstUsers?.ContainsKey(srcUser.email) ?? false)
                    {
                        WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"Username '{srcUser.email}' is already taken."), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                        continue;
                    }
                    #endregion

                    #region Add this user to the payload
                    // Name resolution via UserMappingCsv
                    string mappedUserName = !string.IsNullOrEmpty(srcUser.email) ? srcUser.email : srcUser.userName;
                    if (userMapping is not null)
                    {
                        string lookupKey = srcUser.email ?? srcUser.userName ?? "";
                        if (userMapping.TryGetValue(lookupKey, out var mapped) && !string.IsNullOrEmpty(mapped))
                        {
                            mappedUserName = mapped;
                        }
                    }

                    var command = new CreateUserCommandBase()
                    {
                        id = Guid.NewGuid().ToString(),

                        userName = mappedUserName,

                        email = srcUser.email,
                        name = srcUser.name,
                        surname = srcUser.surname,
                        displayName = srcUser.displayName,
                        //type = srcUser.type, // Why is the type different?
                        bypassBasicAuthRestriction = srcUser.bypassBasicAuthRestriction,
                        // legacyId // This is probably not needed.
                        invitationAccepted = srcUser.invitationAccepted
                    };
                    payload.users.Add(command);
                    #endregion
                }
                #endregion

                if (payload.users.Count == 0) continue;

                try
                {
                    var response = dstDrive.CreatePmUserBulk(payload);
                    dstDrive.PmUsers.ClearCache();
                    dstDrive.PmGroups.ClearCache();

                    // BulkCreate can refuse some or all users (e.g. invalid /
                    // duplicate / email-constraint) WITHOUT throwing. The previous
                    // code ignored the response, so such rejections were silently
                    // dropped — the operator never learned a user wasn't migrated.
                    // Surface an explicit non-success the way NewPmUserBulk gates on
                    // result.succeeded. Only act on an explicit `false` so a null
                    // result (API-shape variance) doesn't produce a false alarm.
                    if (response?.result?.succeeded == false)
                    {
                        var detail = response.result.errors is { Length: > 0 } errs
                            ? string.Join("; ", errs)
                            : "no detail returned by the server";
                        WriteWarning($"{dstDrive.NameColonSeparator}: BulkCreate did not fully succeed for {payload.users.Count} user(s) in this group. Server reported: {detail}");
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, "Failed to new PmUsers", ex), "CopyPmUserError", ErrorCategory.InvalidOperation, dstDrive));
                }
            }
        }
    }
}
