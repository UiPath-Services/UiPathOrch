using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "PmGroupLicense", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
public class AddPmGroupLicenseCmdlet : OrchestratorPSCmdlet
{
    // Accumulates the bundle codes to add per group across all pipeline rows,
    // applied once each in EndProcessing (the license API is atomic-replace, so a
    // group must be PUT exactly once with its full merged set — never once per row).
    // Keyed by (drive, lower-cased group name): -GroupName identifies a group by
    // name, so rows naming the same group case-insensitively aggregate together
    // regardless of which PmDirectoryEntityInfo instance the directory cache hands
    // back. The value carries the resolved group (for its identifier on the PUT)
    // alongside the merged codes.
    private Dictionary<(OrchDriveInfo drive, string groupNameKey), (PmDirectoryEntityInfo group, HashSet<string> codes)>? _parameterSets;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(SearchGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LicenseCompleter))]
    [SupportsWildcards]
    public string[]? License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private class SearchGroupNameCompleter : OrchArgumentCompleter
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

            var wpGroupName = CreateSelfExclusionList(commandAst, "GroupName", wordToComplete);

            var drives = ResolvePmDrives(fakeBoundParameters);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                //var existingGroups = drive.GetPmGroups().Values;
                //var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                var groups = drive.SearchPmDirectoryCache.Get(wordToComplete.ToLower());
                if (groups is null) continue;

                foreach (var group in groups
                    .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                    .ExcludeByWildcards(e => e?.identityName, wpGroupName)
                    .OrderBy(e => e.identityName))
                {
                    bFound = true;
                    string tiphelp = group.TipHelp(drive.NameColonSeparator);
                    yield return new CompletionResult(PathTools.EscapePSText(group?.identityName), group?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No groups found for '{wordToComplete}')""");
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
            var drives = ResolvePmDrives(fakeBoundParameters);

            var groupNames = GetFakeBoundParameters(fakeBoundParameters, "GroupName");
            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            foreach (var drive in drives)
            {
                // Collect each matched group's (available codes, held codes) plus a
                // representative bundle per code for the tooltip. Suggest the union of
                // available licenses minus only the ones EVERY matched group already
                // holds (SuggestableLicenseCodes), so a license missing from any one
                // group stays actionable. Scoped per drive — a same-named group on
                // another drive is a different entity.
                var perGroup = new List<(IEnumerable<string?> available, IEnumerable<string?>? held)>();
                var bundleByCode = new Dictionary<string, AvailableUserBundle>(StringComparer.OrdinalIgnoreCase);

                foreach (var groupName in groupNames)
                {
                    var groups = drive.SearchPmDirectoryCache.Get(groupName.ToLower());
                    if (groups is null) continue;

                    foreach (var group in FilterDirectoryGroupsByName(groups, groupName)
                        .OrderBy(e => e.identityName))
                    {
                        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                        if (availableUserBundles?.availableUserBundles is null) continue;

                        var heldCodes = drive.PmLicensedGroups.Get()
                            .FirstOrDefault(g => g.id == group.identifier)?.userBundleLicenses;
                        perGroup.Add((availableUserBundles.availableUserBundles.Select(b => b?.code), heldCodes));

                        foreach (var b in availableUserBundles.availableUserBundles)
                        {
                            if (b?.code is not null) bundleByCode.TryAdd(b.code, b);
                        }
                    }
                }

                foreach (var item in SuggestableLicenseCodes(perGroup)
                    // Display name = catalog friendly name; raw-code fallback so an
                    // uncatalogued bundle can't make completion throw.
                    .Select(code => (code, desc: AvailableUserBundlesItems.Items.TryGetValue(code, out var n) ? n : code))
                    .ExcludeByWildcards(x => x.desc, wpLicense)
                    .OrderBy(x => x.desc))
                {
                    string tiphelp = drive.NameColonSeparator + item.desc;
                    if (bundleByCode.TryGetValue(item.code, out var b))
                    {
                        tiphelp += $"  Available: {b.total - b.allocated}";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(item.desc), item.desc, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    // The directory Search API matches by prefix (startsWith), so a request for
    // "Developers" also returns "Developers2", and a -ExportCsv round trip would
    // license the wrong / extra group. Narrow the results to the name actually
    // requested: a bare name matches exactly and a wildcard pattern matches per
    // its pattern (GroupName is [SupportsWildcards]) — the same WildcardPattern
    // semantics Get-/Remove- use, so each exported row binds to exactly one group.
    // Pure / static so the prefix-over-match guard is unit-testable.
    internal static IEnumerable<PmDirectoryEntityInfo> FilterDirectoryGroupsByName(
        IEnumerable<PmDirectoryEntityInfo> groups, string requestedName)
    {
        var wp = new WildcardPattern(requestedName, WildcardOptions.IgnoreCase);
        return groups.Where(g =>
            (g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
            && g.identityName is not null
            && wp.IsMatch(g.identityName));
    }

    // The license codes -License completion should suggest across one or more
    // matched groups. Candidates = the union of every group's available bundles
    // (the API returns held bundles too); excluded = only the codes EVERY group
    // already holds (the intersection of the held sets). So for a single group it
    // is exactly "available minus held"; for several groups a license stays
    // suggested as long as at least one group is missing it. Case-insensitive on
    // codes; empty input yields an empty set. Pure / static for unit testing.
    internal static HashSet<string> SuggestableLicenseCodes(
        IReadOnlyCollection<(IEnumerable<string?> available, IEnumerable<string?>? held)> groups)
    {
        var union = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (groups is null || groups.Count == 0) return union;

        HashSet<string>? commonHeld = null;
        foreach (var (available, held) in groups)
        {
            foreach (var c in available ?? []) if (c is not null) union.Add(c);

            var heldSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in held ?? []) if (c is not null) heldSet.Add(c);

            if (commonHeld is null) commonHeld = heldSet;
            else commonHeld.IntersectWith(heldSet);
        }

        if (commonHeld is not null) union.ExceptWith(commonHeld);
        return union;
    }

    // Resolves -License wildcards to the bundle codes addable to a group. The
    // available-bundles API returns codes (its bundle `name` is always empty on
    // this build), so matching goes through the static catalog: a pattern matches
    // a bundle if it matches the catalog friendly name OR the raw code (so users
    // can pass either form, and a Get-PmGroupLicense -ExportCsv License column —
    // which is the friendly name — round-trips). Restricted to availableCodes so
    // only licenses actually offered to the group are added. Pure / static for
    // unit testing.
    internal static HashSet<string> ResolveLicenseCodesForGroup(
        IEnumerable<WildcardPattern>? wpLicense, IEnumerable<string?>? availableCodes)
    {
        var resolved = new HashSet<string>();
        if (wpLicense is null || availableCodes is null) return resolved;

        foreach (var code in availableCodes)
        {
            if (code is null) continue;
            string friendly = AvailableUserBundlesItems.Items.TryGetValue(code, out var n) ? n : code;
            foreach (var wp in wpLicense)
            {
                if (wp.IsMatch(friendly) || wp.IsMatch(code))
                {
                    resolved.Add(code);
                    break;
                }
            }
        }
        return resolved;
    }

    protected override void ProcessRecord()
    {
        _parameterSets ??= [];

        var drives = SessionState.EnumPmDrives(Path);

        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        var wpLicense = License.Split1stValueByUnescapedCommasPreservingEscapes().ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            foreach (var groupName in (GroupName ?? []).WithCancellation(cancelHandler.Token))
            {
                var groups = drive.SearchPmDirectoryCache.Get(groupName.ToLower());
                if (groups is null) continue;

                foreach (var group in FilterDirectoryGroupsByName(groups, groupName)
                    .OrderBy(e => e.identityName).WithCancellation(cancelHandler.Token))
                {
                    // Aggregate by name (case-insensitive) so multiple rows for the
                    // same group merge into one PUT. identityName is non-null here
                    // (FilterDirectoryGroupsByName filters out null names).
                    var key = (drive, group.identityName!.ToLowerInvariant());
                    if (!_parameterSets.TryGetValue(key, out var entry))
                    {
                        entry = (group, []);
                        _parameterSets[key] = entry;
                    }

                    var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                    entry.codes.UnionWith(ResolveLicenseCodesForGroup(
                        wpLicense,
                        availableUserBundles?.availableUserBundles?.Select(b => b?.code)));
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_parameterSets is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var parameterSet in _parameterSets
            .OrderBy(p => p.Key.drive.Name)
            .ThenBy(p => p.Key.groupNameKey).WithCancellation(cancelHandler.Token))
        {
            var drive = parameterSet.Key.drive;
            var (group, codesToAdd) = parameterSet.Value;

            var existingLicensedGroup = drive.PmLicensedGroups.Get();
            var targetGroup = existingLicensedGroup.FirstOrDefault(g => g.id == group.identifier);

            var existingSet = new HashSet<string>(targetGroup?.userBundleLicenses ?? []);

            int initialCount = existingSet.Count;
            existingSet.UnionWith(codesToAdd);

            // Skip processing if there are no licenses to add
            if (existingSet.Count == initialCount) continue;

            string target = group.GetPSPath(drive.NameColonSeparator);
            if (ShouldProcess(target, "Add License to PmGroup"))
            {
                try
                {
                    UpdateLicensedGroupCommand cmd = new()
                    {
                        id = group.identifier,
                        useExternalLicense = targetGroup?.useExternalLicense ?? false,
                        ubls = existingSet.ToArray()
                    };

                    var ret = drive.OrchAPISession.PutPmLicenseGroup(cmd);
                    if (ret is not null)
                    {
                        ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                        // UpdateLicensedGroupResponse is a fresh per-call
                        // response (not a shared cache singleton), so set the
                        // drive-local labels directly — no ShallowClone needed.
                        ret.Path = drive.NameColonSeparator;
                        ret.GroupName = group.identityName;
                        WriteObject(ret);
                    }
                    drive.PmLicensedGroups.ClearCache();
                    drive.PmUserLicenseGroupAllocations.ClearCache();
                    drive.PmAvailableUserBundles.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "AddPmGroupLicenseError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
            }
        }
    }
}
