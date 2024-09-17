using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchAssetLink")]
    [OutputType(typeof(Entities.SimpleFolder))]
    public class GetAssetLinkCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(AssetNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        // TODO: GetLinkedAssetName として共通化したい
        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetAssets(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var asset in entities!
                        .Where(a => wp.IsMatch(a.Name))
                        .Where(a => (a.UserValues == null || !a.UserValues.Any()) && a.ValueScope != "PerRobot")
                        .ExcludeByWildcards(a => a?.Name, wpName)
                        .OrderBy(a => a.Name))
                    {
                        //string tooltip = System.IO.Path.Combine(asset.Path!, asset.Name!);
                        string tooltip = asset.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        // TODO: この実装はきれいにできる
        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();


            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    var assets = drive.GetAssets(folder);
                    Parallel.ForEach(assets, asset =>
                    {
                        drive.GetFoldersForAsset(folder, asset);
                    });
                }
                catch { }
            });

            HashSet<(Int64 folderId, string assetName)> outputLink = new();
            foreach (var (drive, folder) in drivesFolders)
            {
                ReadOnlyCollection<Asset> assets;
                try
                {
                    assets = drive.GetAssets(folder);
                }
                catch (Exception ex)
                {
                    string target = folder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError", ErrorCategory.InvalidOperation, target));
                    continue;
                }

                foreach (var asset in assets
                        .FilterByWildcards(a => a?.Name, wpName)
                        .OrderBy(a => a.Name))
                {
                    AccessibleFoldersDto? accessibleFolders = null;
                    try
                    {
                        accessibleFolders = drive.GetFoldersForAsset(folder, asset);
                    }
                    catch (Exception ex)
                    {
                        string target = System.IO.Path.Combine(folder.GetPSPath(), asset.Name!);
                        WriteError(new ErrorRecord(new OrchException(target, ex), "AddAssetLinkError", ErrorCategory.InvalidOperation, target));
                        continue;
                    }

                    if (accessibleFolders != null &&
                        accessibleFolders.AccessibleFolders != null &&
                        accessibleFolders.AccessibleFolders.Length > 1)
                    {
                        bool outputDone = false;
                        foreach (var accessibleFolder in accessibleFolders.AccessibleFolders)
                        {
                            if (!outputLink.Add((accessibleFolder.Id ?? 0, asset.Name!)))
                            {
                                outputDone = true;
                            }
                        }
                        if (!outputDone)
                        {
                            WriteObject(accessibleFolders.AccessibleFolders
                                .OrderBy(a => a.FullyQualifiedName),
                                true);
                        }
                    }
                }
            }
        }
    }
}
