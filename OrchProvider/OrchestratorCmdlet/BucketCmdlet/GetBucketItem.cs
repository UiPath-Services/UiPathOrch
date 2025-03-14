using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_FullPath;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchBucketItem")]
[OutputType(typeof(BlobFile))]
public class GetBucketItemCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FullPathCompleter))]
    [SupportsWildcards]
    public string[]? FullPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class FullPathCompleter : OrchArgumentCompleter
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

            // パラメータで選択済みの FullPath は、候補から除外する
            var wpFullPath = CreateWPListFromParameter(commandAst, "FullPath", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df =>
            {
                var buckets = df.drive.Buckets.Get(df.folder).FilterByWildcards(e => e?.Name, wpName);
                return ParallelResults.ForEach(buckets, bucket =>
                    df.drive.BucketFiles.Get(df.folder, bucket));
            });

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var bucket in entities!)
                {
                    if (!bucket.TryGetValue(out var items)) continue;

                    foreach (var item in items!
                        .Where(e => wp.IsMatch(e.FullPath))
                        .ExcludeByWildcards(e => e?.FullPath, wpFullPath)
                        .OrderBy(e => e.FullPath))
                    {
                        string tiphelp = TipHelp(item);
                        yield return new CompletionResult(PathTools.EscapePSText(item.FullPath), item.FullPath, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpFullPath = FullPath.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Buckets.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var (drive, folder) = result.Source;

                foreach (var bucket in entities
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    try
                    {
                        var files = drive.BucketFiles.Get(folder, bucket);
                        WriteObject(files
                            .FilterByWildcards(file => file?.FullPath, wpFullPath)
                            .OrderBy(file => file.FullPath),
                            true);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(bucket.GetPSPath(), ex), "GetBucketFileError", ErrorCategory.InvalidOperation, bucket));
                    }
                }
            }
            catch(OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetBucketError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
