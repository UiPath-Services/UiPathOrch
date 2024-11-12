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

using Positional = UiPath.PowerShell.Positional.Name_Link;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchAssetLink")]
    [OutputType(typeof(Entities.SimpleFolder))]
    class RemoveAssetLinkCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        //[ArgumentCompleter(typeof(LinkCompleter))]
        [SupportsWildcards]
        public string[]? Link { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
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
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name_Link.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

                // TODO: link も取得する。link を複数もつアセットだけを候補に表示したい。

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var asset in entities!
                        .Where(a => wp.IsMatch(a.Name))
                        .Where(a => (a.UserValues == null || !a.UserValues.Any()) && a.ValueScope != "PerRobot")
                        .ExcludeByWildcards(a => a?.Name, wpName)
                        .OrderBy(a => a.Name))
                    {
                        string tooltip = System.IO.Path.Combine(asset.Path!, asset.Name!);
                        yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        // TODO: シングルスレッド化しないと。
        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            Parallel.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                try
                {
                    var assets = drive.Assets.Get(folder);
                    Parallel.ForEach(assets, asset =>
                    {
                        drive.GetFoldersForAsset(folder, asset);
                    });
                }
                catch { }
            });

            using var cancelHandler = new ConsoleCancelHandler();

            HashSet<(Int64 folderId, string assetName)> outputLink = [];
            foreach (var (drive, folder) in drivesFolders)
            {
                ICollection<Asset> assets;
                try
                {
                    assets = drive.Assets.Get(folder);
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
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            if (!outputLink.Add((accessibleFolder.Id ?? 0, asset.Name!)))
                            {
                                outputDone = true;
                            }
                        }
                        if (!outputDone)
                        {
                            WriteObject(accessibleFolders.AccessibleFolders, true);
                        }
                    }
                }
            }
        }
    }
}
