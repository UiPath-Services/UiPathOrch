using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchMachine", SupportsShouldProcess = true)]
public class CopyMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    internal static void CopyMachines(
        IWritableHost _this,
        OrchDriveInfo srcDrive,
        string[]? name,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        srcDrive.Machines.ClearCache();

        List<ExtendedMachine> srcMachines;
        try
        {
            srcMachines = srcDrive!.Machines.Get()
                .Where(m => m.Scope != "PersonalWorkspace")
                .FilterByNames(m => m?.Name, name)
                .OrderBy(m => m.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcDrive?.NameColonSeparator ?? "", ex), "CopyMachineError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        using var reporter = new ProgressReporter(_this, 1, 100, "Copying machines");

        int index = 0;
        reporter.TotalNum = dstDrives.Count * srcMachines.Count;

        foreach (var dstDrive in dstDrives)
        {
            if (srcDrive == dstDrive) continue;

            string target = dstDrive.NameColonSeparator;
            try
            {
                var dstRobots = dstDrive.Robots.Get();
                foreach (var machine in srcMachines)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    reporter.WriteProgress(++index, $"{machine.GetPSPath()} to {dstDrive.NameColonSeparator}");

                    string targetMachine = machine.GetPSPath();
                    if (shouldProcess || _this.ShouldProcess($"Item: {targetMachine} Destination: {dstDrive.NameColonSeparator}", $"Copy Machine"))
                    {
                        if (machine.Scope == "Cloud")
                        {
                            _this.WriteWarning($"{System.IO.Path.Combine(srcDrive.NameColonSeparator, machine.Name!)}: Copying cloud machine is not supported.");
                            continue;
                        }

                        try
                        {
                            var newMachine = OrchCollectionExtensions.DeepCopy(machine);
                            // newMachine.Path = null; // Not needed because it has a JsonIgnore attribute
                            newMachine.LicenseKey = null;
                            newMachine.Key = null;
                            newMachine.Id = null;
                            newMachine.EndpointDetectionStatus = null;
                            newMachine.RobotVersions = null;
                            newMachine.UpdateInfo = null;

                            if (machine.RobotUsers is not null && machine.RobotUsers.Any())
                            {
                                var robotUsers = new List<RobotUser>();
                                foreach (var robotUser in machine.RobotUsers)
                                {
                                    // TODO: Unify user and robot Id migration to use a static method in OrchFolderProvider.
                                    var srcRobots = srcDrive.Robots.Get();
                                    var srcRobot = srcRobots.FirstOrDefault(r => r.Id == robotUser.RobotId);

                                    if (srcRobot is null)
                                    {
                                        _this.WriteError(
                                            new ErrorRecord(new OrchException(machine.GetPSPath(), $"Robot does not exist with Id = {robotUser.RobotId}."),
                                            "FindRobotError",
                                            ErrorCategory.InvalidArgument,
                                            machine));
                                        continue;
                                    }
                                    else
                                    {
                                        var dstRobot = dstRobots.FirstOrDefault(r => string.Compare(r.Name, srcRobot.Name, StringComparison.OrdinalIgnoreCase) == 0);
                                        if (dstRobot is null)
                                        {
                                            _this.WriteError(
                                                new ErrorRecord(new OrchException(machine.GetPSPath(), $"Robot does not exist with name = {srcRobot.Name} in {dstDrive.NameColonSeparator}."),
                                                "FindRobotError",
                                                ErrorCategory.InvalidArgument,
                                                machine));
                                            continue;
                                        }
                                        else
                                        {
                                            var newRobotUser = new RobotUser();
                                            newRobotUser.UserName = robotUser.UserName!;
                                            newRobotUser.RobotId = dstRobot.Id!;
                                            robotUsers.Add(newRobotUser);
                                        }
                                    }
                                }
                                newMachine.RobotUsers = robotUsers.ToArray();
                            }

                            var addedMachine = dstDrive.OrchAPISession.AddMachine(newMachine);
                            //if (addedMachine is not null)
                            //{
                            //    addedMachine.Path = dstDrive.NameColonSeparator;
                            //    _this.WriteObject(addedMachine); // Cannot call this from a provider cmdlet...
                            //}
                            dstDrive.Machines.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            string dstTarget = $"{dstDrive.NameColonSeparator}{machine.Name}";
                            _this.WriteError(new ErrorRecord(new OrchException(dstTarget, ex), "CopyMachineError", ErrorCategory.InvalidOperation, dstTarget));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(target, ex), "CopyMachineError", ErrorCategory.InvalidOperation, target);
                _this.WriteError(errorRecord);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath)!);
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        using var cancelHandler = new ConsoleCancelHandler();
        CopyMachines(this, srcDrive, Name, dstDrives, false, cancelHandler.Token);
    }
}
