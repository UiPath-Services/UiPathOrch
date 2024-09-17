using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchPmRobotAccount", SupportsShouldProcess = true)]
    public class CopyPmRobotAccountCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmRobotAccountNameCompleter<Positional.Name_Destination>))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_Destination>))]
        public string[]? Path { get; set; }

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
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

                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.Name_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDrives = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDrives.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.Name_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

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
            var srcDrives = OrchDriveInfo.EnumOrchDrives(Path);
            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);
            var wpDisplayName = Name.ConvertToWildcardPatternList();

            foreach (var srcDrive in srcDrives)
            {
                srcDrive._dicPmRobotAccounts = null;
                srcDrive._dicPmRobotAccounts_Exceptions.ClearCache();
            }

            using var results = OrchThreadPool.RunForEach(srcDrives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetPmRobotAccounts());

            string msg = "Copying PmRobotAccount";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    var srcDrive = result.Source!;

                    int index = 0;
                    reporter.TotalNum = entities.Count * dstDrives.Count;

                    foreach (var dstDrive in dstDrives)
                    {
                        var srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
                        var dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                        // コピー元とコピー先が同じ場合は何もしない
                        if (srcPartitionGlobalId == dstPartitionGlobalId) continue;

                        foreach (var srcRobot in entities.Values
                            .Where(r => r != null)
                            .FilterByWildcards(r => r!.displayName!, wpDisplayName)
                            .OrderBy(r => r!.displayName))
                        {
                            reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {srcRobot.GetPSPath()} to {dstDrive.NameColonSeparator}");

                            string target = $"Item: {System.IO.Path.Combine(srcDrive!.NameColon, srcRobot!.displayName!)} Destination: {dstDrive.NameColonSeparator}";
                            if (ShouldProcess(target, "Copy Identity Robot Account"))
                            {
                                // この名前のロボットを新規追加
                                var cmd = new CreateRobotAccountCommand()
                                {
                                    partitionGlobalId = dstPartitionGlobalId,
                                    name = srcRobot.name,
                                    displayName = srcRobot.displayName,
                                    groupIDsToAdd = Core.OrchProvider.FindDstIdGroups(
                                        this, srcDrive, srcRobot.groupIds,
                                        dstDrive, "Copying Identity Robot Account")?.Select(group => group.id!).ToList()
                                };

                                try
                                {
                                    var newRobot = dstDrive.OrchAPISession.CreatePmRobot(cmd);
                                    if (newRobot != null)
                                    {
                                        //newRobot.Path = dstDrive.NameColonSeparator;
                                        //WriteObject(newRobot);
                                        dstDrive._dicPmRobotAccounts![newRobot.id!] = newRobot;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "CreatePmRobotAccountError", ErrorCategory.InvalidOperation, target));
                                    continue;
                                }
                            }
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target));
                    continue;
                }
            }
        }
    }
}
