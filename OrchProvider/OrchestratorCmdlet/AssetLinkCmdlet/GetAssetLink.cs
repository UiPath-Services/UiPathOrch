using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAssetLink")]
[OutputType(typeof(Entities.SimpleFolder))]
public class GetAssetLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

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

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => wp.IsMatch(a.Name))
                    .Where(a => (a.UserValues is null || !a.UserValues.Any()) && a.ValueScope != "PerRobot")
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

    // TODO: This implementation can be cleaned up
    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
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

        HashSet<(Int64 folderId, string assetName)> outputLink = new();
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

                if (accessibleFolders is not null &&
                    accessibleFolders.AccessibleFolders is not null &&
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
