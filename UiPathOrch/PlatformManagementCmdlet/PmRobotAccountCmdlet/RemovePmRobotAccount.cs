using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmRobotAccount", SupportsShouldProcess = true)]
public class RemovePmRobotAccountCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmRobotAccountNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entities = drive.PmRobotAccounts.Get();
                var partitionGlobalId = drive!.GetPartitionGlobalId();

                foreach (var robot in entities
                    .Where(r => r is not null)
                    .FilterByWildcards(r => r!.name!, wpName)
                    .OrderBy(r => r!.name)
                    .WithProgressBar(this, $"Removing PmRobotAccounts in {drive.NameColonSeparator}", r => r!.name)
                    .WithCancellation(cancelHandler.Token))
                {
                    string target = robot.GetPSPath(drive.NameColonSeparator);
                    if (ShouldProcess(target, "Remove PmRobotAccount"))
                    {
                        try
                        {
                            drive!.OrchAPISession.RemovePmRobot(partitionGlobalId!, robot.id!);
                            drive.PmRobotAccounts.ClearCache();
                            drive.PmGroups.ClearCache();
                            drive.SearchPmDirectoryCache.ClearCache();
                            drive.SearchDirectoryCache.ClearCache();
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
}
