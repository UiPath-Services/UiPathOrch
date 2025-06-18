using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

public class EnableFolderMachineInheritCommandBase<EnableInherit> : OrchestratorPSCmdlet where EnableInherit : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    internal class FolderMachineNameCompleter : OrchArgumentCompleter
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

            foreach (var machineFolder in results
                .Select(r => r.Item)
                .Where(m => wp.IsMatch(m.Name))
                .Where(m => m.IsAssignedToFolder.GetValueOrDefault())
                .Where(m => EnableInherit.Value
                    ? !m.PropagateToSubFolders.GetValueOrDefault()
                    : m.PropagateToSubFolders.GetValueOrDefault())
                .ExcludeByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name))
            {
                yield return new CompletionResult(PathTools.EscapePSText(machineFolder.Name), machineFolder.Name, CompletionResultType.ParameterValue, TipHelp(machineFolder));
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(EnableInherit.Value ? "Enable" : "Disable")} FolderMachineInherit";

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.FolderMachinesAssigned.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var machines = result.GetResult(cancelHandler.Token);
                if (machines is null) continue;

                var (drive, folder) = result.Source;

                foreach (var machine in machines
                    .Where(m => m.IsAssignedToFolder.GetValueOrDefault())
                    .Where(m => EnableInherit.Value
                        ? !m.PropagateToSubFolders.GetValueOrDefault()
                        : m.PropagateToSubFolders.GetValueOrDefault())
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name))
                {
                    if (ShouldProcess(machine.GetPSPath(), action))
                    {
                        try
                        {
                            drive.OrchAPISession.SetFolderMachineInherit(folder.Id!.Value, machine.Id!.Value, EnableInherit.Value);
                            // 本当は、このフォルダーとそのサブフォルダーのキャッシュだけをクリアすべきだが、
                            drive.FolderMachinesAssigned.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(EnableInherit.Value ? "Enable" : "Disable")}FolderMachineInheritError";
                            WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
