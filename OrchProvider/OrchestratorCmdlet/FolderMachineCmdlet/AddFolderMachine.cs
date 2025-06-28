using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name_PropagateToSubFolders;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchFolderMachine", SupportsShouldProcess = true)]
public class AddFolderMachineCommand : OrchestratorPSCmdlet
{
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

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssignable.Get(df.folder));

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
        // この修正をしておくことが必要だ。そうでないと、CSV インポート時に解決できない Path があると
        // CSV の後続の行の処理が全部キャンセルされてしまう。。
        // 全ての cmdlet をまとめて修正したい。
        // OrchDriveInfo.EnumFolders() の引数に IWritableHost を渡して、エラー処理を呼び出し先で行う方が安全に漏れなく修正できそうだ。
        //List<(OrchDriveInfo drive, Folder folder)> drivesFolders;
        //try
        //{
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        //}
        //catch (Exception ex)
        //{
        //    WriteError(new ErrorRecord(new OrchException(string.Join(',', Path!), ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, this));
        //    return;
        //}

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

                if (folder.FolderType == "Personal") continue;

                var addingMachines = machines!.FilterByWildcards(m => m?.Name, wpName).ToList();
                // TODO: これ入れないと。このままだといまいちな感じ。Name を複数指定したとき、ひとつでも合致すれば警告が出ない。
                // Name をひとつずつ確認していかないといけない。
                //if (addingMachines.Count == 0)
                //{
                //    WriteWarning($"'{folder.GetPSPath()}': No matching machine found with '{string.Join(',', Name!)}' in {drive.NameColonSeparator}.");
                //    continue;
                //}

                string targetFolder = folder.GetPSPath();
                try
                {
                    var machineIds = addingMachines.Select(m => m.Id ?? 0);
                    string targetMachines = string.Join(", ", addingMachines.Select(m => m.Name!));

                    string target = $"Item: {targetMachines} Destination: {folder.GetPSPath()}";
                    if (ShouldProcess(target, "Add Folder Machines"))
                    {
                        drive.OrchAPISession.AddMachinesToFolder(folder.Id ?? 0, machineIds);
                        drive.FolderMachinesAssigned.ClearCache(folder);
                        drive.FolderMachinesAssignable.ClearCache(folder);
                        drive.MachinesRobots.ClearCache(folder);

                        // 非 null の場合に処理する
                        // machine を add するだけなら true の場合のみ処理すればいいのだけど
                        // 実際には、machine を update する場合もあるから、false も処理しないといけない。
                        // 既存のと同じマシンを add しただけでは、既存のマシンの PropagateToSubFolders は変化しない。
                        // これ、Update-OrchFolderMachine も作るべきなのか？
                        bool? bPropagateToSubFolders = PropagateToSubFolders.ToNullableBool();
                        if (bPropagateToSubFolders is not null)
                        {
                            foreach (var machineId in machineIds)
                            {
                                try
                                {
                                    drive.OrchAPISession.SetFolderMachineInherit(folder.Id!.Value, machineId, bPropagateToSubFolders.Value);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(targetFolder, ex), "SetFolderMachineInheritError", ErrorCategory.InvalidOperation, folder));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(targetFolder, ex), "AddFolderMachineError", ErrorCategory.InvalidOperation, folder));
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
