using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchMachine", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.ExtendedMachine))]
    public class CopyMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string? Path { get; set; }

        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // コピー元のドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path");
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var wpDestination = CreateWPListFromParameter(commandAst, "Destination", Positional.Name_Destination.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();
            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!);
            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            srcDrive._dicExtendedMachines = null;
            srcDrive._dicExtendedMachines_Exception.ClearCache();

            List<ExtendedMachine> srcMachines;
            try
            {
                srcMachines = srcDrive!.GetMachines()
                    .Where(m => m.Scope != "PersonalWorkspace")
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive?.NameColonSeparator ?? "", ex), "CopyMachineError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }

            string msg = "Copying machines";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            int index = 0;
            reporter.TotalNum = dstDrives.Count * srcMachines.Count;

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var dstDrive in dstDrives)
            {
                if (srcDrive == dstDrive) continue;

                string target = dstDrive.NameColonSeparator;
                try
                {
                    var dstRobots = dstDrive.GetRobots();
                    foreach (var machine in srcMachines)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {machine.GetPSPath()} to {dstDrive.NameColonSeparator}");

                        string targetMachine = machine.GetPSPath();
                        if (ShouldProcess($"Item: {targetMachine} Destination: {dstDrive.NameColonSeparator}", $"Copy Machine"))
                        {
                            if (machine.Scope == "Cloud")
                            {
                                WriteWarning($"{System.IO.Path.Combine(srcDrive.NameColonSeparator, machine.Name!)}: Copying cloud machine is not supported.");
                                continue;
                            }

                            try
                            {
                                var newMachine = OrchCollectionExtensions.DeepCopy(machine);
                                // newMachine.Path = null; // JsonIgnore 属性がついているので不要
                                newMachine.LicenseKey = null;
                                newMachine.Key = null;
                                newMachine.Id = null;
                                newMachine.EndpointDetectionStatus = null;
                                newMachine.RobotVersions = null;
                                newMachine.UpdateInfo = null;

                                if (machine.RobotUsers != null && machine.RobotUsers.Any())
                                {
                                    var robotUsers = new List<RobotUser>();
                                    foreach (var robotUser in machine.RobotUsers)
                                    {
                                        // TODO: ユーザーとロボットの Id の移行を、OrchFolderProvider の static method で行うように統一。

                                        // UserName
                                        var newRobotUser = new RobotUser();
                                        newRobotUser.UserName = robotUser.UserName!;

                                        // RobotId
                                        var newRobotIds = dstRobots.Where(r => r.Username == robotUser.UserName);
                                        if (!newRobotIds.Any())
                                        {
                                            throw new Exception($"There is no robot with the UserName set to {robotUser.UserName}.");
                                        }
                                        if (newRobotIds.Take(2).Count() == 2)
                                        {
                                            throw new Exception($"There are multiple robots with the UserName set to {robotUser.UserName}.");
                                        }
                                        newRobotUser.RobotId = newRobotIds.First().Id!;

                                        // HasTriggers
                                        // need to set "HasTriggers"? Not sure, not set for now..
                                        newRobotUser.HasTriggers = false; // robotUser.HasTriggers;

                                        robotUsers.Add(newRobotUser);
                                    }
                                    newMachine.RobotUsers = robotUsers;
                                }

                                var addedMachine = dstDrive.OrchAPISession.AddMachine(newMachine);
                                if (addedMachine != null)
                                {
                                    addedMachine.Path = dstDrive.NameColonSeparator;
                                    WriteObject(addedMachine);
                                }
                                dstDrive._dicExtendedMachines = null;
                                dstDrive._dicMachinesAssignable = null;
                            }
                            catch (Exception ex)
                            {
                                string dstTarget = $"{dstDrive.NameColonSeparator}{machine.Name}";
                                var errorRecord = new ErrorRecord(new OrchException(dstTarget, ex), "CopyMachineError", ErrorCategory.InvalidOperation, dstTarget);
                                WriteError(errorRecord);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "CopyMachineError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                }
            }
        }
    }
}
