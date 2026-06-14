using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Drops one or more users from the Platform Management licensed-users set
// (the "License Allocations to Users" table in the Portal UI), including
// the empty-bundle "No license" rows left behind by
// Remove-PmUserLicense <user> *. Wraps the bare DELETE endpoint
// /portal_/api/license/accountant/UserLicense with body {userIds:[...]}.
//
// The DELETE accepts a batched userIds array, so this cmdlet collects matching
// users across pipeline rows and -Email wildcards and sends ONE DELETE per
// drive in EndProcessing — N users = 1 round trip, not N. ShouldProcess is
// still per-user, so -WhatIf / -Confirm prompt granularly.
[Cmdlet(VerbsCommon.Remove, "PmLicensedUser", SupportsShouldProcess = true)]
public class RemovePmLicensedUserCmdlet : OrchestratorPSCmdlet
{
    // Per drive: distinct users approved for removal (deduplicated by id).
    private Dictionary<OrchDriveInfo, Dictionary<string, NuLicensedUser>>? _toRemove;

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Completes from the licensed-users set on each Pm drive. The tooltip
    // surfaces what would be removed (current bundle labels, or "No license"
    // for an empty-bundles row) so the user can see consequences before
    // confirming.
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
                    string bundleHint = (u.userBundleLicenseNames is { Length: > 0 })
                        ? string.Join(", ", u.userBundleLicenseNames)
                        : "No license";
                    string tiphelp = $"{drive.NameColonSeparator}{label} — {bundleHint}";
                    yield return new CompletionResult(PathTools.EscapePSText(label), label, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _toRemove ??= [];

        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<NuLicensedUser> matched;
            try
            {
                // Filter the licensed-users list by -Email wildcards. Matching
                // is OR over email and name (mirrors how the licensed-users
                // table identifies a row in the UI). When -Email is omitted
                // entirely we match every licensed user, paralleling
                // Remove-PmLicensedGroup's behavior without -GroupName.
                matched = drive.PmLicensedUsers.Get()
                    .FilterByNamesAny([u => u?.email, u => u?.name], Email)
                    .OrderBy(u => u.email);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex),
                    "GetPmLicensedUserError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            if (!_toRemove.TryGetValue(drive, out var bucket))
            {
                bucket = [];
                _toRemove[drive] = bucket;
            }
            foreach (var u in matched.WithCancellation(cancelHandler.Token))
            {
                if (u.id is null) continue;
                bucket.TryAdd(u.id, u); // dedup by user id across pipeline rows
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_toRemove is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, bucket) in _toRemove.OrderBy(kv => kv.Key.Name))
        {
            // Per-user ShouldProcess so -WhatIf prints one row per user and
            // -Confirm prompts can approve/deny individually. Collected
            // approvals are then sent as one batched DELETE per drive.
            var approved = new List<NuLicensedUser>();
            foreach (var u in bucket.Values
                .OrderBy(x => x.email, StringComparer.OrdinalIgnoreCase)
                .WithCancellation(cancelHandler.Token))
            {
                string target = $"{drive.NameColonSeparator}{u.email ?? u.name ?? u.id}";
                if (ShouldProcess(target, "Remove PmLicensedUser"))
                {
                    approved.Add(u);
                }
            }

            if (approved.Count == 0) continue;

            try
            {
                drive.OrchAPISession.RemovePmLicensedUser(approved.Select(u => u.id!).ToArray());
                drive.PmLicensedUsers.ClearCache();
                drive.PmAvailableUserBundles.ClearCache();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // The whole batch failed (API rejected the call). Attribute the
                // error to the drive — we can't easily say which userIds caused
                // the rejection without an additional probe.
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex),
                    "RemovePmLicensedUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
