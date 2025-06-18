using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchFolderMachineAccountMapping")]
[OutputType(typeof(Entities.ExtendedRobot))]
public class GetFolderMachineAccountMappingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

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
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

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

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var (drive, folder) = df;
                var folderMachines = drive.FolderMachinesAssigned.Get(folder)
                    .Where(m => !m.PropagateToSubFolders.GetValueOrDefault())
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name);

                List<List<ExtendedRobot>> ret = [];
                foreach (var folderMachine in folderMachines)
                {
                    var machinesRobots = drive.MachinesRobots.Get(folder, folderMachine);
                    if (machinesRobots.Count == 0) continue;

                    var folderRobots = drive.FolderRobots.Get(folder, folderMachine)
                        .Where(fr => machinesRobots.Any(mr => mr.RobotId == fr.Id)).ToList();
                    ret.Add(folderRobots);
                }
                return ret;
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            try
            {
                var robotsList = result.GetResult(cancelHandler.Token);
                if (robotsList is null) continue;

                foreach (var robots in robotsList)
                {
                    WriteObject(robots.OrderBy(r => r.User?.FullName), true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderMachineAccountError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // シングルスレッドで実装すると、次のようになる
        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    var folderMachines = drive.FolderMachinesAssigned.Get(folder)
        //        .Where(m => !m.PropagateToSubFolders.GetValueOrDefault())
        //        .FilterByWildcards(m => m.Name, wpName)
        //        .OrderBy(m => m.Name);

        //    foreach (var fm in folderMachines)
        //    {
        //        var machinesRobots = drive.MachinesRobots.Get(folder, fm);
        //        var folderRobots = drive.FolderRobots.Get(folder, fm);

        //        foreach (var folderRobot in folderRobots)
        //        {
        //            folderRobot.MappingEnabled = machinesRobots.Any(mr => mr.RobotId == folderRobot.Id);
        //            WriteObject(folderRobot);
        //        }
        //    }
        //}
    }
}

