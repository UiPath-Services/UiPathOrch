using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchMachineClientSecret", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.MachineSecretKey))]
public class AddMachineClientSecretCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    // Scope が "PersonalWorkspace" と "AutomationCloudRobot" のマシンは除外するため、この completer は共通化できない
    // 除外する Scope をパラメータ化して、処理を共通にした方が良さそうだけど、ちと面倒。。
    // というか、列挙する部分とかフィルタする部分をパラメータ化するクラスを導入して
    // より一般に共通化できるのではないか？
    private class MachineNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drives, drive => drive.Machines.Get());

            bool bFound = false;
            foreach (var machine in results
                .Select(r => r.Item)
                .Where(m => wp.IsMatch(m.Name))
                .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot") // 共通の処理との差異はここだけ
                .ExcludeByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name!))
            {
                bFound = true;
                string tiphelp = TipHelp(machine);
                yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
            }

            if (!bFound)
            {
                yield return new CompletionResult("'(No credential stores found)'");
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        var wpName = Name.Split1stValueByUnescapedCommas()?.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            var machines = drive.Machines.Get();
            var targetMachines = machines
                .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot")
                .FilterByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name);

            foreach (var m in targetMachines)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                if (ShouldProcess(m.GetPSPath(), "Add ClientSecret"))
                {
                    try
                    {
                        var key = drive.OrchAPISession.AddMachineClientSecret(m.LicenseKey!);
                        drive._dicMachineClientSecrets = null;
                        if (key is not null)
                        {
                            MachineSecretKey output = new()
                            {
                                Path = drive.NameColonSeparator,
                                Name = m.Name,
                                ClientId = m.LicenseKey,
                                SecretId = key.id,
                                ClientSecret = key.secret,
                                CreationTime = key.creationTime,
                                Description = m.Description,
                                Type = m.Type,
                                Scope = m.Scope
                            };
                            WriteObject(output);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(m.GetPSPath(), ex), "AddMachineClientSecretError", ErrorCategory.InvalidOperation, m));
                    }
                }
            }
        }
    }
}
