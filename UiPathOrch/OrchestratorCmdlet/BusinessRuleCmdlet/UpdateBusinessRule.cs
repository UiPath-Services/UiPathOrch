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
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

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

        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
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

                foreach (var existing in rules
                    .WithProgressBar(this, $"Updating business rules in {folder.GetPSPath()}", r => r.Name)
                    .WithCancellation(cancelHandler.Token))
                {
                    if (string.IsNullOrEmpty(existing.Id)) continue;

                    string target = existing.GetPSPath();

                    try
                    {
                        // Only call the API when something actually changes. A supplied rule-definition
                        // file (-Source) is a content update we can't diff, so it always writes; the
                        // Description / Tags metadata is compared against the current rule. The
                        // change-detection lives in the pure, API-free ComputeBusinessRuleUpdate core.
                        var posting = new BusinessRule { Name = existing.Name };
                        bool dirty = ComputeBusinessRuleUpdate(posting, existing, new BusinessRuleUpdateInputs
                        {
                            Description = Description,
                            Tags = Tags,
                            HasSourceFile = fileBytes is not null,
                        });

                        if (!dirty) continue;
                        if (!ShouldProcess(target, "Update BusinessRule")) continue;

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

    /// <summary>Pure inputs for <see cref="ComputeBusinessRuleUpdate"/> — no API access.</summary>
    internal sealed class BusinessRuleUpdateInputs
    {
        public string? Description { get; init; }
        public string[]? Tags { get; init; }
        /// <summary>True when a -Source rule-definition file was supplied (an uncomparable content update).</summary>
        public bool HasSourceFile { get; init; }
    }

    /// <summary>
    /// Applies the requested metadata changes onto <paramref name="payload"/> and returns whether
    /// anything actually changed, so the caller can skip the API when the request is a no-op. A
    /// supplied rule-definition file always writes (its content can't be diffed); Description and
    /// Tags are compared against <paramref name="existing"/>. A field left null on the payload means
    /// "no change" for the partial-update API. No API access — fully unit-testable.
    /// </summary>
    internal static bool ComputeBusinessRuleUpdate(BusinessRule payload, BusinessRule existing, BusinessRuleUpdateInputs input)
    {
        bool dirty = input.HasSourceFile;

        if (!string.IsNullOrEmpty(input.Description) && !string.Equals(existing.Description, input.Description, StringComparison.Ordinal))
        {
            payload.Description = input.Description;
            dirty = true;
        }

        dirty |= payload.AssignTags(input.Tags, existing, b => b.Tags, (b, v) => b.Tags = v);

        return dirty;
    }
}
