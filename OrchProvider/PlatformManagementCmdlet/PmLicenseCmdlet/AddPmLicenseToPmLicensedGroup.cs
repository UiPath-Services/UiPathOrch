using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchPmLicenseToPmLicensedGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
    public class AddAllocationToUserLicenseGroup: OrchestratorPSCmdlet
    {
        // code を管理
        //private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;
        private Dictionary<(OrchDriveInfo drive, PmDirectoryEntityInfo group), HashSet<string>>? _parameterSets;

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(SearchGroupNameCompleter))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(LicenseCompleter))]
        [SupportsWildcards]
        public string[]? License { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_License>))]
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

                var wpGroupName = CreateWPListFromParameter(commandAst, "GroupName", Positional.GroupName_Type_UserName.Parameters, wordToComplete);

                var drives = ResolveDrives(fakeBoundParameters);

                bool bFound = false;
                foreach (var drive in drives)
                {
                    //var existingGroups = drive.GetPmGroups().Values;
                    //var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                    var groups = drive.SearchPmDirectoryUsers(wordToComplete);
                    if (groups == null) continue;

                    foreach (var group in groups
                        .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                        .ExcludeByWildcards(e => e?.identityName, wpGroupName)
                        .OrderBy(e => e.identityName))
                    {
                        bFound = true;
                        string tiphelp = TipHelp(group);
                        yield return new CompletionResult(PathTools.EscapePSText(group?.identityName), group?.identityName, CompletionResultType.Text, tiphelp);
                    }
                }
                if (!bFound)
                {
                    yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
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
                var drives = ResolveDrives(fakeBoundParameters);

                var groupNames = GetParameterValues(commandAst, "GroupName", GroupName_License.Parameters);
                var wpLicense = CreateWPListFromParameter(commandAst, parameterName, GroupName_License.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives)
                {
                    foreach (var groupName in groupNames)
                    {
                        var groups = drive.SearchPmDirectoryUsers(groupName);
                        if (groups == null) continue;

                        foreach (var group in groups
                            .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                            .OrderBy(e => e.identityName))
                        {
                            var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                            if (availableUserBundles?.availableUserBundles == null) continue;

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
                yield break;
            }
        }

        protected override void ProcessRecord()
        {
            _parameterSets ??= [];

            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
            var wpLicense = License.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                foreach (var groupName in GroupName ?? [])
                {
                    var groups = drive.SearchPmDirectoryUsers(groupName);
                    if (groups == null) continue;

                    foreach (var group in groups
                        .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                        .OrderBy(e => e.identityName))
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
            if (_parameterSets == null) return;

            foreach (var parameterSet in _parameterSets
                .OrderBy(p => p.Key.drive.Name)
                .OrderBy(p => p.Key.group.identityName))
            {
                var (drive, group) = parameterSet.Key;
                var codesToAdd = parameterSet.Value;

                var existingLicensedGroup = drive.GetPmLicensedGroups();
                var targetGroup = existingLicensedGroup.FirstOrDefault(g => g.id == group.identifier);

                var existingSet = new HashSet<string>(targetGroup?.userBundleLicenses ?? []);

                int initialCount = existingSet.Count;
                existingSet.UnionWith(codesToAdd);

                // 追加すべきライセンスがなければ処理をスキップ
                if (existingSet.Count == initialCount) continue;

                string target = group.GetPSPath();
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
                        if (ret != null)
                        {
                            ret.Path = drive.NameColonSeparator;
                            ret.GroupName = group.identityName;
                            ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                            WriteObject(ret);
                        }
                        drive._dicPmLicensedGroups = null;
                        drive._dicPmUserLicenseGroupAllocations = null;
                        drive._dicPmAvailableUserBundles = null;
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
}
