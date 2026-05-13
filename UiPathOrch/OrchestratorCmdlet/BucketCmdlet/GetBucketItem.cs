using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchBucketItem")]
[OutputType(typeof(BlobFile))]
public class GetBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketFullPathCompleter))]
    [SupportsWildcards]
    public string[]? FullPath { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
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
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetBucketError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
