using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmTestCase", SupportsShouldProcess = true)]
//[OutputType(typeof(Entities.TmTestCase))]
public class RemoveTmTestCaseCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var driveProject in drivesProjects)
        {
            var (drive, project) = driveProject;

            try
            {
                var entities = drive.GetTmTestCases(project);

                foreach (var testCase in entities
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(testCase.GetPSPath(), "Remove TmTestCase"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmTestCase(project.id!, testCase.id!);
                            drive._dicTmTestCases = null;
                            drive._dicTmTestCasesExceptions.ClearCache(project.id);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "RemoveTmTestCaseError", ErrorCategory.InvalidOperation, testCase));
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
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseError", ErrorCategory.InvalidOperation, project));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //    protected override void ProcessRecord()
    //    {
    //        var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
    //        var wpName = Name.ConvertToWildcardPatternList();

    //        using var results = OrchThreadPool.RunForEach(drivesProjects,
    //            dp => dp.project.GetPSPath(),
    //            dp => dp.project,
    //            dp => dp.drive.GetTmTestCases(dp.project));

    //        using var cancelHandler = new ConsoleCancelHandler();
    //        foreach (var result in results)
    //        {
    //            try
    //            {
    //                var entities = result.GetResult(cancelHandler.Token);
    //                if (entities is null) continue;

    //                var (drive, project) = result.Source;

    //                foreach (var testCase in entities
    //                    .FilterByWildcards(e => e.name!, wpName)
    //                    .OrderBy(e => e.name))
    //                {
    //                    if (ShouldProcess(testCase.GetPSPath(), "Remove TmTestCase"))
    //                    {
    //                        try
    //                        {
    //                            drive.OrchAPISession.RemoveTmTestCase(project.id!, testCase.id!);
    //                            drive._dicTmTestCases = null;
    //                            drive._dicTmTestCasesExceptions.TryRemove(project.id!);
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                            WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "RemoveTmTestCaseError", ErrorCategory.InvalidOperation, testCase));
    //                        }
    //                    }
    //                }
    //            }
    //            catch (OrchException ex)
    //            {
    //                WriteError(new ErrorRecord(ex, "GetTmTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
    //            }
    //        }
    //    }
}
