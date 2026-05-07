using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Removes per-robot (UserValue) entries from one or more assets, regardless of asset type.
// The primary motivation is Secret-typed assets, where the value is masked by the API so the
// empty-delete convention used on Set-OrchAsset / Set-OrchCredentialAsset is not round-trip
// safe. This cmdlet provides the explicit, type-agnostic path for any asset.
[Cmdlet(VerbsCommon.Remove, "OrchAssetUserValue", SupportsShouldProcess = true)]
public class RemoveAssetUserValueCommand : OrchestratorPSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? MachineName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpUserName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var result in results)
            {
                foreach (var asset in result.FilterByWildcards(a => a?.Name, wpName))
                {
                    if (asset.UserValues is null) continue;
                    foreach (var uv in asset.UserValues
                        .Where(uv => uv.UserName is { } un && wp.IsMatch(un))
                        .ExcludeByWildcards(uv => uv?.UserName, wpUserName))
                    {
                        if (!seen.Add(uv.UserName!)) continue;
                        yield return new CompletionResult(
                            PathTools.EscapePSText(uv.UserName!), uv.UserName!,
                            CompletionResultType.ParameterValue,
                            $"{asset.GetPSPath()} -- {uv.UserName}\\{uv.MachineName}");
                    }
                }
            }
        }
    }

    private class MachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();
            var wpMachineName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var result in results)
            {
                foreach (var asset in result.FilterByWildcards(a => a?.Name, wpName))
                {
                    if (asset.UserValues is null) continue;
                    foreach (var uv in asset.UserValues
                        .FilterByWildcards(uv => uv?.UserName, wpUserName)
                        .Where(uv => uv.MachineName is { } mn && wp.IsMatch(mn))
                        .ExcludeByWildcards(uv => uv?.MachineName, wpMachineName))
                    {
                        if (!seen.Add(uv.MachineName!)) continue;
                        yield return new CompletionResult(
                            PathTools.EscapePSText(uv.MachineName!), uv.MachineName!,
                            CompletionResultType.ParameterValue,
                            $"{asset.GetPSPath()} -- {uv.UserName}\\{uv.MachineName}");
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();
        // When MachineName is not specified, match any (including null — covers user-only UserValues).
        var wpMachineName = MachineName is not null && MachineName.Length > 0
            ? MachineName.ConvertToWildcardPatternList()
            : null;

        List<(OrchDriveInfo drive, Int64 folderId)> cachesToClear = [];

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
            var assets = drive.Assets.Get(folder).FilterByWildcards(a => a?.Name, wpName).ToList();

            foreach (var existing in assets.WithCancellation(cancelHandler.Token))
            {
                if (existing.UserValues is null || existing.UserValues.Count == 0) continue;

                var copy = OrchCollectionExtensions.DeepCopy(existing);
                copy.Path = folder.GetPSPath();

                var keep = new List<AssetUserValue>();
                var removed = new List<AssetUserValue>();

                foreach (var uv in copy.UserValues!)
                {
                    // wpUserName comes from a Mandatory parameter so PowerShell guarantees it is
                    // populated by the time we reach ProcessRecord; the null guard satisfies the
                    // compiler (CS8604) without changing observable behavior.
                    bool userMatch = wpUserName is not null
                        && wpUserName.Any(p => p.IsMatch(uv.UserName ?? ""));
                    bool machineMatch = wpMachineName is null
                        || wpMachineName.Any(p => p.IsMatch(uv.MachineName ?? ""));

                    if (userMatch && machineMatch)
                        removed.Add(uv);
                    else
                        keep.Add(uv);
                }

                if (removed.Count == 0) continue;

                foreach (var uv in removed)
                {
                    string target = $"{copy.GetPSPath()} [{uv.UserName}\\{uv.MachineName}]";
                    if (!ShouldProcess(target, "Remove UserValue")) return;
                }

                if (keep.Count > 0)
                {
                    copy.UserValues = keep;
                }
                else
                {
                    copy.UserValues = null;
                    copy.ValueScope = "Global";
                }

                try
                {
                    drive.OrchAPISession.PutAsset(folder.Id ?? 0, copy);
                    cachesToClear.Add((drive, folder.Id ?? 0));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        new OrchException(copy.GetPSPath(), ex),
                        "RemoveOrchAssetUserValueError",
                        ErrorCategory.InvalidOperation,
                        copy));
                }
            }
        }

        foreach (var (drive, folderId) in cachesToClear)
        {
            drive.Assets.ClearCache(folderId);
        }
    }
}
