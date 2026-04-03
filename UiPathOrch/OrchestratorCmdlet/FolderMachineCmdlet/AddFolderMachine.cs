using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchFolderMachine", SupportsShouldProcess = true)]
public class AddFolderMachineCommand : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo Drive, Folder Folder), Dictionary<MachineFolder, bool?>>? _csvLines = null;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? PropagateToSubFolders { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
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

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssignable.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var entity in result
                    .Where(b => wp.IsMatch(b.Name))
                    .ExcludeByWildcards(b => b?.Name, wpName)
                    .OrderBy(b => b.Name))
                {
                    string tooltip = entity.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(entity.Name), entity.Name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.FolderMachinesAssignable.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var machines = result.GetResult(cancelHandler.Token);
                if (machines is null) continue;
                var (drive, folder) = result.Source;

                var addingMachines = machines!
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name)
                    .ToList();

                string targetFolder = folder.GetPSPath();
                foreach (var targetMachine in addingMachines)
                {
                    if (string.IsNullOrEmpty(targetMachine.Name)) continue;

                    string target = $"Item: {targetMachine.Name} Destination: {folder.GetPSPath()}";
                    if (ShouldProcess(target, "Add Folder Machine"))
                    {
                        _csvLines ??= [];
                        if (!_csvLines.TryGetValue((drive, folder), out var entry))
                        {
                            entry = [];
                            _csvLines[(drive, folder)] = entry;
                        }
                        entry[targetMachine] = PropagateToSubFolders.ToNullableBool();
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_csvLines == null) return;

        var sortedParameters = _csvLines
            .OrderBy(kv => kv.Key.Drive.Name)
            .ThenBy(kv => kv.Key.Folder.FullyQualifiedNameOrderable);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var param in sortedParameters)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            var (drive, folder) = param.Key;
            var machinesPropagatesPairs = param.Value;
            var machineIds = machinesPropagatesPairs.Keys.Where(m => m.Id is not null).Select(m => m.Id!.Value).ToList();
            if (machineIds.Count == 0) continue;
            try
            {
                drive.OrchAPISession.AddMachinesToFolder(folder.Id ?? 0, machineIds);
                drive.FolderMachinesAssigned.ClearCache(folder);
                drive.FolderMachinesAssignable.ClearCache(folder);
                drive.MachinesRobots.ClearCache(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "AddFolderMachineError", ErrorCategory.InvalidOperation, folder));
            }

            foreach (var (machine, propagateToSubFolders) in machinesPropagatesPairs)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                if (propagateToSubFolders.GetValueOrDefault())
                {
                    try
                    {
                        drive.OrchAPISession.SetFolderMachineInherit(folder.Id!.Value, machine.Id!.Value, propagateToSubFolders.GetValueOrDefault());
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(machine.GetPSPath(), ex), "SetFolderMachineInheritError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
