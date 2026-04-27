using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchBusinessRule", SupportsShouldProcess = true)]
public class RemoveBusinessRuleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
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
                var rules = drive.BusinessRules.Get(folder);

                foreach (var rule in rules
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(rule.Id)) continue;

                    if (ShouldProcess(rule.GetPSPath(), "Remove BusinessRule"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveBusinessRule(folder.Id ?? 0, rule.Id);
                            drive.BusinessRules.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(rule.GetPSPath(), ex), "RemoveBusinessRuleError", ErrorCategory.InvalidOperation, rule));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetBusinessRuleError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
