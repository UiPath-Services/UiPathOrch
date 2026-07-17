using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

class CsvLine(string? email, string? name, string? surname, string? displayName, string? type, string? bypassBasicAuthRestriction, string? invitationAccepted)
{
    // id is generated with new Guid() at API call time.
    // The Dictionary key is the userName (the identifier); the email is carried here
    // because on Automation Suite / on-premises a local user's email can differ from
    // the userName (it equals the userName on Automation Cloud and whenever no separate
    // -UserName was given).
    public string? email { get; set; } = email;
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
    // Key: (drive, groupIds) Value: Dictionary<userName, csvLine>
    Dictionary<(OrchDriveInfo drive, string[] groupNames), Dictionary<string, CsvLine>> _params = new(new DriveGroupIdsComparer());

    // The email address. On Automation Cloud this is also the identifier (userName ==
    // email). Optional so a userName-only call is possible; at least one of -Email /
    // -UserName must be supplied.
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    public string? Email { get; set; }

    // The login name / identifier. On Automation Suite and on-premises a local user's
    // userName can differ from the email; when omitted it defaults to the email (the
    // long-standing behaviour, and what Automation Cloud requires). DestinationUserName
    // is an alias so a user-mapping row's target name binds straight to it.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("DestinationUserName")]
    public string? UserName { get; set; }

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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        // Split GroupName specified in CSV by commas (PreservingEscapes so a backtick-escaped
        // metacharacter survives to ContainsWildcardCharacters / the literal branch below).
        var groupNameEnum = GroupName.SplitValuesByUnescapedCommasPreservingEscapes();

        _params ??= [];

        // userName is the identifier and defaults to the email; email defaults to the
        // userName only when it is email-shaped — a bare non-address -UserName (e.g.
        // "admin") creates a userName-only local user with an EMPTY email instead of an
        // invalid one. Require at least one. See ResolveNewPmUserIdentity.
        var (effUserName, effEmail) = Core.OrchProvider.ResolveNewPmUserIdentity(UserName, Email);
        if (string.IsNullOrEmpty(effUserName))
        {
            WriteError(new ErrorRecord(
                new PSArgumentException("Specify -UserName or -Email (at least one is required)."),
                "NewPmUserMissingIdentity", ErrorCategory.InvalidArgument, null));
            return;
        }

        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        foreach (var drive in drives)
        {
            // An email-less user is first-class on Automation Suite / on-premises (sign-in is
            // userName + password), but Automation Cloud signs users in by email — the create
            // succeeds there, yet the account cannot sign in. The caller asked for it
            // explicitly, so create it anyway and say what it implies (Copy-PmUser skips the
            // same case, because a migration must not silently produce unusable accounts).
            if (string.IsNullOrEmpty(effEmail) && drive._psDrive.ResolvedEdition == Core.OrchEdition.Cloud)
            {
                WriteWarning($"{drive.NameColonSeparator}{effUserName}: creating without an email address. Automation Cloud signs users in by email, so this account will not be able to sign in until one is added (Update-PmUser -NewEmail).");
            }

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

            string target = System.IO.Path.Combine(drive.NameColonSeparator, effUserName);
            if (_params.TryGetValue((drive, orderedGroupNames)!, out var userName_line))
            {
                if (userName_line.TryGetValue(effUserName, out var line))
                {
                    WriteWarning($"{drive.NameColonSeparator}{effUserName}: duplicate entry found. This entry will be ignored.");
                    continue;
                }
                if (ShouldProcess(target, "New PmUser"))
                {
                    line = new(effEmail, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                    userName_line[effUserName] = line;
                }
            }
            else
            {
                if (ShouldProcess(target, "New PmUser"))
                {
                    userName_line = [];
                    CsvLine line = new(effEmail, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted);
                    userName_line[effUserName] = line;
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
                var userName = userName_line.Key;
                var line = userName_line.Value;
                var user = new CreateUserCommandBase()
                {
                    id = Guid.NewGuid().ToString(),
                    bypassBasicAuthRestriction = line.bypassBasicAuthRestriction,
                    legacyId = null,
                    invitationAccepted = line.invitationAccepted
                };

                // userName is the dictionary key (the identifier); email is carried on
                // the line and may differ on AS / on-prem (it equals userName on Cloud
                // and whenever -UserName was omitted).
                user.AssignStringIfNotNullOrEmpty(userName, (u, v) => u.userName = v);
                user.AssignStringIfNotNullOrEmpty(line.email, (u, v) => u.email = v);
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
