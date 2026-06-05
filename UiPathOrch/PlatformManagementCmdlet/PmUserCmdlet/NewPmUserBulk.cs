using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

class CsvLine(string? name, string? surname, string? displayName, string? type, string? bypassBasicAuthRestriction, string? invitationAccepted)
{
    // id is generated with new Guid() at API call time
    // email is managed as the Dictionary key
    // userName is set to the same value as email
    public string? name { get; set; } = name;
    public string? surname { get; set; } = surname;
    public string? displayName { get; set; } = displayName;
    public string? type { get; set; } = type;
    public bool? bypassBasicAuthRestriction { get; set; } = bypassBasicAuthRestriction.ToNullableBool();
    public bool? invitationAccepted { get; set; } = invitationAccepted.ToNullableBool();
}

internal class DriveGroupIdsComparer : IEqualityComparer<(OrchDriveInfo drive, string[] groupIds)>
{
    public bool Equals((OrchDriveInfo drive, string[] groupIds) x, (OrchDriveInfo drive, string[] groupIds) y)
    {
        // drive uses simple reference comparison
        if (!ReferenceEquals(x.drive, y.drive))
        {
            return false;
        }

        // groupIds uses content comparison
        return x.groupIds.SequenceEqual(y.groupIds);
    }

    public int GetHashCode((OrchDriveInfo drive, string[] groupIds) obj)
    {
        // Use the hash code of drive
        int hash = obj.drive.GetHashCode();

        // Calculate hash code based on the contents of groupIds
        foreach (var guid in obj.groupIds)
        {
            hash = hash ^ guid.GetHashCode();
        }

        return hash;
    }
}

[Cmdlet(VerbsCommon.New, "PmUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.PmUser))]
public class NewPmUserCmdlet : OrchestratorPSCmdlet
{
    // Key: (drive, groupIds) Value: Dictionary<email, csvLine>
    Dictionary<(OrchDriveInfo drive, string[] groupNames), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("UserName", "DestinationUserName")]
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
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        // Split GroupName specified in CSV by commas (PreservingEscapes so a backtick-escaped
        // metacharacter survives to ContainsWildcardCharacters / the literal branch below).
        var groupNameEnum = GroupName.SplitValuesByUnescapedCommasPreservingEscapes();

        _params ??= [];

        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            var groups = drive.PmGroups.Get();
            // Case sensitivity doesn't matter for existing group names, but we need to ignore case for newly created group names.
            HashSet<string> groupNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (var groupName in groupNameEnum ?? [])
            {
                // A backtick-escaped metacharacter (`* `? `[) is not a wildcard here: it falls to
                // the literal branch and is WildcardPattern.Unescape'd to its literal name. An
                // unescaped wildcard expands against existing groups. Get-PmUser exports group
                // names WildcardPattern.Escaped (EscapeCsvValue(..., true)), so both round-trip.
                // If the group name contains wildcards, expand them; otherwise keep it as-is.
                // If a group with the kept name doesn't exist, create it later.
                if (WildcardPattern.ContainsWildcardCharacters(groupName))
                {
                    var wpGroupName = new WildcardPattern(groupName, WildcardOptions.IgnoreCase);
                    var targetGroupNames = groups
                        .Where(g => !string.IsNullOrEmpty(g.name) && wpGroupName.IsMatch(g.name))
                        .Select(g => g.name!);
                    groupNames.UnionWith(targetGroupNames);
                }
                else
                {
                    groupNames.Add(WildcardPattern.Unescape(groupName));
                }
            }

            string[] orderedGroupNames = [.. groupNames.Order()]; // Sort so it can be used as a key

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

            // Get group IDs from group names
            List<string> groupIds = [];
            foreach (var groupName in groupNames)
            {
                var group = drive.PmGroups.Get().FirstOrDefault(g => g.name?.Equals(groupName, StringComparison.OrdinalIgnoreCase) ?? false);
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

                // Set the email value in both userName and email fields
                user.AssignStringIfNotNullOrEmpty(email, (u, v) => u.userName = v);
                user.AssignStringIfNotNullOrEmpty(email, (u, v) => u.email = v);
                user.AssignStringIfNotNullOrEmpty(line.name, (u, v) => u.name = v);
                user.AssignStringIfNotNullOrEmpty(line.surname, (u, v) => u.surname = v);
                user.AssignStringIfNotNullOrEmpty(line.displayName, (u, v) => u.displayName = v);
                user.AssignStringIfNotNullOrEmpty(line.type, (u, v) => u.type = v);

                payload.users.Add(user);
            }

            try
            {
                var response = drive.OrchAPISession.CreatePmUserBulk(payload);
                drive.PmUsers.ClearCache();
                drive.PmGroups.ClearCache();

                if (response?.result?.succeeded ?? false)
                {
                    WriteObject(response?.users?.OrderBy(u => u.name).Select(u => { var c = u.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }), true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "NewPmUserError", ErrorCategory.InvalidOperation, payload));
            }
        }
    }
}
