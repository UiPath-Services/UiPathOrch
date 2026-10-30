using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "TmRequirement", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class UpdateTmRequirementCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // Replaces the requirement's labels with this list (removeOtherLabels), as -Tags does on
    // Update-OrchQueue. An empty list leaves the labels alone.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmLabelNameCompleter))]
    public string[]? Labels { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();
        var labels = Labels?.Where(l => !string.IsNullOrEmpty(l)).ToArray();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project) in drivesProjects.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<TmRequirement>? requirements = null;
            try
            {
                requirements = drive.TmRequirements.Get(project);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmRequirementError", ErrorCategory.InvalidOperation, project));
            }
            if (requirements is null) continue;

            foreach (var requirement in requirements
                .SelectByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.objKey!, ObjKeyComparer.Instance)
                .ToList()
                .WithProgressBar(this, $"Updating requirements in {project.GetPSPath()}", e => e.name)
                .WithCancellation(cancelHandler.Token))
            {
                var operations = ComputeRequirementPatch(requirement, NewName, Description);
                bool hasLabels = labels is { Length: > 0 };
                if (operations.Count == 0 && !hasLabels)
                {
                    continue;
                }

                var target = requirement.GetPSPath();
                if (ShouldProcess(target, "Update TmRequirement"))
                {
                    try
                    {
                        if (operations.Count != 0)
                        {
                            drive.OrchAPISession.PatchTmRequirement(project.id!, requirement.id!, operations);
                        }
                        if (hasLabels)
                        {
                            drive.OrchAPISession.AddTmObjectLabels(project.id!, new TmBulkAddLabelsRequest
                            {
                                objectType = "requirement",
                                objectIds = [requirement.id!],
                                labels = labels,
                                projectId = project.id,
                                removeOtherLabels = true,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTmRequirementError", ErrorCategory.InvalidOperation, requirement));
                    }
                    drive.TmRequirements.ClearCache(project.id);
                    if (hasLabels) drive.TmLabels.ClearCache(project.id);
                }
            }
        }
    }

    /// <summary>
    /// The JSON Patch operations that bring <paramref name="current"/> to the requested name and
    /// description: one "replace" per field that was given AND differs from the current value, so
    /// an unchanged request sends nothing. No API access -- unit-testable.
    /// </summary>
    internal static List<TmPatchOperation> ComputeRequirementPatch(TmRequirement current, string? newName, string? description)
    {
        var operations = new List<TmPatchOperation>();
        if (newName is not null && newName != current.name)
        {
            operations.Add(new TmPatchOperation { op = "replace", path = "/name", value = newName });
        }
        if (description is not null && description != (current.description ?? ""))
        {
            operations.Add(new TmPatchOperation { op = "replace", path = "/description", value = description });
        }
        return operations;
    }
}
