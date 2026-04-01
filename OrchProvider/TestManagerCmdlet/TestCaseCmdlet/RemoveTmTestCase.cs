using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmTestCaseCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var driveProject in drivesProjects)
        {
            var (drive, project) = driveProject;

            try
            {
                var entities = drive.TmTestCases.Get(project);

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
                            drive.TmTestCases.ClearCache();
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

    // Multi-threaded version
    // Rewritten as single-threaded because it can be slower when HTTP calls are capped
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
