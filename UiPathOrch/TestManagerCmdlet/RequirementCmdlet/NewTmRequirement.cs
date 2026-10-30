using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "TmRequirement", SupportsShouldProcess = true)]
[OutputType(typeof(TmRequirement))]
public class NewTmRequirementCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewTmRequirementNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // Label names. The requirement DTO carries no labels; they are added by a second call
    // (objectlabels/requirement/addlabels) once the requirement exists.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmLabelNameCompleter))]
    public string[]? Labels { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class NewTmRequirementNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesProjects = SessionState.EnumTmFolders(paramPath);
            var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.TmRequirements.Get(dp.project));

            // Exclude Names already selected by the parameter from the candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewRequirement", names, entities, e => e.name!));
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath));
        var labels = Labels?.Where(l => !string.IsNullOrEmpty(l)).ToArray();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project) in drivesProjects)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(project.GetPSPath(), name);
                if (!ShouldProcess(target, "New TmRequirement"))
                {
                    continue;
                }

                TmRequirement? created;
                try
                {
                    created = drive.OrchAPISession.PostTmRequirement(project.id!, new TmCreateRequirementRequest
                    {
                        name = WildcardPattern.Unescape(name),
                        description = Description,
                        projectId = project.id,
                    });
                    drive.TmRequirements.ClearCache(project.id);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "NewTmRequirementError", ErrorCategory.InvalidOperation, project));
                    continue;
                }
                if (created is null) continue;
                created.Path = project.GetPSPath();

                // The requirement exists at this point; a label failure is reported but the
                // requirement is still emitted, so the caller can see (and fix) what landed.
                if (labels is { Length: > 0 })
                {
                    try
                    {
                        drive.OrchAPISession.AddTmObjectLabels(project.id!, new TmBulkAddLabelsRequest
                        {
                            objectType = "requirement",
                            objectIds = [created.id!],
                            labels = labels,
                            projectId = project.id,
                        });
                        drive.TmLabels.ClearCache(project.id);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(created.GetPSPath(), "The requirement was created, but its labels could not be added.", ex), "AddTmRequirementLabelError", ErrorCategory.InvalidOperation, created));
                    }
                }

                WriteObject(created);
            }
        }
    }
}
