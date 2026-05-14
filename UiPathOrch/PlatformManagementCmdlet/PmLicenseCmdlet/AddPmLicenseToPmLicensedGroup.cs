using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "PmLicenseToPmLicensedGroup", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
public class AddPmLicenseToPmLicenseGroup : OrchestratorPSCmdlet
{
    // Manages license codes
    //private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;
    private Dictionary<(OrchDriveInfo drive, PmDirectoryEntityInfo group), HashSet<string>>? _parameterSets;

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
        public override IEnumerable<CompletionResult> CompleteArgument(
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
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var groupNames = GetFakeBoundParameters(fakeBoundParameters, "GroupName");
            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var drive in drives)
            {
                foreach (var groupName in groupNames)
                {
                    var groups = drive.SearchPmDirectoryCache.Get(groupName.ToLower());
                    if (groups is null) continue;

                    foreach (var group in groups
                        .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                        .OrderBy(e => e.identityName))
                    {
                        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                        if (availableUserBundles?.availableUserBundles is null) continue;

                        foreach (var bundle in availableUserBundles.availableUserBundles
                            //.Where(bundle => !(group.userBundleLicenses?.Contains(bundle.code) ?? false))
                            .ExcludeByWildcards(bundle => AvailableUserBundlesItems.Items[bundle!.code!], wpLicense)
                            .OrderBy(bundle => AvailableUserBundlesItems.Items[bundle.code!]))
                        {
                            string desc = AvailableUserBundlesItems.Items[bundle.code!];
                            string tiphelp = $"{drive.NameColonSeparator}{desc}  Available: {bundle.total - bundle.allocated}";
                            //string tiphelp = $"{drive.NameColonSeparator}{bundle.code}  Available: {bundle.total - bundle.allocated}";
                            yield return new CompletionResult(PathTools.EscapePSText(desc), desc, CompletionResultType.Text, tiphelp);
                        }
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _parameterSets ??= [];

        var drives = SessionState.EnumPmDrives(Path);

        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        var wpLicense = License.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            foreach (var groupName in (GroupName ?? []).WithCancellation(cancelHandler.Token))
            {
                var groups = drive.SearchPmDirectoryCache.Get(groupName.ToLower());
                if (groups is null) continue;

                foreach (var group in groups
                    .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                    .OrderBy(e => e.identityName).WithCancellation(cancelHandler.Token))
                {
                    //var licenseGroups = drive.GetPmLicensedGroups();
                    //var targetGroups = licenseGroups.SelectByWildcards(g => g?.name, wpGroupName);

                    //foreach (var group in targetGroups.OrderBy(g => g.name))
                    {
                        if (!_parameterSets.TryGetValue((drive, group), out var codes))
                        {
                            codes = [];
                            _parameterSets[(drive, group)] = codes;
                        }

                        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                        codes.UnionWith(availableUserBundles?.availableUserBundles?
                            .SelectByWildcards(b => b?.name, wpLicense)?
                            .Select(b => b.code!) ?? []);
                    }
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
            .OrderBy(p => p.Key.group.identityName).WithCancellation(cancelHandler.Token))
        {
            var (drive, group) = parameterSet.Key;
            var codesToAdd = parameterSet.Value;

            var existingLicensedGroup = drive.PmLicensedGroups.Get();
            var targetGroup = existingLicensedGroup.FirstOrDefault(g => g.id == group.identifier);

            var existingSet = new HashSet<string>(targetGroup?.userBundleLicenses ?? []);

            int initialCount = existingSet.Count;
            existingSet.UnionWith(codesToAdd);

            // Skip processing if there are no licenses to add
            if (existingSet.Count == initialCount) continue;

            string target = group.GetPSPath(drive.NameColonSeparator);
            if (ShouldProcess(target, "Add License to PmLicenseGroup"))
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
                        ret.Path = drive.NameColonSeparator;
                        ret.GroupName = group.identityName;
                        ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                        WriteObject(ret);
                    }
                    drive.PmLicensedGroups.ClearCache();
                    drive.PmUserLicenseGroupAllocations.ClearCache();
                    drive.PmAvailableUserBundles.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "AddPmLicenseToPmLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
            }
        }
    }
}
