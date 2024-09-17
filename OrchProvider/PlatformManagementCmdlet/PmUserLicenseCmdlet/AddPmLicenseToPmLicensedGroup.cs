using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using System.Text.RegularExpressions;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchPmLicenseToPmLicensedGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
    public class AddAllocationToUserLicenseGroup: OrchestratorPSCmdlet
    {
        // code を管理
        private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmLicenseGroupNameCompleter<GroupName_License>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(LicenseCompleter))]
        [SupportsWildcards]
        public string[]? License { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_License>))]
        public string[]? Path { get; set; }

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

                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", GroupName_License.Parameters);
                var wpLicense = CreateWPListFromParameter(commandAst, parameterName, GroupName_License.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmUserLicenseGroups());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var group in entities!
                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name))
                    {
                        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group);
                        if (availableUserBundles?.availableUserBundles == null) continue;

                        foreach (var bundle in availableUserBundles.availableUserBundles
                            .Where(bundle => !(group.userBundleLicenses?.Contains(bundle.code) ?? false))
                            .ExcludeByWildcards(bundle => AvailableUserBundlesItems.Items[bundle!.code!], wpLicense)
                            .OrderBy(bundle => AvailableUserBundlesItems.Items[bundle.code!]))
                        {
                            string desc = AvailableUserBundlesItems.Items[bundle.code!];
                            string tiphelp = $"{drive.NameColonSeparator}{bundle.code}  Available: {bundle.total - bundle.allocated}";
                            yield return new CompletionResult(PathTools.EscapePSText(desc), desc, CompletionResultType.Text, tiphelp);
                        }
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            _parameterSets ??= [];

            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpGroupName = GroupName.ConvertToWildcardPatternList();
            var wpLicense = License.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                var licenseGroups = drive.GetPmUserLicenseGroups();
                var targetGroups = licenseGroups.SelectByWildcards(g => g?.name, wpGroupName);

                foreach (var group in targetGroups.OrderBy(g => g.name))
                {
                    if (!_parameterSets.TryGetValue((drive, group), out var codes))
                    {
                        codes = [];
                        _parameterSets[(drive, group)] = codes;
                    }

                    var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group);
                    codes.UnionWith(availableUserBundles?.availableUserBundles?
                        .SelectByWildcards(b => b?.name, wpLicense)?
                        .Select(b => b.code!) ?? []);
                }
            }
        }

        protected override void EndProcessing()
        {
            if (_parameterSets == null) return;

            foreach (var parameterSet in _parameterSets
                .OrderBy(p => p.Key.drive.Name)
                .OrderBy(p => p.Key.group.name))
            {
                var (drive, group) = parameterSet.Key;
                var codesToAdd = parameterSet.Value;

                var existingSet = new HashSet<string>(group.userBundleLicenses ?? []);

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
                            id = group.id,
                            useExternalLicense = group.useExternalLicense,
                            ubls = existingSet.ToArray()
                        };

                        var ret = drive.OrchAPISession.PutUserLicenseGroup(cmd);
                        if (ret != null)
                        {
                            ret.Path = drive.NameColonSeparator;
                            ret.GroupName = group.name;
                            ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                            WriteObject(ret);
                        }
                        drive._dicPmUserLicenseGroups = null;
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
