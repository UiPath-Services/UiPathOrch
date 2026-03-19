using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Email_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmUser", SupportsShouldProcess = true)]
public class CopyPmUserCommand : OrchestratorPSCmdlet
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

    [Parameter]
    public string? UserMappingCsv { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(Path);
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
                if (ShouldProcess(srcUser.GetPSPath(), "Copy PmUser"))
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
                    dstUsers = dstDrive.PmUsers.Get().ToDictionary(
                        u => u.email!,
                        u => u,
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
                    #region Skip if a user with the same name already exists in dstDrive
                    if (dstUsers?.ContainsKey(srcUser.email!) ?? false)
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
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, "Failed to new PmUsers", ex), "CopyPmUserError", ErrorCategory.InvalidOperation, dstDrive));
                }
            }
        }
    }
}
