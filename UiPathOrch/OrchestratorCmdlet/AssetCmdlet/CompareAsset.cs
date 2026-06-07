using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare assets between two locations and report the differences, in the spirit of
// Compare-Object but resolved over Orch drives/folders and matched by Name (not Id, which
// is tenant-local). Primary use is cross-tenant / cross-folder migration verification:
// "did the assets I copied land with the same values?".
//
// The reference side (-Path / pipeline) is specified like every other asset cmdlet — bulk,
// wildcards, -Recurse. The difference side (-DifferencePath, mandatory) is a single folder.
// Two modes, selected by -DifferenceName:
//
//   * omitted  → name-match: each reference asset is compared to the same-named asset in the
//                corresponding difference folder (mirrored relative path under -DifferencePath
//                when -Recurse is used). Reference-only assets emit "<=", difference-only "=>".
//
//   * supplied → broadcast: every reference asset is compared to the single named asset in
//                -DifferencePath, even when the names differ. Lets you diff two differently
//                named assets (e.g. ApiKey_Old vs ApiKey_New), including within one folder.
//
// NOTES on behavior worth knowing:
//   * Full, bidirectional diffs come from the -Path / -DifferencePath form. Driving the
//     reference side from the pipeline (Get-OrchAsset ... | Compare-OrchAsset) binds Name per
//     piped item, so the comparison is reference-scoped: it reports "<=" / "<>" / "==" for the
//     piped assets but CANNOT surface "=>" (assets that exist only on the difference side),
//     because no reference item drives them. Use the -Path form to detect difference-side
//     additions.
//   * Credential/Secret values can't be read back, so password/secret drift is invisible. The
//     credential USERNAME is still compared (it surfaces in Value as "username: <name>" and in
//     CredentialUsername).
//   * Cross-tenant: per-user values (UserValues) are keyed by user name, which usually differs
//     across tenants. Pass -UserMappingCsv (same CSV as Copy-OrchAsset) to translate reference
//     user names to their difference-side equivalents before comparing; otherwise remapped
//     users show up as spurious UserValues differences. The mapping self-disables when both
//     sides are the same tenant/org.
[Cmdlet(VerbsData.Compare, "OrchAsset")]
[OutputType(typeof(OrchComparison))]
public class CompareAssetCmdlet : OrchestratorPSCmdlet
{
    // Reference side. Positional 0 (so `Compare-OrchAsset <ref> <diff>` reads ref-then-diff
    // like Compare-Object), pipeline-bindable, defaults to the current location when omitted.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    // Difference side. Mandatory single folder; positional 1.
    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    // When set, every reference asset is compared to this single asset in -DifferencePath
    // (broadcast mode). Single by design: comparing many reference assets against many named
    // targets has no well-defined pairing — that's a rename-map job, not this parameter.
    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    public string? DifferenceName { get; set; }

