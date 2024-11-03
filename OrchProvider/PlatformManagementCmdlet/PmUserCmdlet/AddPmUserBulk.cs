using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;
using System.Linq;
using System.Collections.Concurrent;

namespace UiPath.PowerShell.Commands
{
    class CsvLine(string? name, string? surname, string? displayName, string? type, string? bypassBasicAuthRestriction, string? invitationAccepted)
    {
        // id は API call 時に new Guid() で生成
        // email は Dictionary の Key で管理
        // userName は email と同じものにする
        public string? name { get; set; } = name;
        public string? surname { get; set; } = surname;
        public string? displayName { get; set; } = displayName;
        public string? type { get; set; } = type;
        public bool? bypassBasicAuthRestriction { get; set; } = bool.TryParse(bypassBasicAuthRestriction, out var value) ? value : null;
        public bool? invitationAccepted { get; set; } = bool.TryParse(invitationAccepted, out var value) ? value : null;
    }

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

    [Cmdlet(VerbsCommon.Add, "OrchPmUserBulk", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.PmUser))]
    public class AddPmUserBulkCommand : OrchestratorPSCmdlet
    {
        // Key: (drive, groupIds) Value: Dictionary<email, csvLine>
        Dictionary<(OrchDriveInfo drive, string[] groupIds), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

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
        [ArgumentCompleter(typeof(PmGroupNameCompleter<UserName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<UserName>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            // CSV に指定された GroupName はカンマで区切る
            var groupName = GroupName?
                 .SelectMany(name => name.Split(',', StringSplitOptions.RemoveEmptyEntries))
                 .Select(name => name.Trim())
                 .ToArray();

            _params ??= [];

            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpGroupName = groupName.ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                var groups = drive.GetPmGroups();
                var groupIds = groups.Values
                    .SelectByWildcards(g => g?.name, wpGroupName)
                    .Select(g => g!.id)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();

                string target = System.IO.Path.Combine(drive.NameColonSeparator, Email!);
                if (_params.TryGetValue((drive, groupIds)!, out var userName_line))
                {
                    if (userName_line.TryGetValue(Email!, out var line))
                    {
                        WriteWarning($"{drive.NameColonSeparator}{Email}: duplicate entry found. This entry will be ignored.");
                        continue;
                    }
                    if (ShouldProcess(target, "Add PmUser"))
                    {
                        line = new(Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                        userName_line[Email!] = line;
                    }
                }
                else
                {
                    if (ShouldProcess(target, "Add PmUser"))
                    {
                        userName_line = [];
                        CsvLine line = new(Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                        userName_line[Email!] = line;
                        _params[(drive, groupIds)!] = userName_line;
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            foreach (var param in _params)
            {
                var drive = param.Key.drive;
                var groupIds = param.Key.groupIds;

                CreateUsersCommand payload = new()
                {
                    users = [],
                    partitionGlobalId = drive.GetPartitionGlobalId(),
                    groupIDs = groupIds
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
                    drive._dicPmUsers = null;
                    drive._dicPmUsers_Exception.ClearCache();
                    drive._dicPmGroups = null;
                    drive._dicPmGroups_Exception.ClearCache();
                    drive._dicSearchForUsersAndGroups = null;
                    drive._dicSearchForUsersAndGroups_Exception.ClearCache();

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
}
