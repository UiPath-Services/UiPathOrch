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

            // Automation Cloud keys local users by email (users are invite-based and
            // the identifier IS the email), so keep the historical email-as-userName
            // there to avoid regressing Cloud migrations (confirmed: Cloud users have
            // userName == email). Automation Suite and on-premises use an identity
            // model where a local user's userName is separate from the email (verified
            // on AS: a migrated user keeps a userName != email), so there we preserve
            // the source userName. A -UserMappingCsv entry overrides either way.
            bool preserveUserName = dstDrive._psDrive.ResolvedEdition != OrchEdition.Cloud;

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
                HashSet<string> dstUserNames = null;
                try
                {
                    // Build the "already taken" lookups defensively: a plain
                    // ToDictionary(u => u.email) throws "an item with the same key
                    // has already been added" when the destination has two+ users
                    // sharing a key — in practice empty-email entries (key ""),
                    // which collide. That exception was swallowed into a warning,
                    // leaving dstUsers null and the duplicate check silently
                    // disabled. Drop empty emails (they can't be a meaningful dup
                    // target for an email-keyed create) and collapse duplicates so
                    // the lookup is built reliably. Also index by userName so the
                    // duplicate guard catches a collision on the name we create with
                    // (the destination's identifier once userName != email).
                    var dstAllUsers = dstDrive.PmUsers.Get();
                    dstUsers = Core.OrchProvider.BuildDstUserLookup(dstAllUsers);
                    dstUserNames = new HashSet<string>(
                        dstAllUsers.Where(u => !string.IsNullOrEmpty(u?.userName)).Select(u => u!.userName!),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    WriteWarning($"{dstDrive.NameColonSeparator}: Failed to get PmUsers: {ex.Message}");
                }
                #endregion

                #region Add srcUsers belonging to the same srcGroups into a single payload
                foreach (var srcUser in srcUsers)
                {
                    #region Skip email-less users only where the destination requires an email
                    // Automation Cloud keys local users by email and rejects creation
                    // without one (users are invite-based there), so an email-less source
                    // user cannot be created — skip it with an actionable warning. On
                    // Automation Suite / on-premises the userName is the identifier and the
                    // email is optional (verified on-prem 25.10.2: BulkCreate accepts a
                    // userName with an empty email), so such users migrate username-only
                    // instead of being silently dropped.
                    if (Core.OrchProvider.MustSkipEmaillessUser(preserveUserName, srcUser))
                    {
                        WriteWarning($"{dstDrive.NameColonSeparator}: Skipping user '{srcUser.userName}' because it has no email address. Automation Cloud cannot create a local user without an email — recreate it manually there, or migrate to Automation Suite / on-premises where the userName is enough.");
                        continue;
                    }
                    #endregion

                    #region Resolve the userName to create with
                    // Preserve the source userName by default; Automation Cloud keeps
                    // email-as-userName; a -UserMappingCsv entry overrides. The full
                    // policy (and its edge cases) lives in ResolvePmUserName so it can
                    // be unit-tested without a live drive.
                    string mappedUserName = Core.OrchProvider.ResolvePmUserName(
                        srcUser.userName, srcUser.email, preserveUserName, userMapping);

                    // A source user with neither a userName nor an email has no identifier
                    // to create it with.
                    if (string.IsNullOrEmpty(mappedUserName))
                    {
                        WriteWarning($"{dstDrive.NameColonSeparator}: Skipping a source user that has neither a userName nor an email — nothing to create it with.");
                        continue;
                    }
                    #endregion

                    #region Skip if the user already exists at the destination
                    // Match on the userName we would create (the destination's
                    // identifier) or on the email (catches a user created by an earlier
                    // email-as-userName run). Either collision means the server would
                    // reject the create.
                    if ((dstUserNames?.Contains(mappedUserName) ?? false) ||
                        (dstUsers?.ContainsKey(srcUser.email!) ?? false))
                    {
                        WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"A user with the name '{mappedUserName}' or email '{srcUser.email}' already exists at the destination."), "CopyPmUserDuplicate", ErrorCategory.ResourceExists, srcDrive));
                        continue;
                    }
                    #endregion

                    #region Add this user to the payload
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
                    if (Core.OrchProvider.IsBulkCreateFailure(response))
                    {
                        var detail = response?.result?.errors is { Length: > 0 } errs
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
