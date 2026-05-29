using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// Removes one or more user license bundles from a Platform Management user.
// Mirror of Add-PmLicenseToPmLicensedUser: re-uses the same atomic-replace PUT
// (PutPmLicenseUser) but submits the user's existing userBundleLicenses MINUS
// the matched codes. `-License *` strips every bundle the user currently holds
// — whether that also drops the user from the licensed-users set or leaves an
// empty record is an API-side observation (silent emit on the empty result
// either way, matching Add's contract).
[Cmdlet(VerbsCommon.Remove, "PmLicenseFromPmLicensedUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.NuLicensedUser))]
public class RemovePmLicenseFromPmLicensedUserCmdlet : OrchestratorPSCmdlet
{
    // Per (drive, userId): the existing licensed-user record (always present —
    // we error early if the target isn't in the licensed-users set), the bundle
    // codes to remove (intersection of -License wildcards with current bundles),
    // and the friendly label for ShouldProcess / errors.
    private Dictionary<(OrchDriveInfo drive, string userId),
        (NuLicensedUser existing, HashSet<string> codesToRemove, string displayName)>? _parameterSets;

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

    // Completes from the licensed-users set only — these are the only candidates
    // that can have anything removed.
    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var drive in drives)
            {
                foreach (var u in drive.PmLicensedUsers.Get()
                    .Where(x => wp.IsMatch(x?.email) || wp.IsMatch(x?.name))
                    .ExcludeByWildcards(x => x?.email, wpUserName)
                    .OrderBy(x => x.email))
                {
                    string label = u.email ?? u.name ?? "";
                    string tiphelp = u.userBundleLicenseNames is { Length: > 0 }
                        ? $"{drive.NameColonSeparator}{label} — {string.Join(", ", u.userBundleLicenseNames)}"
                        : $"{drive.NameColonSeparator}{label}";
                    yield return new CompletionResult(PathTools.EscapePSText(label), label, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    // Completes from the bundles the specified user actually holds (intersected
    // with the friendly-name catalog), so "removable" candidates are visible.
    // Falls back to a hint when -Email isn't bound yet.
    private class LicenseCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var emails = GetFakeBoundParameters(fakeBoundParameters, "Email");
            if (emails is null || !emails.Any())
            {
                yield return new CompletionResult($@"""(Provide -Email first to list removable bundles)""");
                yield break;
            }

            var drives = ResolvePmDrives(fakeBoundParameters);
            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var drive in drives)
            {
                var licensedUsers = drive.PmLicensedUsers.Get();
                foreach (var email in emails)
                {
                    var u = licensedUsers.FirstOrDefault(lu =>
                        string.Equals(lu.email, email, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(lu.name, email, StringComparison.OrdinalIgnoreCase));
                    if (u?.userBundleLicenses is null) continue;

                    foreach (var code in u.userBundleLicenses
                        .Where(c =>
                        {
                            var friendly = AvailableUserBundlesItems.Items.TryGetValue(c, out var n) ? n : c;
                            return wp.IsMatch(friendly) || wp.IsMatch(c);
                        })
                        .OrderBy(c => AvailableUserBundlesItems.Items.TryGetValue(c, out var n) ? n : c))
                    {
                        string name = AvailableUserBundlesItems.Items.TryGetValue(code, out var n2) ? n2 : code;
                        if (wpLicense?.Any(p => p.IsMatch(name)) == true) continue;
                        string tiphelp = $"{name} ({code})";
                        yield return new CompletionResult(PathTools.EscapePSText(name), name, CompletionResultType.Text, tiphelp);
                    }
                }
            }
        }
    }

    // Resolves -License wildcards against the user's CURRENT bundles (not the
    // whole catalog), so `-License *` strips exactly what's held and a pattern
    // that doesn't match anything held is silently a no-op (matches Add's
    // contract for non-matching patterns). A pattern matches a code if it
    // matches the friendly name (via AvailableUserBundlesItems) OR the raw code.
    // Pure / static for unit testing.
    internal static HashSet<string> ResolveLicenseCodesToRemove(
        IEnumerable<WildcardPattern>? wpLicense,
        IEnumerable<string>? currentBundles)
    {
        var codes = new HashSet<string>();
        if (wpLicense is null || currentBundles is null) return codes;
        foreach (var code in currentBundles)
        {
            string? friendly = AvailableUserBundlesItems.Items.TryGetValue(code, out var n) ? n : null;
            foreach (var wp in wpLicense)
            {
                if (wp.IsMatch(code) || (friendly is not null && wp.IsMatch(friendly)))
                {
                    codes.Add(code);
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

        var drives = SessionState.EnumPmDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var existingLicensedUsers = drive.PmLicensedUsers.Get();

            foreach (var email in (Email ?? []).WithCancellation(cancelHandler.Token))
            {
                // Only operate on users that are in the licensed-users set —
                // there's nothing to remove from someone who isn't licensed.
                var targetUser = existingLicensedUsers.FirstOrDefault(e =>
                    string.Compare(e.name, email, StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(e.email, email, StringComparison.OrdinalIgnoreCase) == 0);

                if (targetUser is null)
                {
                    WriteError(new ErrorRecord(
                        new OrchException(drive.NameColonSeparator,
                            new Exception($"User '{email}' is not a licensed user on {drive.NameColonSeparator}.")),
                        "UserNotLicensedError", ErrorCategory.ObjectNotFound, email));
                    continue;
                }

                var codesToRemove = ResolveLicenseCodesToRemove(wpLicense, targetUser.userBundleLicenses);
                if (codesToRemove.Count == 0)
                {
                    // Pattern matched no held bundle — silent skip.
                    continue;
                }

                string userId = targetUser.id!;
                string displayName = targetUser.name ?? targetUser.email ?? email;

                if (!_parameterSets.TryGetValue((drive, userId), out var entry))
                {
                    entry = (targetUser, [], displayName);
                    _parameterSets[(drive, userId)] = entry;
                }
                entry.codesToRemove.UnionWith(codesToRemove);
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
            var (existing, codesToRemove, displayName) = kvp.Value;

            // Final code set = current MINUS removed (atomic-replace PUT).
            var existingSet = new HashSet<string>(existing.userBundleLicenses ?? []);
            int initialCount = existingSet.Count;
            existingSet.ExceptWith(codesToRemove);
            if (existingSet.Count == initialCount) continue; // nothing actually removed

            string target = $"{drive.NameColonSeparator}{displayName}";
            if (!ShouldProcess(target, "Remove License from PmLicensedUser")) continue;

            try
            {
                UpdateLicensedUserCommand cmd = new()
                {
                    userIds = [userId],
                    licenseCodes = existingSet.ToArray(),
                    // NuLicensedUser does not surface useExternalLicense; matches
                    // Add's default (verified from the Portal capture).
                    useExternalLicense = false,
                };
                drive.OrchAPISession.PutPmLicenseUser(cmd);

                drive.PmLicensedUsers.ClearCache();
                drive.PmAvailableUserBundles.ClearCache();

                // After a successful remove the user record MAY disappear from
                // the licensed-users set when its bundle list ends up empty
                // (an API-side observation). Emit only if still present —
                // matches Add's silent-when-absent contract.
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
                    "RemovePmLicenseFromPmLicensedUserError", ErrorCategory.InvalidOperation, target));
                continue;
            }
        }
    }
}
