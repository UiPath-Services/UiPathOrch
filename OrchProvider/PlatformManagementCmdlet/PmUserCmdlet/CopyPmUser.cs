using System.Collections.Concurrent;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Email_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.PmUser))]
public class CopyPmUserCommand : OrchestratorPSCmdlet
{
    // Key: (drive, groupIds) Value: Dictionary<email, csvLine>
    Dictionary<(OrchDriveInfo drive, string[] groupIds), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = OrchDriveInfo.GetPmDrive(Path);
        var dstDrives = OrchDriveInfo.EnumPmDrives(Destination);
        var wpEmail = Email.ConvertToWildcardPatternList();

        #region コピーするユーザーの一覧を作成
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

        #region srcDrive のグループ一覧を取得
        ConcurrentDictionary<string, PmGroup> srcGroups;
        try
        {
            srcGroups = srcDrive.GetPmGroups();
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

            #region dstDrive のグループ一覧を取得
            Dictionary<string, PmGroup> dstGroups;
            try
            {
                dstGroups = dstDrive.GetPmGroups().Values.ToDictionary(g => g.name!, g => g, StringComparer.OrdinalIgnoreCase);
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
                var srcUsers = groupedUsers.Value; // 同じ srcGroups に所属する srcUser 一覧


                #region payload を作成
                var payload = new CreateUsersCommand()
                {
                    users = [],
                    partitionGlobalId = dstDrive.GetPartitionGlobalId(),
                    groupIDs = Core.OrchProvider.FindDstPmGroups(
                        this, srcDrive, srcGroupIds,
                        dstDrive, "Copying PmUser")?.Select(group => group.id!).ToArray()
                };
                #endregion

                #region dstDrive のユーザー一覧を取得
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

                #region 同じ srcGroups に所属する srcUsers を、単一の payload に追加
                foreach (var srcUser in srcUsers)
                {
                    #region すでに dstDrive に同名のユーザーがいればスキップ
                    if (dstUsers?.ContainsKey(srcUser.email!) ?? false)
                    {
                        WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"Username '{srcUser.email}' is already taken."), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                        continue;
                    }
                    #endregion

                    #region payload に、このユーザーを追加
                    var command = new CreateUserCommandBase()
                    {
                        id = Guid.NewGuid().ToString(),
                        
                        userName = !string.IsNullOrEmpty(srcUser.email) ? srcUser.email : srcUser.userName,
                                                               
                        email = srcUser.email,
                        name = srcUser.name,
                        surname = srcUser.surname,
                        displayName = srcUser.displayName,
                        //type = srcUser.type, // なぜ型が違うのか？
                        bypassBasicAuthRestriction = srcUser.bypassBasicAuthRestriction,
                        // legacyId // これは多分不要であろう。
                        invitationAccepted = srcUser.invitationAccepted
                    };
                    payload.users.Add(command);
                    #endregion
                };
                #endregion

                if (payload.users.Count == 0) continue;

                try
                {
                    var response = dstDrive.CreatePmUserBulk(payload);
                    dstDrive.PmUsers.ClearCache();
                    dstDrive._dicPmGroups = null;
                    dstDrive._dicPmGroups_Exception.ClearCache();

                    if (response?.result?.succeeded ?? false)
                    {
                        foreach (var user in response?.users ?? [])
                        {
                            user.Path = dstDrive.NameColonSeparator;
                        }
                        WriteObject(response?.users?.OrderBy(u => u.email), true);
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
