using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: see GetBusinessRule.cs for rationale (OR.BusinessRules scope not available
// to External Applications or Personal Access Tokens).
[Cmdlet(VerbsCommon.New, "OrchBusinessRule", SupportsShouldProcess = true)]
[OutputType(typeof(BusinessRule))]
class NewBusinessRuleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Source { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        // Resolve absolute file path against the current PSDrive cwd, not against the .NET cwd.
        string sourcePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Source!);
        if (!File.Exists(sourcePath))
        {
            WriteError(new ErrorRecord(new FileNotFoundException("Rule definition file not found.", sourcePath),
                "NewBusinessRuleSourceNotFound", ErrorCategory.ObjectNotFound, sourcePath));
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(sourcePath);
        string fileName = System.IO.Path.GetFileName(sourcePath);

        var drivesFolders = SessionState.EnumFolders(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            string target = System.IO.Path.Combine(folder.GetPSPath(), Name!);
            if (!ShouldProcess(target, "New BusinessRule")) continue;

            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var posting = new BusinessRule { Name = Name };
                posting.AssignStringIfNotNullOrEmpty(Description, (b, v) => b.Description = v);
                posting.AssignTags(Tags, (b, v) => b.Tags = v);

                var created = drive.OrchAPISession.CreateBusinessRule(folder.Id ?? 0, posting, fileName, fileBytes);
                drive.BusinessRules.ClearCache(folder);
                if (created is not null)
                {
                    created.Path = folder.GetPSPath();
                    WriteObject(created);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "NewBusinessRuleError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
