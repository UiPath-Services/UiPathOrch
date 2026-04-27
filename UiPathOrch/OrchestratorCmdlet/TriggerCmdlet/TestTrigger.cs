using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Wraps POST /odata/ProcessSchedules/.../ValidateProcessSchedule. Sends an existing trigger
// (fetched via Get-OrchTrigger or named via -Name) to the server's pre-flight validator and
// returns a ValidationResult with IsValid + Errors + ErrorCodes (e.g. RobotNotFound,
// TemplateNoLicense, RobotConcurrencyLimit, ...). Useful for "would this schedule actually run?"
// preview without enabling/firing it.
[Cmdlet(VerbsDiagnostic.Test, "OrchTrigger")]
[OutputType(typeof(ValidationResult))]
public class TestTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var triggers = drive.GetTriggers(folder)
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name)
                    .ToList();

                foreach (var trigger in triggers)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var result = drive.OrchAPISession.ValidateProcessSchedule(folder.Id ?? 0, trigger);
                        if (result is not null)
                        {
                            // Decorate with the trigger's PSPath so downstream pipelines can correlate
                            // results back to the source schedule.
                            var pso = new PSObject(result);
                            pso.Properties.Add(new PSNoteProperty("TriggerName", trigger.Name));
                            pso.Properties.Add(new PSNoteProperty("TriggerPSPath", trigger.GetPSPath()));
                            WriteObject(pso);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "TestTriggerError", ErrorCategory.InvalidOperation, trigger));
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
