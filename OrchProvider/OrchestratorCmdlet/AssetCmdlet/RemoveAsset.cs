using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name_ValueType;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchAsset", SupportsShouldProcess = true)]
public class RemoveAssetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ValueTypeCompleter))]
    [SupportsWildcards]
    public string[]? ValueType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

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

            // パラメータで選択された Name のみ対象とする
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // パラメータで選択済みの ValueType は、候補から除外する
            var wpValueType = CreateWPListFromParameter(commandAst, "ValueType", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var asset in result.Result
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

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpValueType = ValueType.ConvertToWildcardPatternList();
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var assets = drive.Assets.Get(folder);

                foreach (var asset in assets
                    .FilterByWildcards(asset => asset?.ValueType, wpValueType)
                    .FilterByWildcards(asset => asset?.Name, wpName)
                    .OrderBy(asset => asset.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    try
                    {
                        if (ShouldProcess(asset.GetPSPath(), "Remove Asset"))
                        {
                            drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            drive.Assets.ClearCache(folder);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(asset.GetPSPath(), ex), "RemoveAssetError", ErrorCategory.InvalidOperation, asset));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetAssetError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
    //    var wpValueType = ValueType.ConvertToWildcardPatternList();
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetAssets(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var assets = result.GetResult(cancelHandler.Token);
    //            if (assets is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var asset in assets
    //                .FilterByWildcards(asset => asset.ValueType!, wpValueType)
    //                .FilterByWildcards(asset => asset.Name!, wpName)
    //                .OrderBy(asset => asset.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                try
    //                {
    //                    if (ShouldProcess(asset.GetPSPath(), "Remove Asset"))
    //                    {
    //                        drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
    //                        drive._dicAssets?.TryRemove(folder.Id.Value, out _);
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    WriteError(new ErrorRecord(new OrchException(asset.GetPSPath(), ex), "RemoveAssetError", ErrorCategory.InvalidOperation, asset));
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetAssetError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
