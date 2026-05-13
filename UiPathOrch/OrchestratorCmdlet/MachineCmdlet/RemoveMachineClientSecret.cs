using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchMachineClientSecret", SupportsShouldProcess = true)]
public class RemoveMachineClientSecretCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(SecretIdCompleter))]
    [SupportsWildcards]
    public string[]? SecretId { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    // This completer cannot be shared because it excludes machines with Scope "PersonalWorkspace" and "AutomationCloudRobot".
    // It would be better to parameterize the excluded Scopes and share the logic, but that's a bit cumbersome...
    // Actually, we could introduce a class that parameterizes the enumeration and filtering parts
    // to generalize this more broadly.
    private class MachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Names already selected via parameter from the candidates
            var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Machines.Get());

            bool bFound = false;
            foreach (var result in results)
            {
                foreach (var machine in result
                    .Where(m => wp.IsMatch(m.Name))
                    .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot") // This is the only difference from the shared logic
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
                yield return new CompletionResult($@"""(No machines found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
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
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude SecretIds already selected via parameter from the candidates
            var wpSecretId = CreateSelfExclusionList(commandAst, "SecretId", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Machines.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var machine in result
                    .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot") // This is the only difference from the shared logic
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name!))
                {
                    if (machine.LicenseKey is null) continue;

                    var secrets = drive.GetMachineClientSecret(machine.LicenseKey);
                    if (secrets is null) continue;

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
        var drives = SessionState.EnumOrchDrives(Path);

        var wpName = Name.Split1stValueByUnescapedCommas()?.ConvertToWildcardPatternList();
        var wpSecretId = SecretId.Split1stValueByUnescapedCommas()?.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<ExtendedMachine> machines = null;
            try
            {
                machines = drive.Machines.Get();
            }
            catch (OperationCanceledException)
            {
                throw;
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

            foreach (var m in targetMachines.WithCancellation(cancelHandler.Token))
            {
                if (m.LicenseKey is null) continue;

                MachineClientSecretResponse[] secrets = null;
                try
                {
                    secrets = drive.GetMachineClientSecret(m.LicenseKey);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachineClientSecretsError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
                if (secrets is null) continue;

                foreach (var secret in secrets
                    .Select(secret => (secret, secret.id.ToString()))
                    .FilterByWildcards(secret_id => secret_id.Item2, wpSecretId)
                    .OrderBy(secret_id => secret_id.Item2))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    string target = System.IO.Path.Combine(m.GetPSPath(), secret.Item2!);

                    if (ShouldProcess(target, "Remove ClientSecret"))
                    {
                        try
                        {
                            drive.OrchAPISession.DeleteMachineClientSecret(secret.Item2!);
                            drive.MachineClientSecrets.ClearCache();
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
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
