using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands;

// このコマンドレットからは、メンバーを追加する機能は外す。
// 空っぽのグループを追加するだけのコマンドレットでないと、ShouldProcess がうまいことサポートできないため。

[Cmdlet(VerbsCommon.New, "PmGroup", SupportsShouldProcess = true)]
public class AddPmGroupCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewPmGroupNameCompleter))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class NewPmGroupNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);
            var results = ParallelResults2.ForEachMany(drives, drive => drive.GetPmGroups());

            // パラメータで選択済みの Name は、候補から除外する
            var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            yield return new CompletionResult(GenerateNewEntityName("NewGroup", names, results, e => e.Item.Value.name!));
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumPmDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            var partitionGlobalId = drive.GetPartitionGlobalId();
            foreach (var groupName in GroupName!)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string target = System.IO.Path.Combine(drive.NameColonSeparator, groupName);
                if (ShouldProcess(target, "New PmGroup"))
                {
                    try
                    {
                        var newGroup = drive.CreatePmGroup(WildcardPattern.Unescape(groupName));
                        if (newGroup is not null)
                        {
                            WriteObject(newGroup);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewPmGroupError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
