using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// Allocates one or more user license bundles to a Platform Management user.
// Mirrors the shape of Add-PmLicenseToPmLicensedGroup (its group counterpart):
// ProcessRecord collects target bundle codes per user across pipeline rows; the
// actual PUT happens in EndProcessing so multiple licenses for the same user
// fold into a single request. The PUT is atomic-replace (matches the Portal
// "Licenses > Users" UI), so we merge the user's existing
// userBundleLicenses with the new codes before submitting.
[Cmdlet(VerbsCommon.Add, "PmLicenseToPmLicensedUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.NuLicensedUser))]
public class AddPmLicenseToPmLicensedUserCmdlet : OrchestratorPSCmdlet
{
    // Per (drive, userId): the pre-existing licensed-user record (if any), the
    // bundle codes to add, and a friendly label used for ShouldProcess targets
    // and error messages. The HashSet is shared by reference across pipeline
    // rows touching the same user.
    private Dictionary<(OrchDriveInfo drive, string userId),
        (NuLicensedUser? existing, HashSet<string> codes, string displayName)>? _parameterSets;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LicenseCompleter))]
    [SupportsWildcards]
    public string[]? License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude user names already selected via parameters from candidates
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var users = drive.SearchDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    .Where(u => u.type == 0)
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .OrderBy(e => e.identityName))
                {
                    bFound = true;
                    string tiphelp = TipHelp(user);
                    yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{wordToComplete}')""");
            }
        }
    }

    private class LicenseCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Suggest friendly bundle names from the static catalog (e.g.
            // "Attended - Named User"). The server validates the resolved codes
            // on PUT, so an org that doesn't own a bundle still gets a useful
            // error rather than a misleading "no candidates" silence here. The
            // raw code is shown in the tooltip for users who prefer codes.
            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            // Self-exclusion is inlined (not via ExcludeByWildcards) because the
            // catalog is a Dictionary<string,string> — iterating yields the
            // KeyValuePair *struct*, and ExcludeByWildcards<T>'s Func<T?,_>
            // selector turns kv into Nullable<KeyValuePair>, which makes `.Value`
            // resolve to Nullable<T>.Value instead of the dict entry's value.
            foreach (var kv in AvailableUserBundlesItems.Items
                .Where(kv => wp.IsMatch(kv.Value) || wp.IsMatch(kv.Key))
                .OrderBy(kv => kv.Value))
            {
                if (wpLicense?.Any(p => p.IsMatch(kv.Value)) == true) continue;
                string name = kv.Value;
                string tiphelp = $"{name} ({kv.Key})";
                yield return new CompletionResult(PathTools.EscapePSText(name), name, CompletionResultType.Text, tiphelp);
            }
        }
    }

    // Resolves -License wildcards against the static bundle catalog. A pattern
    // matches a bundle if it matches the friendly name OR the raw code, so
    // users can pass either form. Pure / static for unit testing.
    internal static HashSet<string> ResolveLicenseCodes(IEnumerable<WildcardPattern>? wpLicense)
    {
        var codes = new HashSet<string>();
        if (wpLicense is null) return codes;
        foreach (var kv in AvailableUserBundlesItems.Items)
        {
            foreach (var wp in wpLicense)
            {
                if (wp.IsMatch(kv.Value) || wp.IsMatch(kv.Key))
                {
                    codes.Add(kv.Key);
                    break;
                }
            }
        }
        return codes;
    }

    protected override void ProcessRecord()
    {
        _parameterSets ??= [];

        Email = Email.Split1stValueByUnescapedCommas()?.ToArray();
        var wpLicense = License.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();
        var targetCodes = ResolveLicenseCodes(wpLicense);
        if (targetCodes.Count == 0)
        {
            // No catalog bundle matched the License pattern; nothing to allocate
            // on this row. Matches Add-PmLicenseToPmLicensedGroup's silent-skip
            // behavior when its wildcard matches no available bundle.
            return;
        }

        var drives = SessionState.EnumPmDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var existingLicensedUsers = drive.PmLicensedUsers.Get();

            foreach (var email in (Email ?? []).WithCancellation(cancelHandler.Token))
            {
                // Already in the licensed-users set? Reuse its record so we can
                // merge bundles in EndProcessing. Match on both 'name' and
                // 'email' since the input parameter accepts either form.
                var targetUser = existingLicensedUsers.FirstOrDefault(e =>
                    string.Compare(e.name, email, StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(e.email, email, StringComparison.OrdinalIgnoreCase) == 0);

                string? userId;
                string displayName;
                if (targetUser is not null)
                {
                    userId = targetUser.id;
                    displayName = targetUser.name ?? targetUser.email ?? email;
                }
                else
                {
                    // Not yet a licensed user — look the principal up in the
                    // directory to get an identifier for the PUT. PutPmLicenseUser
                    // covers both add + bundle-assignment in one round trip.
                    var resolvedUser = drive.PmBulkResolveByName("user", [email], e => e, null);
                    if (resolvedUser is null || !resolvedUser.Any())
                    {
                        WriteError(new ErrorRecord(
                            new OrchException(drive.NameColonSeparator,
                                new Exception($"User '{email}' was not found in the directory on {drive.NameColonSeparator}.")),
                            "UserNotFoundError", ErrorCategory.ObjectNotFound, email));
                        continue;
                    }
                    userId = resolvedUser.First().Value!.identifier!;
                    displayName = email;
                }

                if (string.IsNullOrEmpty(userId)) continue;

                if (!_parameterSets.TryGetValue((drive, userId), out var entry))
                {
                    entry = (targetUser, [], displayName);
                    _parameterSets[(drive, userId)] = entry;
                }
                entry.codes.UnionWith(targetCodes);
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_parameterSets is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var kvp in _parameterSets
            .OrderBy(p => p.Key.drive.Name)
            .ThenBy(p => p.Value.displayName, StringComparer.OrdinalIgnoreCase)
            .WithCancellation(cancelHandler.Token))
        {
            var (drive, userId) = kvp.Key;
            var (existing, codesToAdd, displayName) = kvp.Value;

            // PUT is atomic-replace, so merge the user's pre-existing bundles
            // with the new codes before submitting. If nothing actually changes,
            // skip the round trip (matches the group cmdlet's contract).
            var existingSet = new HashSet<string>(existing?.userBundleLicenses ?? []);
            int initialCount = existingSet.Count;
            existingSet.UnionWith(codesToAdd);
            if (existingSet.Count == initialCount) continue;

            string target = $"{drive.NameColonSeparator}{displayName}";
            if (!ShouldProcess(target, "Add License to PmUser")) continue;

            try
            {
                UpdateLicensedUserCommand cmd = new()
                {
                    userIds = [userId],
                    licenseCodes = existingSet.ToArray(),
                    // NuLicensedUser does not surface useExternalLicense; the
                    // Portal request defaults to false (verified from capture).
                    useExternalLicense = false,
                };
                drive.OrchAPISession.PutPmLicenseUser(cmd);

                // Bundle allocations and the licensed-users list are both stale
                // after the PUT. Drop both caches so the refresh below (and any
                // subsequent cmdlet) sees fresh data.
                drive.PmLicensedUsers.ClearCache();
                drive.PmAvailableUserBundles.ClearCache();

                // Emit the post-PUT view of the user with friendly bundle
                // labels stamped on a per-emit ShallowClone (matches the
                // ShallowClone path-isolation convention in this codebase).
                var refreshed = drive.PmLicensedUsers.Get()
                    .FirstOrDefault(u => u.id == userId);
                if (refreshed is not null)
                {
                    var clone = refreshed.ShallowClone();
                    clone.Path = drive.NameColonSeparator;
                    clone.userBundleLicenseNames = clone.userBundleLicenses
                        ?.Select(b => AvailableUserBundlesItems.Items.TryGetValue(b, out var n) ? n : b)
                        .ToArray();
                    WriteObject(clone);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new OrchException(target, ex),
                    "AddPmLicenseToPmLicensedUserError", ErrorCategory.InvalidOperation, target));
                continue;
            }
        }
    }
}
