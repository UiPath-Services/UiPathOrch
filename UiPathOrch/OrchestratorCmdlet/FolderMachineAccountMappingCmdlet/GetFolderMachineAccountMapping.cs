using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchFolderMachineAccountMapping")]
[OutputType(typeof(Entities.ExtendedRobot))]
public class GetFolderMachineAccountMappingCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Only enumerate folder machines that are not PropagateToSubFolders
    internal class FolderMachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude Names already selected via parameter from the candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var machine in result
                    .Where(m => !m.PropagateToSubFolders.GetValueOrDefault())
                    .Where(m => wp.IsMatch(m.Name))
                    .ExcludeByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, TipHelp(machine));
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
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
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting folder machine account mappings");
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var robotsList = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
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

        // A single-threaded implementation would look like this
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

