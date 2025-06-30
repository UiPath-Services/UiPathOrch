using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmRobotAccount", SupportsShouldProcess = true)]
public class RemovePmRobotAccountCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmRobotAccountNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmRobotAccounts.Get();
                var partitionGlobalId = drive!.GetPartitionGlobalId();

                foreach (var robot in entities
                    .Where(r => r is not null)
                    .FilterByWildcards(r => r!.name!, wpName)
                    .OrderBy(r => r!.name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = robot.GetPSPath();
                    if (ShouldProcess(target, "Remove PmRobotAccount"))
                    {
                        try
                        {
                            drive!.OrchAPISession.RemovePmRobot(partitionGlobalId!, robot.id!);
                            drive.PmRobotAccounts.ClearCache();
                            drive.PmGroups.ClearCache();
                            drive._dicSearchPmDirectory = null;
                            drive._dicSearchDirectory = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmRobotAccountError", ErrorCategory.InvalidOperation, robot));
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmRobotAccountError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetPmRobotAccounts()
    //    );

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var drive = result.Source;

    //            var partitionGlobalId = drive!.GetPartitionGlobalId();

    //            foreach (var robot in entities.Values
    //                .Where(r => r is not null)
    //                .FilterByWildcards(r => r!.name!, wpName)
    //                .OrderBy(r => r!.name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                string target = robot.GetPSPath();
    //                if (ShouldProcess(target, "Remove PmRobotAccount"))
    //                {
    //                    try
    //                    {
    //                        drive!.OrchAPISession.RemoveIdentityRobot(partitionGlobalId!, robot.id!);
    //                        drive._dicPmRobotAccounts?.Remove(robot.id ?? "", out var _);
    //                        drive._dicPmGroups = null;
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmRobotAccountError", ErrorCategory.InvalidOperation, robot));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
