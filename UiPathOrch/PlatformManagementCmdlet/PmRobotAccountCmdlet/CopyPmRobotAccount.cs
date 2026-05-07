using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmRobotAccount", SupportsShouldProcess = true)]
public class CopyPmRobotAccountCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmRobotAccountNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        // TODO: Shouldn't this exception be thrown from inside GetOrchDrive()?
        var srcDrive = SessionState.GetPmDrive(Path!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");

        var dstDrives = SessionState.EnumDestinationDrives(Destination!);
        var wpDisplayName = Name.ConvertToWildcardPatternList();

        srcDrive.PmRobotAccounts.ClearCache();

        try
        {
            var srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();

            var srcRobots = srcDrive.PmRobotAccounts.Get();
            var targetRobots = srcRobots
                .Where(r => r is not null)
                .FilterByWildcards(r => r!.displayName!, wpDisplayName)
                .OrderBy(r => r!.displayName)
                .ToList();

            using var reporter = new ProgressReporter(this, 1, 100, "Copying PmRobotAccount");

            using var cancelHandler = new ConsoleCancelHandler();

            reporter.TotalNum = targetRobots.Count * dstDrives.Count;
            int index = 0;
            foreach (var srcRobotAccount in targetRobots.OrderBy(r => r!.displayName))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                foreach (var dstDrive in dstDrives)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    var dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                    // Do nothing if source and destination are the same
                    if (srcPartitionGlobalId == dstPartitionGlobalId) continue;

                    try
                    {
                        reporter.WriteProgress(++index, $"{srcRobotAccount.GetPSPath()} to {dstDrive.NameColonSeparator}");

                        string target = $"Item: {System.IO.Path.Combine(srcDrive!.NameColon, srcRobotAccount!.displayName!)} Destination: {dstDrive.NameColonSeparator}";

                        if (ShouldProcess(target, "Copy PmRobotAccount"))
                        {
                            try
                            {
                                // Create a new robot with this name
                                var cmd = new CreateRobotAccountCommand()
                                {
                                    partitionGlobalId = dstPartitionGlobalId,
                                    name = srcRobotAccount.name,
                                    displayName = srcRobotAccount.displayName,
                                    groupIDsToAdd = Core.OrchProvider.FindDstPmGroups(
                                        this, srcDrive, srcRobotAccount.groupIds,
                                        dstDrive, "Copying PmRobotAccount")?.Select(group => group.id!).ToList()
                                };

                                var newRobot = dstDrive.CreatePmRobot(cmd);
                                if (newRobot is not null)
                                {
                                    WriteObject(newRobot);
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "CreatePmRobotAccountError", ErrorCategory.InvalidOperation, target));
                                continue;
                            }
                        }
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
        }
        catch (OrchException ex)
        {
            WriteError(new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target));
        }
    }
}
