using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmGroupLicense", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
public class RemovePmGroupLicenseCmdlet : OrchestratorPSCmdlet
{
    // Manages license codes
    private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicenseCompleter))]
    [SupportsWildcards]
    public string[]? License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class PmLicenseCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpGroupName = GetFakeBoundParameters(fakeBoundParameters, "GroupName").ConvertToWildcardPatternList();
            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicensedGroups.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var group in result
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name))
                {
                    if (group.userBundleLicenses is null) continue;

                    var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.id, group.name!);

                    foreach (var bundle in group.userBundleLicenses
                        .Select(bundle => (bundle, AvailableUserBundlesItems.Items.GetValueOrDefault(bundle, bundle)))
                        .ExcludeByWildcards(b => b.Item2, wpLicense)
                        .OrderBy(b => b.Item2))
                    {
                        string tiphelp = $"{drive.NameColonSeparator}{bundle.bundle}";
                        var availableUserBundle = availableUserBundles?.availableUserBundles?.FirstOrDefault(b => string.Compare(b.code, bundle.bundle, StringComparison.OrdinalIgnoreCase) == 0);
                        if (availableUserBundle is not null)
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

        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpLicense = License.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            var licenseGroups = drive.PmLicensedGroups.Get();
            var targetGroups = licenseGroups.SelectByWildcards(g => g?.name, wpGroupName);

            foreach (var group in targetGroups.OrderBy(g => g.name))
            {
                // Resolved against what THIS GROUP holds, like Remove-PmUserLicense resolves
                // against what the user holds -- the same helper, so the two Remove cmdlets
                // cannot drift apart. It used to resolve against the static catalog, and only
                // by the catalog's friendly NAME, which made this the one cmdlet in the family
                // that would not take a raw bundle code: `-License APPTESTDEVNU` matched
                // nothing and removed nothing, without a word. The catalog was never the right
                // source either -- it lists fifteen bundles and a live org can hold codes it
                // has never heard of.
                if (!_parameterSets.TryGetValue((drive, group), out var codes))
                {
                    codes = [];
                    _parameterSets[(drive, group)] = codes;
                }
                codes.UnionWith(RemovePmUserLicenseCmdlet.ResolveLicenseCodesToRemove(
                    wpLicense, group.userBundleLicenses));
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_parameterSets is null) return;

        foreach (var parameterSet in _parameterSets
            .OrderBy(p => p.Key.drive.Name)
            .OrderBy(p => p.Key.group.name)
            .WithProgressBar(this, "Removing licenses from PmGroups", p => p.Key.group.name))
        {
            var (drive, group) = parameterSet.Key;
            var codesToRemove = parameterSet.Value;

            var existingSet = new HashSet<string>(group.userBundleLicenses ?? []);

            int initialCount = existingSet.Count;
            existingSet.ExceptWith(codesToRemove);

            // Skip processing if there are no licenses to remove
            if (existingSet.Count == initialCount) continue;

            string target = group.GetPSPath(drive.NameColonSeparator);
            if (ShouldProcess(target, "Remove License from PmGroup"))
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
                    if (ret is not null)
                    {
                        ret.userBundleLicenseNames = ret.userBundleCodes?.Select(b => AvailableUserBundlesItems.Items.GetValueOrDefault(b, b)).ToArray();
                        // UpdateLicensedGroupResponse is a fresh per-call
                        // response (not a shared cache singleton), so set the
                        // drive-local labels directly — no ShallowClone needed.
                        ret.Path = drive.NameColonSeparator;
                        ret.GroupName = group.name;
                        WriteObject(ret);
                    }
                    drive.PmLicensedGroups.ClearCache();
                    drive.PmUserLicenseGroupAllocations.ClearCache();
                    drive.PmAvailableUserBundles.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemovePmGroupLicenseError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
            }
        }
    }
}