    // Filters the reference assets (and, in name-match mode, the difference assets too, so
    // "=>" rows stay within the same name scope).
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(AssetValueTypeCompleter))]
    [SupportsWildcards]
    public string[]? ValueType { get; set; }

    // Restrict the comparison to these property names (see Comparators / UserValues). Defaults
    // to the full curated set. Mirrors Compare-Object -Property. Unrecognized names are warned
    // about and ignored — without that warning a typo'd property would silently compare nothing
    // and report every asset as equal.
    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    // Cross-tenant user-name translation for the per-user (UserValues) comparison; same CSV
    // (SourceUserName,DestinationUserName) consumed by Copy-OrchAsset. No effect within one
    // tenant/org.
    [Parameter]
    public string? UserMappingCsv { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Also emit "==" rows for assets that match on every compared property (Compare-Object
    // -IncludeEqual). Off by default: only differences are surfaced.
    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    // Migration-relevant scalar property set. Deliberately excludes Id/Key/timestamps/
    // CreatorUserId and other tenant-local fields. Bool values are normalized to lower case so
    // a "True"/"true" representation difference between API versions isn't a false positive.
    // Tags are normalized to an order-independent string. UserValues is handled separately
    // (EmitComparison) because it takes the reference-side user-name mapping.
    private static readonly (string Name, Func<Asset, object?> Get)[] ScalarComparators =
    [
        ("ValueType", a => a.ValueType),
        ("ValueScope", a => a.ValueScope),
        ("Value", a => NormalizeValue(a)),
        ("Description", a => a.Description),
        ("CredentialUsername", a => a.CredentialUsername),
        ("ExternalName", a => a.ExternalName),
        ("AllowDirectApiAccess", a => a.AllowDirectApiAccess),
        ("Tags", a => NormalizeTags(a.Tags)),
    ];

    internal const string UserValuesProperty = "UserValues";

    internal static readonly HashSet<string> ValidPropertyNames =
        new(ScalarComparators.Select(c => c.Name).Append(UserValuesProperty), StringComparer.OrdinalIgnoreCase);

    // Pure diff surface (no WriteObject) so the comparator set, bool/tag/user-value
    // normalization, the reference-side user mapping, and -Property scoping are unit-testable
    // without a live drive. Returns every differing property; empty means "equal".
    internal static List<PropertyDifference> ComputeAssetDifferences(
        Asset reference, Asset difference,
        IReadOnlyCollection<string>? only, bool compareUserValues,
        Dictionary<string, string>? userMapping)
    {
        var diffs = EntityComparison.DiffProperties(reference, difference, ScalarComparators, only);

        // UserValues is compared outside the generic engine so the reference side can be run
        // through the user-name mapping while the difference side is kept as-is.
        if (compareUserValues)
        {
            var rv = NormalizeUserValues(reference.UserValues, userMapping);
            var dv = NormalizeUserValues(difference.UserValues, null);
            if (!EntityComparison.ValueEquals(rv, dv))
            {
                diffs.Add(new PropertyDifference { Property = UserValuesProperty, ReferenceValue = rv, DifferenceValue = dv });
            }
        }

        return diffs;
    }

    // Flush deferred per-drive warnings for both sides in this same call. The base default
    // covers the current location and -Path; -DifferencePath (and -LiteralPath) are ours.
    protected override IEnumerable<string> GetTargetDriveNames()
    {
        foreach (var n in base.GetTargetDriveNames()) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("DifferencePath", out var dp))
            foreach (var n in ExtractDriveNamesFromBoundPath(dp)) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("LiteralPath", out var lp))
            foreach (var n in ExtractDriveNamesFromBoundPath(lp)) yield return n;
    }

    protected override void ProcessRecord()
    {
        var effPath = EffectivePath(Path, LiteralPath);
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(effPath);
        var srcDrivesFolders = SessionState.EnumFolders(effPath, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(DifferencePath);

        var wpName = Name.ConvertToWildcardPatternList();
        var wpValueType = ValueType.ConvertToWildcardPatternList();

        HashSet<string>? only = null;
        if (Property is { Length: > 0 })
        {
            only = new HashSet<string>(Property, StringComparer.OrdinalIgnoreCase);
            var unknown = only.Where(p => !ValidPropertyNames.Contains(p)).ToList();
            if (unknown.Count > 0)
            {
                WriteWarning($"-Property: ignoring unrecognized name(s): {string.Join(", ", unknown)}. " +
                             $"Comparable properties are: {string.Join(", ", ValidPropertyNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))}.");
            }
        }

        bool compareUserValues = only is null || only.Contains(UserValuesProperty);

        // SourceUserName -> DestinationUserName, applied to reference UserValues only. Null for
        // same-tenant compares (LoadUserMappingCsv self-disables and warns).
        var userMapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, UserMappingCsv);

        using var cancelHandler = new ConsoleCancelHandler();

        if (!string.IsNullOrEmpty(DifferenceName))
        {
            CompareBroadcast(srcDrive, srcDrivesFolders, dstDrive, dstRootFolder, wpName, wpValueType, only, compareUserValues, userMapping, cancelHandler);
        }
        else
        {
            CompareByName(srcDrive, srcRootFolder, srcDrivesFolders, dstDrive, dstRootFolder, wpName, wpValueType, only, compareUserValues, userMapping, cancelHandler);
        }
    }

    // Broadcast mode: each reference asset vs the single -DifferenceName asset.
    private void CompareBroadcast(
        OrchDriveInfo srcDrive,
        List<(OrchDriveInfo drive, Folder folder)> srcDrivesFolders,
        OrchDriveInfo dstDrive, Folder dstRootFolder,
        List<WildcardPattern>? wpName, List<WildcardPattern>? wpValueType,
        HashSet<string>? only, bool compareUserValues, Dictionary<string, string>? userMapping,
        ConsoleCancelHandler cancelHandler)
    {
        Asset? target;
        try
        {
            target = dstDrive.Assets.Get(dstRootFolder)
                .FirstOrDefault(a => string.Equals(a?.Name, DifferenceName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(dstRootFolder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, dstRootFolder));
            return;
        }

        // An explicitly named target that doesn't exist is almost always a typo, so stop with
        // one clear error rather than flooding every reference asset with a "<=" row.
        if (target is null)
        {
            WriteError(new ErrorRecord(
                new OrchException(dstRootFolder.GetPSPath(), $"DifferenceName '{DifferenceName}' was not found in '{dstRootFolder.GetPSPath()}'."),
                "DifferenceNameNotFound", ErrorCategory.ObjectNotFound, DifferenceName));
            return;
        }

        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<Asset> refAssets;
            try
            {
                refAssets = srcDrive.Assets.Get(srcFolder)
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(a => a?.Name, wpName);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            foreach (var refAsset in refAssets)
            {
                if (refAsset is null) continue;
                EmitComparison(refAsset, target, only, compareUserValues, userMapping);
            }
        }
    }

    // Name-match mode: reference folder vs the mirrored difference folder, matched by Name.
    private void CompareByName(
        OrchDriveInfo srcDrive, Folder srcRootFolder,
        List<(OrchDriveInfo drive, Folder folder)> srcDrivesFolders,
        OrchDriveInfo dstDrive, Folder dstRootFolder,
        List<WildcardPattern>? wpName, List<WildcardPattern>? wpValueType,
        HashSet<string>? only, bool compareUserValues, Dictionary<string, string>? userMapping,
        ConsoleCancelHandler cancelHandler)
    {
        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            List<Asset> refAssets;
            try
            {
                refAssets = srcDrive.Assets.Get(srcFolder)
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(a => a?.Name, wpName)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            // The difference folder mirroring srcFolder's relative path. Null (silently) when
            // it doesn't exist on the difference side — then every reference asset is "<=".
            Folder? dstFolder = ResolveDifferenceFolderOrNull(srcRootFolder, srcFolder, dstDrive, dstRootFolder);

            var diffByName = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);
            if (dstFolder is not null)
            {
                try
                {
                    foreach (var a in dstDrive.Assets.Get(dstFolder)
                        .FilterByWildcards(x => x?.ValueType, wpValueType)
                        .FilterByWildcards(x => x?.Name, wpName))
                    {
                        if (a?.Name is { } n) diffByName[n] = a;
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, dstFolder));
                    continue;
                }
            }

            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var refAsset in refAssets)
            {
                if (refAsset is null) continue;
                if (refAsset.Name is { } name && diffByName.TryGetValue(name, out var diffAsset))
                {
                    matched.Add(name);
                    EmitComparison(refAsset, diffAsset, only, compareUserValues, userMapping);
                }
                else
                {
                    EmitOnly(refAsset, EntityComparison.ReferenceOnly);
                }
            }

            foreach (var kv in diffByName)
            {
                if (matched.Contains(kv.Key)) continue;
                EmitOnly(kv.Value, EntityComparison.DifferenceOnly);
            }
        }
    }

    private void EmitComparison(Asset reference, Asset difference, HashSet<string>? only, bool compareUserValues, Dictionary<string, string>? userMapping)
    {
        var diffs = ComputeAssetDifferences(reference, difference, only, compareUserValues, userMapping);

        if (diffs.Count == 0)
        {
            if (IncludeEqual.IsPresent)
            {
                WriteObject(new OrchComparison
                {
                    SideIndicator = EntityComparison.Equal,
                    Name = reference.Name,
                    Path = reference.GetPSPath(),
                    DifferencePath = difference.GetPSPath(),
                    ReferenceObject = reference,
                    DifferenceObject = difference,
                });
            }
            return;
        }

        WriteObject(new OrchComparison
        {
            SideIndicator = EntityComparison.Different,
            Name = reference.Name,
            Path = reference.GetPSPath(),
            DifferencePath = difference.GetPSPath(),
            Differences = diffs,
            ReferenceObject = reference,
            DifferenceObject = difference,
        });
    }

    private void EmitOnly(Asset asset, string side)
    {
        bool isReference = side == EntityComparison.ReferenceOnly;
        WriteObject(new OrchComparison
        {
            SideIndicator = side,
            Name = asset.Name,
            Path = isReference ? asset.GetPSPath() : null,
            DifferencePath = isReference ? null : asset.GetPSPath(),
            ReferenceObject = isReference ? asset : null,
            DifferenceObject = isReference ? null : asset,
        });
    }

    // Mirror of GetRelativeDstFolder's path computation, but returns null silently when the
    // mirrored folder is absent (Compare must not create folders or raise an error there).
    private static Folder? ResolveDifferenceFolderOrNull(Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder)
    {
        string relativePath = srcFolder.GetRelativePath(srcRootFolder);
        string dstRoot = dstRootFolder.FullyQualifiedName ?? "";
        string strDstFolder = dstRoot == "" ? relativePath : (dstRoot + "/" + relativePath).Trim('/');

        if (string.IsNullOrEmpty(strDstFolder)) return dstDrive.RootFolder;

        return dstDrive.GetFolders()
            .FirstOrDefault(f => string.Compare(f.FullyQualifiedName, strDstFolder, StringComparison.OrdinalIgnoreCase) == 0);
    }

    // Bool values are normalized to lower case so a "True" vs "true" representation difference
    // (which can vary by API version) doesn't read as drift; other types compare verbatim.
    internal static string? NormalizeValue(Asset asset)
        => string.Equals(asset.ValueType, "Bool", StringComparison.OrdinalIgnoreCase)
            ? asset.Value?.ToLowerInvariant()
            : asset.Value;

    internal static string? NormalizeTags(Tag[]? tags)
        => tags is null || tags.Length == 0
            ? null
            : string.Join(";", tags.Select(t => $"{t.Name}={t.Value}").OrderBy(s => s, StringComparer.Ordinal));

    // Order-independent normalized form of the per-user overrides. The reference side's user
    // names are translated through userMapping (SourceUserName -> DestinationUserName) so a
    // cross-tenant rename doesn't read as a difference; the difference side passes null.
    internal static string? NormalizeUserValues(List<AssetUserValue>? userValues, Dictionary<string, string>? userMapping)
    {
        if (userValues is null || userValues.Count == 0) return null;
        return string.Join(";", userValues
            .Select(u =>
            {
                var name = u.UserName;
                if (name is not null && userMapping is not null
                    && userMapping.TryGetValue(name, out var mapped) && !string.IsNullOrEmpty(mapped))
                {
                    name = mapped;
                }
                return $"{name}\\{u.MachineName}={u.ValueType}:{u.Value}";
            })
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
    }
}
