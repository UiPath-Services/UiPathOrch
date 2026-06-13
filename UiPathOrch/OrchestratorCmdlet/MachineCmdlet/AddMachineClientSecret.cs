using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchMachineClientSecret", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.MachineSecretKey))]
public class AddMachineClientSecretCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // This completer cannot be shared because it excludes machines with Scope "PersonalWorkspace" and "AutomationCloudRobot".
    // It would be better to parameterize the excluded Scopes and share the logic, but that's a bit cumbersome...
    // Actually, we could introduce a class that parameterizes the enumeration and filtering parts
    // to generalize this more broadly.
    private class MachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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

                if (!bFound)
                {
                    yield return new CompletionResult($@"""(No machines found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var machines = drive.Machines.Get();
            var targetMachines = machines
                .Where(m => m.Scope != "PersonalWorkspace" && m.Scope != "AutomationCloudRobot")
                .FilterByNames(m => m?.Name, Name)
                .OrderBy(m => m.Name);

            foreach (var m in targetMachines.WithCancellation(cancelHandler.Token))
            {
                if (ShouldProcess(m.GetPSPath(), "Add ClientSecret"))
                {
                    try
                    {
                        var key = drive.OrchAPISession.AddMachineClientSecret(m.LicenseKey!);
                        drive.MachineClientSecrets.ClearCache();
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
