using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Pure decision helpers behind Copy-PmUser / Copy-PmRobotAccount, extracted onto
// OrchProvider (alongside ResolveDstUserPure) so the cross-org copy safeguards can
// be unit-tested without a live drive. Behaviour is preserved verbatim from the
// inline code; the cmdlets call these.
public partial class OrchProvider
{
    // Copy-PmUser: does the source user lack an email? Used with MustSkipEmaillessUser
    // to decide whether an email-less user can be created at the destination — only
    // Automation Cloud requires an email (Automation Suite / on-premises accept a
    // userName-only local user).
    internal static bool HasNoEmail(PmUser? user) => string.IsNullOrEmpty(user?.email);

    // Copy-PmUser: an email-less source user can only be created where the destination
    // doesn't require an email. Automation Cloud (preserveUserName == false) keys local
    // users by email and rejects creation without one (users are invite-based), so it
    // must be skipped there. Automation Suite / on-premises use the userName as the
    // identifier and accept an empty email (verified on-prem 25.10.2: BulkCreate creates
    // a userName-only user), so such a user is migrated rather than dropped.
    internal static bool MustSkipEmaillessUser(bool preserveUserName, PmUser? user) =>
        !preserveUserName && HasNoEmail(user);

    // Copy-PmUser: resolve the userName to create the destination user with.
    // Default preserves the source userName so a local user keeps its identity,
    // falling back to the email only when there is no userName. On Automation Cloud
    // (preserveUserName == false) the identifier is the email, so the historical
    // email-as-userName is kept and Cloud migrations don't regress. A UserMappingCsv
    // entry overrides either default and is tried FIRST: it may be keyed by the source
    // userName (matching the SourceUserName column and the sibling copy cmdlets) or,
    // for backward compatibility with earlier email-keyed sheets, by the email —
    // userName is looked up first, then email. An empty mapped value is ignored (falls
    // through to the default) so a blank cell never blanks the created userName.
    internal static string ResolvePmUserName(
        string? srcUserName, string? srcEmail, bool preserveUserName,
        IReadOnlyDictionary<string, string>? userMapping)
    {
        if (userMapping is not null)
        {
            if (!string.IsNullOrEmpty(srcUserName) &&
                userMapping.TryGetValue(srcUserName, out var byName) && !string.IsNullOrEmpty(byName))
            {
                return byName;
            }
            if (!string.IsNullOrEmpty(srcEmail) &&
                userMapping.TryGetValue(srcEmail, out var byEmail) && !string.IsNullOrEmpty(byEmail))
            {
                return byEmail;
            }
        }

        return preserveUserName
            ? (!string.IsNullOrEmpty(srcUserName) ? srcUserName : (srcEmail ?? ""))
            : (!string.IsNullOrEmpty(srcEmail) ? srcEmail : (srcUserName ?? ""));
    }

    // New-PmUser: resolve (userName, email) from the -UserName / -Email parameters.
    // userName is the identifier and defaults to the email (always so on Automation
    // Cloud, and the long-standing single-parameter behaviour); email defaults to the
    // userName when only a userName is supplied. Either input alone therefore yields
    // userName == email (backward compatible with the old -Email/-UserName-alias
    // parameter), while supplying BOTH lets a local user keep a userName distinct from
    // its email on Automation Suite / on-premises. userName is empty only when both
    // inputs are empty, which the caller rejects.
    internal static (string userName, string email) ResolveNewPmUserIdentity(string? userName, string? email)
    {
        string effUserName = !string.IsNullOrEmpty(userName) ? userName : (email ?? "");
        string effEmail = !string.IsNullOrEmpty(email) ? email : (userName ?? "");
        return (effUserName, effEmail);
    }

    // Copy-PmUser: build the destination "already taken" lookup defensively. A
    // plain ToDictionary(u => u.email) throws "an item with the same key has
    // already been added" when the destination has 2+ users sharing a key (in
    // practice empty-email entries -> key ""). Drop empty emails and collapse
    // duplicates so the lookup never throws and the duplicate guard works.
    internal static Dictionary<string, PmUser> BuildDstUserLookup(IEnumerable<PmUser> users) =>
        users
            .Where(u => !string.IsNullOrEmpty(u?.email))
            .GroupBy(u => u!.email!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    // Copy-PmUser: BulkCreate can refuse users without throwing. Treat only an
    // explicit succeeded == false as a failure to surface; a null result is
    // API-shape variance and must NOT raise a false alarm.
    internal static bool IsBulkCreateFailure(BulkCreateResponse? response) =>
        response?.result?.succeeded == false;

    // Copy-PmRobotAccount: existing destination robot-account names, used to skip
    // an account that already exists instead of surfacing a raw server conflict.
    // Empty/null names are dropped (they can't be a meaningful match) and the set
    // is case-insensitive; this never throws on duplicates.
    internal static HashSet<string> BuildExistingRobotNameSet(IEnumerable<PmRobotAccount> robots) =>
        robots
            .Where(r => !string.IsNullOrEmpty(r?.name))
            .Select(r => r!.name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Copy-PmRobotAccount: is this source account already present at the
    // destination? Null/empty name -> false (and never throws on Contains(null)).
    internal static bool RobotNameAlreadyExists(HashSet<string> existingNames, string? name) =>
        !string.IsNullOrEmpty(name) && existingNames.Contains(name);
}
