using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchAsset", SupportsShouldProcess = true)]
public class RemoveAssetCommand : RemoveFolderEntityCmdletBase<Asset>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ValueTypeCompleter))]
    [SupportsWildcards]
    public string[]? ValueType { get; set; }

    private class ValueTypeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Only target the Names selected by the parameter
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude ValueTypes already selected by the parameter from the candidates
            var wpValueType = CreateSelfExclusionList(commandAst, "ValueType", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .FilterByWildcards(a => a?.Name, wpName)
                    .Where(a => wp.IsMatch(a.Name))
                    .ExcludeByWildcards(a => a?.ValueType, wpValueType)
                    .OrderBy(a => a.Name))
                {
                    string tiphelp = TipHelp(asset);
                    yield return new CompletionResult(PathTools.EscapePSText(asset.ValueType), asset.ValueType, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override string EntityNoun => "Asset";
    protected override Func<Asset?, string?> GetName => a => a?.Name;
    protected override Func<Asset, string> GetPSPath => a => a.GetPSPath();
    protected override Func<IEnumerable<Asset>, IEnumerable<Asset>>? PreFilter
        => assets => assets.FilterByWildcards(a => a?.ValueType, ValueType.ConvertToWildcardPatternList());

    protected override IEnumerable<Asset> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Assets.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, Asset asset)
    {
        drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
        drive.Assets.ClearCache(folder);
    }
}
