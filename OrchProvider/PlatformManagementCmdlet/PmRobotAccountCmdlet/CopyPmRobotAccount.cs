using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchPmRobotAccount", SupportsShouldProcess = true)]
    public class CopyPmRobotAccountCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmRobotAccountNameCompleter<TPositional>))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string? Path { get; set; }

        protected override void ProcessRecord()
        {
            // TODO: この例外は GetOrchDrive() の中からスローして良いのではないか？
            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!) ?? throw new Exception("Path is not OrchDrive.");

            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);
            var wpDisplayName = Name.ConvertToWildcardPatternList();

            srcDrive.PmRobotAccounts.ClearCache();

            try
            {
                var srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();

                var srcRobots = srcDrive.PmRobotAccounts.Get();
                var targetRobots = srcRobots
                    .Where(r => r != null)
                    .FilterByWildcards(r => r!.displayName!, wpDisplayName)
                    .OrderBy(r => r!.displayName)
                    .ToList();

                string msg = "Copying PmRobotAccount";
                using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

                using var cancelHandler = new ConsoleCancelHandler();

                reporter.TotalNum = targetRobots.Count * dstDrives.Count;
                int index = 0;
                foreach (var srcRobotAccount in targetRobots.OrderBy(r => r!.displayName))
                {
                    foreach (var dstDrive in dstDrives)
                    {
                        try
                        {
                            var dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                            // コピー元とコピー先が同じ場合は何もしない
                            if (srcPartitionGlobalId == dstPartitionGlobalId)
                            {
                                index += targetRobots.Count;
                                continue;
                            }

                            reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {srcRobotAccount.GetPSPath()} to {dstDrive.NameColonSeparator}");

                            string target = $"Item: {System.IO.Path.Combine(srcDrive!.NameColon, srcRobotAccount!.displayName!)} Destination: {dstDrive.NameColonSeparator}";

                            // この名前のロボットを新規追加
                            var cmd = new CreateRobotAccountCommand()
                            {
                                partitionGlobalId = dstPartitionGlobalId,
                                name = srcRobotAccount.name,
                                displayName = srcRobotAccount.displayName,
                                groupIDsToAdd = Core.OrchProvider.FindDstPmGroups(
                                    this, srcDrive, srcRobotAccount.groupIds,
                                    dstDrive, "Copying PmRobotAccount")?.Select(group => group.id!).ToList()
                            };

                            if (ShouldProcess(target, "Copy PmRobotAccount"))
                            {
                                try
                                {
                                    var newRobot = dstDrive.OrchAPISession.CreatePmRobot(cmd);
                                    dstDrive.PmRobotAccounts.ClearCache();
                                    dstDrive._dicPmDirectoryUsers = null;
                                    dstDrive._dicSearchForUsersAndGroups = null;
                                    if (newRobot != null)
                                    {
                                        newRobot.Path = dstDrive.NameColonSeparator;
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
}
