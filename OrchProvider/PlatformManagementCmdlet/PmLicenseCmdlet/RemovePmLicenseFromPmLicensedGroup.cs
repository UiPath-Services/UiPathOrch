using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using System.Text.RegularExpressions;
using System.Reflection.Metadata;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchPmLicenseFromPmLicensedGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
    public class RemoveLicenseFromLicenseGroup: OrchestratorPSCmdlet
    {
        // code を管理
        private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter<GroupName_License>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(LicenseCompleter))]
        [SupportsWildcards]
        public string[]? License { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
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

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmLicensedGroups());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var group in entities!
                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name))
                    {
                        if (group.userBundleLicenses == null) continue;

                        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.id, group.name!);

                        foreach (var bundle in group.userBundleLicenses
                            .Select(bundle => (bundle, AvailableUserBundlesItems.Items[bundle]))
                            .ExcludeByWildcards(b => b.Item2, wpLicense)
                            .OrderBy(b => b.Item2))
                        {
                            string tiphelp = $"{drive.NameColonSeparator}{bundle.bundle}";
                            var availableUserBundle = availableUserBundles?.availableUserBundles?.FirstOrDefault(b => string.Compare(b.code, bundle.bundle, true) == 0);
                            if (availableUserBundle != null)
                            {
                                tiphelp += $"  Available: {availableUserBundle.total - availableUserBundle.allocated}";
                            }
                            yield return new CompletionResult(PathTools.EscapePSText(bundle.Item2), bundle.Item2, CompletionResultType.Text, tiphelp);
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

            var specifiedLicenses = AvailableUserBundlesItems.Items.SelectByWildcards(i => i.Value, wpLicense).ToList();

            foreach (var drive in drives)
            {
                var licenseGroups = drive.GetPmLicensedGroups();
                var targetGroups = licenseGroups.SelectByWildcards(g => g?.name, wpGroupName);

                foreach (var group in targetGroups.OrderBy(g => g.name))
                {
                    if (!_parameterSets.TryGetValue((drive, group), out var codes))
                    {
                        codes = [];
                        _parameterSets[(drive, group)] = codes;
                    }
                    codes.UnionWith(specifiedLicenses.Select(l => l.Key));
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
                var codesToRemove = parameterSet.Value;

                var existingSet = new HashSet<string>(group.userBundleLicenses ?? []);

                int initialCount = existingSet.Count;
                existingSet.ExceptWith(codesToRemove);

                // 削除すべきライセンスがなければ処理をスキップ
                if (existingSet.Count == initialCount) continue;

                string target = group.GetPSPath();
                if (ShouldProcess(target, "Remove License from PmLicenseGroup"))
                {
                    try
                    {
                        UpdateLicensedGroupCommand cmd = new()
                        {
                            id = group.id,
                            useExternalLicense = group.useExternalLicense,
                            ubls = existingSet.ToArray()
                        };

                        var ret = drive.OrchAPISession.PutPmLicenseGroup(cmd);
                        if (ret != null)
                        {
                            ret.Path = drive.NameColonSeparator;
                            ret.GroupName = group.name;
                            ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                            WriteObject(ret);
                        }
                        drive._dicPmLicensedGroups = null;
                        drive._dicPmUserLicenseGroupAllocations = null;
                        drive._dicPmAvailableUserBundles = null;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemovePmLicenseFromPmLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                }
            }
        }
    }
}
