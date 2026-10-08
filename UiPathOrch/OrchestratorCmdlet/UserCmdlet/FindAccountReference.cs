using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// One place a Windows account appears in Orchestrator.
//   Type      User / Robot / Asset / Trigger
//   Property  where in the entity: UserName, UnattendedRobot.UserName, RobotProvision.UserName,
//             Username (classic robot), CredentialUsername, UserValues.CredentialUsername,
//             UserValues.UserName, MachineRobots, ExecutorRobots
//   Account   the value found there
//   Via       for the second-hop rows (a per-user asset value or a trigger that names a robot
//             rather than the account): the user or robot whose account matched
//   CredentialStore  for rows that hold a password: where it is kept. "Orchestrator Database"
//             means the password is stored in Orchestrator and has to be re-entered there
public class AccountReference
{
    public string? Path { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Property { get; set; }
    public string? Account { get; set; }
    public string? Via { get; set; }
    public string? CredentialStore { get; set; }
}

// Answers "where is this Windows account used?" before its password changes or it is retired
// (#jp-help-infra 2023-07-20). Looks at every entity that can hold an account, so the caller
// does not have to know the list: users (sign-in, unattended and attended robot), classic
// robots, credential assets (global and per-user values), and — one hop further — per-user
// asset values and triggers that point at a user or robot whose account matched.
[Cmdlet(VerbsCommon.Find, "OrchAccountReference")]
[OutputType(typeof(AccountReference))]
public class FindAccountReferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(AccountCompleter))]
    public string[] Account { get; set; } = default!;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    internal const string OrchestratorDatabase = "Orchestrator Database";

    // Lists the accounts the tenant entities hold (users and robots). Credential assets are
    // left out: listing them means reading every folder, too slow for a Tab.
    private class AccountCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);
            var wpExclude = CreateSelfExclusionList(commandAst, "Account", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => TenantAccounts(drive));
            foreach (var result in results)
            {
                foreach (var (account, where) in result
                    .Where(a => wp.IsMatch(a.account))
                    .ExcludeByWildcards(a => a.account, wpExclude)
                    .DistinctBy(a => a.account, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(a => a.account, StringComparer.OrdinalIgnoreCase))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(account), account, CompletionResultType.ParameterValue, where);
                }
            }
        }

        private static IEnumerable<(string account, string where)> TenantAccounts(OrchDriveInfo drive)
        {
            var list = new List<(string, string)>();
            foreach (var user in drive.Users.Get())
            {
                string where = user.GetPSPath();
                if (UserSignIn(user) is string signIn) list.Add((signIn, where));
                if (!string.IsNullOrEmpty(user.UnattendedRobot?.UserName)) list.Add((user.UnattendedRobot.UserName, $"{where} (unattended robot)"));
                if (!string.IsNullOrEmpty(user.RobotProvision?.UserName)) list.Add((user.RobotProvision.UserName, $"{where} (attended robot)"));
            }
            foreach (var robot in drive.Robots.Get())
            {
                if (!string.IsNullOrEmpty(robot.Username)) list.Add((robot.Username, $"{drive.NameColonSeparator}{robot.Name} (robot)"));
            }
            return list;
        }
    }

    protected override void ProcessRecord()
    {
        var matcher = new AccountMatcher(Account);
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        // Tenant entities first, once per drive: they also tell the folder pass which users
        // and robots the account belongs to.
        var tenantMatches = new Dictionary<OrchDriveInfo, TenantMatch>();
        foreach (var drive in drivesFolders.Select(df => df.drive).Distinct())
        {
            try
            {
                var match = ScanTenant(drive, matcher);
                tenantMatches[drive] = match;
                WriteObject(match.References, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "FindAccountReferenceError", ErrorCategory.InvalidOperation, drive));
            }
        }

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => (assets: df.drive.Assets.Get(df.folder), triggers: df.drive.Triggers.Get(df.folder)));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, "Finding account references");
        foreach (var result in results)
        {
            try
            {
                var (assets, triggers) = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                var drive = result.Source.drive;
                if (!tenantMatches.TryGetValue(drive, out var tenant)) continue;

                if (assets is not null)
                {
                    WriteObject(ScanAssets(assets, matcher, tenant), true);
                }
                if (triggers is not null)
                {
                    WriteObject(ScanTriggers(triggers, tenant), true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "FindAccountReferenceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }

    // What the tenant pass found: its rows, plus the users and robots whose account matched,
    // which the folder pass follows into per-user asset values and triggers.
    internal class TenantMatch
    {
        public List<AccountReference> References { get; } = [];
        public Dictionary<string, string> UserNames { get; } = new(StringComparer.OrdinalIgnoreCase);   // user name -> matched account
        // Robot id -> (robot name, matched account). Triggers name their robots by id: the
        // trigger list leaves MachineRobots[].RobotUserName empty (25.10, 2026-10-08).
        public Dictionary<long, (string? name, string account)> Robots { get; } = [];
        public Dictionary<long, string> CredentialStoreNames { get; } = [];
    }

    private static TenantMatch ScanTenant(OrchDriveInfo drive, AccountMatcher matcher)
    {
        ICollection<CredentialStore>? stores = null;
        try { stores = drive.CredentialStores.Get(); }
        catch (Exception) { } // without the permission the store column stays empty; the rows still come out

        return ScanTenant(drive.Users.Get(), drive.Robots.Get(), stores, matcher, drive.NameColonSeparator);
    }

    internal static TenantMatch ScanTenant(IEnumerable<User> users, IEnumerable<Robot> robots,
        IEnumerable<CredentialStore>? stores, AccountMatcher matcher, string drivePath)
    {
        var match = new TenantMatch();
        foreach (var store in stores ?? [])
        {
            if (store.Id is long id && store.Name is not null) match.CredentialStoreNames[id] = store.Name;
        }

        var userIds = new HashSet<long>();
        foreach (var user in users.OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase))
        {
            if (user.Id is long uid) userIds.Add(uid);
            string path = user.GetPSPath();

            string? signIn = UserSignIn(user);
            if (matcher.IsMatch(signIn))
            {
                match.References.Add(new() { Path = path, Type = "User", Name = user.UserName, Property = "UserName", Account = signIn });
                Remember(match.UserNames, user.UserName, signIn);
            }

            var ur = user.UnattendedRobot;
            if (matcher.IsMatch(ur?.UserName))
            {
                match.References.Add(new()
                {
                    Path = path, Type = "User", Name = user.UserName, Property = "UnattendedRobot.UserName", Account = ur!.UserName,
                    CredentialStore = StoreName(match, ur.CredentialStoreId),
                });
                Remember(match.UserNames, user.UserName, ur.UserName);
            }

            if (matcher.IsMatch(user.RobotProvision?.UserName))
            {
                match.References.Add(new() { Path = path, Type = "User", Name = user.UserName, Property = "RobotProvision.UserName", Account = user.RobotProvision!.UserName });
                Remember(match.UserNames, user.UserName, user.RobotProvision.UserName);
            }
        }

        foreach (var robot in robots.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!matcher.IsMatch(robot.Username)) continue;
            if (robot.Id is long rid) match.Robots.TryAdd(rid, (robot.Name, robot.Username!));

            // A modern-folder robot is the user's own robot, already reported on the user row.
            if (robot.UserId is long uid && userIds.Contains(uid)) continue;

            match.References.Add(new()
            {
                Path = System.IO.Path.Combine(drivePath, robot.Name ?? ""), Type = "Robot", Name = robot.Name, Property = "Username", Account = robot.Username,
                CredentialStore = StoreName(match, robot.CredentialStoreId),
            });
        }

        // The robot of a matched user is followed too: a user matched by its sign-in runs its
        // robot under another account, but a trigger on that robot still depends on the user.
        foreach (var robot in robots)
        {
            if (robot.Id is not long rid || match.Robots.ContainsKey(rid)) continue;
            string? owner = robot.User?.UserName ?? users.FirstOrDefault(u => u.Id is not null && u.Id == robot.UserId)?.UserName;
            if (owner is not null && match.UserNames.TryGetValue(owner, out var account))
            {
                match.Robots[rid] = (robot.Name, account);
            }
        }

        return match;
    }

    internal static IEnumerable<AccountReference> ScanAssets(IEnumerable<Asset> assets, AccountMatcher matcher, TenantMatch tenant)
    {
        foreach (var asset in assets.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            string path = asset.GetPSPath();

            if (asset.ValueType == "Credential" && matcher.IsMatch(asset.CredentialUsername))
            {
                yield return new()
                {
                    Path = path, Type = "Asset", Name = asset.Name, Property = "CredentialUsername", Account = asset.CredentialUsername,
                    CredentialStore = StoreName(tenant, asset.CredentialStoreId),
                };
            }

            foreach (var uv in asset.UserValues ?? [])
            {
                string owner = uv.UserName ?? uv.MachineName ?? "";
                if (uv.ValueType == "Credential" && matcher.IsMatch(uv.CredentialUsername))
                {
                    yield return new()
                    {
                        Path = path, Type = "Asset", Name = asset.Name, Property = "UserValues.CredentialUsername", Account = uv.CredentialUsername,
                        Via = owner, CredentialStore = StoreName(tenant, uv.CredentialStoreId),
                    };
                }
                else if (uv.UserName is not null && tenant.UserNames.TryGetValue(uv.UserName, out var account))
                {
                    // The value is not the account itself but belongs to a user whose account
                    // matched: the robot reads it while running as that account.
                    yield return new() { Path = path, Type = "Asset", Name = asset.Name, Property = "UserValues.UserName", Account = account, Via = uv.UserName };
                }
            }
        }
    }

    internal static IEnumerable<AccountReference> ScanTriggers(IEnumerable<ProcessSchedule> triggers, TenantMatch tenant)
    {
        foreach (var trigger in triggers.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            string path = trigger.GetPSPath();

            foreach (var mr in trigger.MachineRobots ?? [])
            {
                if (mr.RobotId is long rid && tenant.Robots.TryGetValue(rid, out var robot))
                {
                    yield return new() { Path = path, Type = "Trigger", Name = trigger.Name, Property = "MachineRobots", Account = robot.account, Via = robot.name };
                }
            }

            foreach (var er in trigger.ExecutorRobots ?? [])
            {
                if (er.Id is long rid && tenant.Robots.TryGetValue(rid, out var robot))
                {
                    yield return new() { Path = path, Type = "Trigger", Name = trigger.Name, Property = "ExecutorRobots", Account = robot.account, Via = robot.name };
                }
            }
        }
    }

    // The account a user signs in with: DOMAIN\name for a directory user that carries its
    // domain, the plain user name otherwise (Cloud users carry their e-mail form there).
    internal static string? UserSignIn(User user)
    {
        if (string.IsNullOrEmpty(user.UserName)) return null;
        return string.IsNullOrEmpty(user.Domain) ? user.UserName : $"{user.Domain}\\{user.UserName}";
    }

    private static void Remember(Dictionary<string, string> map, string? name, string? account)
    {
        if (name is not null && account is not null) map.TryAdd(name, account);
    }

    // Only a password-bearing row gets a store: the default store is Orchestrator's own
    // database, any other id is an external vault.
    private static string StoreName(TenantMatch tenant, long? storeId)
    {
        if (storeId is long id && tenant.CredentialStoreNames.TryGetValue(id, out var name)) return name;
        return storeId is null ? OrchestratorDatabase : $"#{storeId}";
    }
}

// Matches the -Account patterns against the account strings Orchestrator stores, which come in
// three spellings: DOMAIN\name, name@domain and a bare name. A pattern that names a domain
// (has '\' or '@') is matched against the whole stored string; a bare pattern is matched against
// the name part only, so "svc_rpa" finds CORP\svc_rpa, svc_rpa@corp.example.com and svc_rpa
// alike — the safe default before a password change, where a missed place is the costly error.
internal class AccountMatcher
{
    private readonly List<(WildcardPattern pattern, bool qualified)> _patterns;

    public AccountMatcher(IEnumerable<string> patterns)
    {
        _patterns = patterns
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Select(p => (WildcardPattern.Get(p, WildcardOptions.IgnoreCase), p.Contains('\\') || p.Contains('@')))
            .ToList();
    }

    public bool IsMatch(string? account)
    {
        if (string.IsNullOrWhiteSpace(account)) return false;
        account = account.Trim();
        string name = NamePart(account);
        foreach (var (pattern, qualified) in _patterns)
        {
            if (pattern.IsMatch(qualified ? account : name)) return true;
        }
        return false;
    }

    internal static string NamePart(string account)
    {
        int bs = account.LastIndexOf('\\');
        if (bs >= 0) return account[(bs + 1)..];
        int at = account.IndexOf('@');
        return at > 0 ? account[..at] : account;
    }
}
