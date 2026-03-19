using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestCase", SupportsShouldProcess = true)]
public class RemoveTestCaseCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestCaseNameCompleter))]
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
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var testCases = drive.TestCases.Get(folder);
                foreach (var testCase in testCases
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(testCase.GetPSPath(), "Remove TestCase"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTestCases(folder.Id ?? 0, [testCase.Id ?? 0]);
                            drive.TestCases.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "RemoveTestCaseError", ErrorCategory.InvalidOperation, testCase));
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestCaseError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // Multi-threaded version
    // Rewrote as single-threaded because it can be slower when HTTP calls are capped
    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesFolders,
    //        df => df.folder.GetPSPath(),
    //        df => df.folder,
    //        df => df.drive.GetTestCases(df.folder));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var (drive, folder) = result.Source;

    //            foreach (var testCase in entities.FilterByWildcards(e => e.Name!, wpName))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(testCase.GetPSPath(), "Remove TestCase"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveTestCases(folder.Id ?? 0, [testCase.Id ?? 0]);
    //                        drive._dicTestCases?.TryRemove(folder.Id ?? 0, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "RemoveTestCaseError", ErrorCategory.InvalidOperation, testCase));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
