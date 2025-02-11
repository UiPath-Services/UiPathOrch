using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestSet", SupportsShouldProcess = true)]
[OutputType(typeof(TestSet))]
public class RemoveTestSetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var testSets = drive.TestSets.Get(folder);
         
                foreach (var testSet in testSets
                    .FilterByWildcards(ts => ts?.Name, wpName)
                    .OrderBy(ts => ts.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(testSet.GetPSPath(), "Remove TestSet"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTestSet(folder.Id ?? 0, testSet.Id ?? 0);
                            drive.TestSets.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "RemoveTestSetError", ErrorCategory.InvalidOperation, testSet));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetTestSets(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;
    //            var (drive, folder) = result.Source;

    //            foreach (var testSet in entities
    //                .FilterByWildcards(ts => ts.Name!, wpName)
    //                .OrderBy(ts => ts.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(testSet.GetPSPath(), "Remove TestSet"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveTestSet(folder.Id ?? 0, testSet.Id ?? 0);
    //                        drive._dicTestSets?.TryRemove(folder.Id.Value, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "RemoveTestSetError", ErrorCategory.InvalidOperation, testSet));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetTestSetError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
