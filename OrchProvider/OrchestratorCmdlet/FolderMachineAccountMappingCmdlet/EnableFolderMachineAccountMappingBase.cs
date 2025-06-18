using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableFolderMachineAccountMappingCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    virtual public string[]? Name { get; set; }

    virtual public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // PropagateToSubFolders ではないフォルダマシンだけを列挙
    // TODO: 同じものが同じフォルダの cmdlet にある
    internal class FolderMachineNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var machine in results
                .Select(r => r.Item)
                .Where(m => !m.PropagateToSubFolders.GetValueOrDefault())
                .Where(m => wp.IsMatch(m.Name))
                .ExcludeByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name))
            {
                yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, TipHelp(machine));
            }
        }
    }

    internal class UserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results
                .Where(m => !m.Item.PropagateToSubFolders.GetValueOrDefault())
                .OrderBy(m => m.Item.Name))
            {
                var (drive, folder) = result.Source;
                var folderMachine = result.Item;

                var machinesRobots = drive.MachinesRobots.Get(folder, folderMachine);

                var folderRobots = drive.FolderRobots.Get(folder, folderMachine);
                foreach (var folderRobot in folderRobots
                    .Where(fr => wp.IsMatch(fr.User!.UserName))
                    .ExcludeByWildcards(fr => fr?.User!.UserName, wpUserName)
                    .Where(fr => Enable.Value
                        ?  machinesRobots.All(mr => mr.RobotId != fr.Id)
                        : !machinesRobots.All(mr => mr.RobotId != fr.Id))
                    .OrderBy(fr => fr.User!.UserName))
                {
                    string tiphelp = $"{drive.NameColonSeparator}{folderRobot.User!.UserName}";
                    if (!string.IsNullOrEmpty(folderRobot.User.FullName))
                    {
                        tiphelp += $" ({folderRobot.User.FullName})";
                    }
                    if (!string.IsNullOrEmpty(folderRobot.Username))
                    {
                        tiphelp += $" ({folderRobot.Username})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(folderRobot.User!.UserName), folderRobot.User.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();
        var wpUserName = UserName.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        string action = Enable.Value ? "Enable" : "Disable";

        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var folderMachines = drive.FolderMachinesAssigned.Get(folder)
                    .Where(m => !m.PropagateToSubFolders.GetValueOrDefault())
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name);

                foreach (var folderMachine in folderMachines)
                {
                    try
                    {
                        var machinesRobots = drive.MachinesRobots.Get(folder, folderMachine);

                        var folderRobots = drive.FolderRobots.Get(folder, folderMachine)
                            .Where(fr => Enable.Value
                                ? machinesRobots.All(mr => mr.RobotId != fr.Id)
                                : !machinesRobots.All(mr => mr.RobotId != fr.Id))
                            .FilterByWildcards(fr => fr!.User?.UserName, wpUserName)
                            .ToList();

                        List<ExtendedRobot> enablingRobots = [];

                        foreach (var folderRobot in folderRobots)
                        {
                            if (ShouldProcess($"Machine: {folderMachine.GetPSPath()} Account: {folderRobot.User!.UserName}", action + " AccountMapping"))
                            {
                                enablingRobots.Add(folderRobot);
                            }
                        }

                        if (enablingRobots.Count == 0) continue;

                        SetMachineRobotsCmd cmd = new()
                        {
                            MachineId = folderMachine.Id,
                            FolderId = folder.Id,
                        };

                        if (Enable.Value)
                        {
                            cmd.AddedRobotIds = enablingRobots.Select(r => r.Id!.Value).ToList();
                        }
                        else
                        {
                            cmd.RemovedRobotIds = enablingRobots.Select(r => r.Id!.Value).ToList();
                        }

                        drive.OrchAPISession.SetMachineRobots(cmd);
                        drive.MachinesRobots.ClearCache(folder);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(folderMachine.GetPSPath(), ex), $"{action}FolderMachineAccountMappingError", ErrorCategory.InvalidOperation, folderMachine));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
