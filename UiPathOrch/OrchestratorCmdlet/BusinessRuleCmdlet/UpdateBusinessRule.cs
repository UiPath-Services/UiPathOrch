using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: see GetBusinessRule.cs for rationale (OR.BusinessRules scope not available
// to External Applications or Personal Access Tokens).
[Cmdlet(VerbsData.Update, "OrchBusinessRule", SupportsShouldProcess = true)]
[OutputType(typeof(BusinessRule))]
class UpdateBusinessRuleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Source { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        byte[]? fileBytes = null;
        string? fileName = null;
        if (!string.IsNullOrEmpty(Source))
        {
            string sourcePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Source);
            if (!File.Exists(sourcePath))
            {
                WriteError(new ErrorRecord(new FileNotFoundException("Rule definition file not found.", sourcePath),
                    "UpdateBusinessRuleSourceNotFound", ErrorCategory.ObjectNotFound, sourcePath));
                return;
            }
            fileBytes = File.ReadAllBytes(sourcePath);
            fileName = System.IO.Path.GetFileName(sourcePath);
        }

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var rules = drive.BusinessRules.Get(folder)
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name)
                    .ToList();

                foreach (var existing in rules.WithCancellation(cancelHandler.Token))
                {
                    if (string.IsNullOrEmpty(existing.Id)) continue;

                    string target = existing.GetPSPath();
                    if (!ShouldProcess(target, "Update BusinessRule")) continue;

                    try
                    {
                        var posting = new BusinessRule { Name = existing.Name };
                        posting.AssignStringIfNotNullOrEmpty(Description, (b, v) => b.Description = v);
                        posting.AssignTags(Tags, (b, v) => b.Tags = v);

                        drive.OrchAPISession.UpdateBusinessRule(folder.Id ?? 0, existing.Id, posting, fileName, fileBytes);
                        drive.BusinessRules.ClearCache(folder);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateBusinessRuleError", ErrorCategory.InvalidOperation, existing));
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
