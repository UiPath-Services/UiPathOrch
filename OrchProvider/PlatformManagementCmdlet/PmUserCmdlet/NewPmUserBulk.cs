using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

class CsvLine(string? name, string? surname, string? displayName, string? type, string? bypassBasicAuthRestriction, string? invitationAccepted)
{
    // id は API call 時に new Guid() で生成
    // email は Dictionary の Key で管理
    // userName は email と同じものにする
    public string? name { get; set; } = name;
    public string? surname { get; set; } = surname;
    public string? displayName { get; set; } = displayName;
    public string? type { get; set; } = type;
    public bool? bypassBasicAuthRestriction { get; set; } = bypassBasicAuthRestriction.ToNullableBool();
    public bool? invitationAccepted { get; set; } = invitationAccepted.ToNullableBool();
}

// TODO: これは OrchComparer.cs に移すべきだ多分。
internal class DriveGroupIdsComparer : IEqualityComparer<(OrchDriveInfo drive, string[] groupIds)>
{
    public bool Equals((OrchDriveInfo drive, string[] groupIds) x, (OrchDriveInfo drive, string[] groupIds) y)
    {
        // drive は単純な参照比較
        if (!ReferenceEquals(x.drive, y.drive))
        {
            return false;
        }

        // groupIds は内容を比較
        return x.groupIds.SequenceEqual(y.groupIds);
    }

    public int GetHashCode((OrchDriveInfo drive, string[] groupIds) obj)
    {
        // drive のハッシュコードを使用
        int hash = obj.drive.GetHashCode();

        // groupIds の内容に基づいてハッシュコードを計算
        foreach (var guid in obj.groupIds)
        {
            hash = hash ^ guid.GetHashCode();
        }

        return hash;
    }
}

[Cmdlet(VerbsCommon.New, "PmUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.PmUser))]
public class AddPmUserBulkCommand : OrchestratorPSCmdlet
{
    // Key: (drive, groupIds) Value: Dictionary<email, csvLine>
    Dictionary<(OrchDriveInfo drive, string[] groupNames), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("UserName")]
    public string? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? SurName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? DisplayName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<PmUserTypeItems>))]
    public string? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? BypassBasicAuthRestriction { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? InvitationAccepted { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        // CSV に指定された GroupName はカンマで区切る
        var groupNameEnum = GroupName.Split1stValueByUnescapedCommas();

        _params ??= [];

        var drives = OrchDriveInfo.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            var groups = drive.GetPmGroups();
            // 既存のグループ名はケースを無視する必要はないが、新規作成のグループ名についてはケースを無視しておかないと。
            HashSet<string> groupNames = new(StringComparer.OrdinalIgnoreCase);
            
            foreach (var groupName in groupNameEnum ?? [])
            {
                // グループ名がワイルドカードを含んでいれば展開、そうでなければそのまま保持
                // で、そのまま保持した名前のグループが存在しなければ、後でグループを作成
                if (WildcardPattern.ContainsWildcardCharacters(groupName))
                {
                    var wpGroupName = new WildcardPattern(groupName, WildcardOptions.IgnoreCase);
                    var targetGroupNames = groups.Values
                        .Where(g => !string.IsNullOrEmpty(g.name) && wpGroupName.IsMatch(g.name))
                        .Select(g => g.name!);
                    groupNames.UnionWith(targetGroupNames);
                }
                else
                {
                    groupNames.Add(groupName);
                }
            }

            string[] orderedGroupNames = [.. groupNames.Order()]; // キーとして利用できるようにソートする

            string target = System.IO.Path.Combine(drive.NameColonSeparator, Email!);
            if (_params.TryGetValue((drive, orderedGroupNames)!, out var userName_line))
            {
                if (userName_line.TryGetValue(Email!, out var line))
                {
                    WriteWarning($"{drive.NameColonSeparator}{Email}: duplicate entry found. This entry will be ignored.");
                    continue;
                }
                if (ShouldProcess(target, "New PmUser"))
                {
                    line = new(Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                    userName_line[Email!] = line;
                }
            }
            else
            {
                if (ShouldProcess(target, "New PmUser"))
                {
                    userName_line = [];
                    CsvLine line = new(Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                    userName_line[Email!] = line;
                    _params[(drive, orderedGroupNames)!] = userName_line;
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        foreach (var param in _params)
        {
            var drive = param.Key.drive;
            var groupNames = param.Key.groupNames;

            // グループ名からグループ ID を取得
            List<string> groupIds = [];
            foreach (var groupName in groupNames)
            {
                var group = drive.GetPmGroups().Values.FirstOrDefault(g => g.name?.Equals(groupName, StringComparison.OrdinalIgnoreCase) ?? false);
                if (group is null)
                {
                    group = this.CreatePmGroup(drive, groupName);
                }
                if (string.IsNullOrEmpty(group?.id)) continue;
                groupIds.Add(group.id);
            }

            CreateUsersCommand payload = new()
            {
                users = [],
                partitionGlobalId = drive.GetPartitionGlobalId(),
                groupIDs = groupIds.ToArray()
            };

            foreach (var userName_line in param.Value)
            {
                var email = userName_line.Key;
                var line = userName_line.Value;
                var user = new CreateUserCommandBase()
                {
                    id = Guid.NewGuid().ToString(),
                    bypassBasicAuthRestriction = line.bypassBasicAuthRestriction,
                    legacyId = null,
                    invitationAccepted = line.invitationAccepted
                };

                // userName と email の両方に email を入れる
                user.AssignStringIfNotNullOrEmpty(email,            (u, v) => u.userName = v);
                user.AssignStringIfNotNullOrEmpty(email,            (u, v) => u.email = v);
                user.AssignStringIfNotNullOrEmpty(line.name,        (u, v) => u.name = v);
                user.AssignStringIfNotNullOrEmpty(line.surname,     (u, v) => u.surname = v);
                user.AssignStringIfNotNullOrEmpty(line.displayName, (u, v) => u.displayName = v);
                user.AssignStringIfNotNullOrEmpty(line.type,        (u, v) => u.type = v);

                payload.users.Add(user);
            }

            try
            {
                var response = drive.OrchAPISession.CreatePmUserBulk(payload);
                drive.PmUsers.ClearCache();
                drive._dicPmGroups = null;
                drive._dicPmGroups_Exception.ClearCache();
                drive._dicSearchDirectory = null;
                drive._dicSearchDirectory_Exception.ClearCache();

                if (response?.result?.succeeded ?? false)
                {
                    foreach (var user in response?.users ?? [])
                    {
                        user.Path = drive.NameColonSeparator;
                    }
                    WriteObject(response?.users?.OrderBy(u => u.name), true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "AddPmUserBulkError", ErrorCategory.InvalidOperation, payload));
            }
        }
    }
}
