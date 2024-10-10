using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;
using Template = UiPath.PowerShell.Positional.Template;
using Any_Foreground_Background = UiPath.PowerShell.Positional.Any_Foreground_Background;
using Any_Windows_Portable  = UiPath.PowerShell.Positional.Any_Windows_Portable;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchMachineClientSecret", SupportsShouldProcess = true)]
    public class RemoveMachineSecretKeyCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter<Name_SecretId>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(SecretIdCompleter))]
        [SupportsWildcards]
        public string[]? SecretId { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Name_SecretId>))]
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
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachines());

                bool bFound = false;
                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var machine in entities!
                        .Where(m => wp.IsMatch(m.Name))
                        .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot") // 共通の処理との差異はここだけ
                        .ExcludeByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name!))
                    {
                        bFound = true;
                        string tiphelp = TipHelp(machine);
                        yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
                if (!bFound)
                {
                    yield return new CompletionResult("'(No credential stores found)'");
                }
            }
        }

        private class SecretIdCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                var wpName = CreateWPListFromOtherParameters(commandAst, "Name", Name_SecretId.Parameters);

                // パラメータで選択済みの SecretId は、候補から除外する
                var wpSecretId = CreateWPListFromParameter(commandAst, "SecretId", Name_SecretId.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachines());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var machine in entities!
                        .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot") // 共通の処理との差異はここだけ
                        .FilterByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name!))
                    {
                        if (machine.LicenseKey == null) continue;

                        var secrets = drive.GetMachineClientSecret(machine.LicenseKey.Value);
                        if (secrets == null) continue;

                        foreach (var secret in secrets
                            .Select(secret => (secret, secret.id.ToString()))
                            .Where(secret_id => wp.IsMatch(secret_id.Item2))
                            .ExcludeByWildcards(secret_id => secret_id.Item2, wpSecretId)
                            .OrderBy(secret_id => secret_id.Item2))
                        {
                            string id = secret.Item2;
                            string target = System.IO.Path.Combine(machine.GetPSPath(), secret.Item2!);
                            string tiphelp = $"{target}  Created: {secret.Item1.creationTime}  ClientId: {machine.LicenseKey}";
                            yield return new CompletionResult(PathTools.EscapePSText(id), id, CompletionResultType.ParameterValue, tiphelp);
                        }
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpName = Name.Split1stValueByUnescapedCommas()?.ConvertToWildcardPatternList();
            var wpSecretId = SecretId.Split1stValueByUnescapedCommas()?.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                IEnumerable<ExtendedMachine> machines = null;
                try
                {
                    machines = drive.GetMachines();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachinesError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }

                var targetMachines = machines
                    .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot")
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name);

                foreach (var m in targetMachines)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (m.LicenseKey == null) continue;

                    MachineClientSecretResponse[] secrets = null;
                    try
                    {
                        secrets = drive.GetMachineClientSecret(m.LicenseKey.Value);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachineClientSecretsError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                    if (secrets == null) continue;

                    foreach (var secret in secrets
                        .Select(secret => (secret, secret.id.ToString()))
                        .FilterByWildcards(secret_id => secret_id.Item2, wpSecretId)
                        .OrderBy(secret_id => secret_id.Item2))
                    {
                        string target = System.IO.Path.Combine(m.GetPSPath(), secret.Item2!);

                        if (ShouldProcess(target, "Remove ClientSecret"))
                        {
                            try
                            {
                                drive.OrchAPISession.DeleteMachineClientSecret(secret.Item2!);
                                drive._dicMachineClientSecrets = null;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(m.GetPSPath(), ex), "AddMachineSecretKeyError", ErrorCategory.InvalidOperation, m));
                            }
                        }
                    }
                }
            }
        }
    }
}
