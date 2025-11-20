using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmTestSet", SupportsShouldProcess = true)]
public class RemoveTmTestSetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter<TPositional>))]
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
                var entity = drive.TmTestSets.Get(project);

                foreach (var testSet in entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(testSet.GetPSPath(), "Remove TmTestSet"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmTestSet(project.id!, testSet.id!);
                            drive.TmTestSets.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "RemoveTmTestSetError", ErrorCategory.InvalidOperation, testSet));
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
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestSetError", ErrorCategory.InvalidOperation, project));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drivesProjects,
    //        dp => dp.project.GetPSPath(),
    //        dp => dp.project,
    //        dp => dp.drive.GetTmTestSets(dp.project));

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entity = result.GetResult(cancelHandler.Token);
    //            if (entity is null) continue;

    //            var (drive, project) = result.Source;

    //            foreach (var testSet in entity
    //                .FilterByWildcards(e => e.name!, wpName)
    //                .OrderBy(e => e.objKey!, ObjKeyComparer.Instance))
    //            {
    //                if (ShouldProcess(testSet.GetPSPath(), "Remove TmTestSet"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveTmTestSet(project.id!, testSet.id!);
    //                        drive._dicTmTestSets?.TryRemove(project.id!, out var _);
    //                        drive._dicTmTestSetsExceptions.TryRemove(project.id!);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "RemoveTmTestSetError", ErrorCategory.InvalidOperation, testSet));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetTmTestSetError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
