using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "TmTestSet", SupportsShouldProcess = true)]
[OutputType(typeof(TmTestSet))]
public class NewTmTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewTmTestSetNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // "Enable RPA activity coverage calculation" in the web form. Off when omitted.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? EnableCoverage { get; set; }

    // Label names. The test set DTO carries no labels; they are added by a second call
    // (objectlabels/testSet/addlabels) once the test set exists.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmLabelNameCompleter))]
    public string[]? Labels { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class NewTmTestSetNameCompleter : OrchArgumentCompleter
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
            var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.TmTestSets.Get(dp.project));

            // Exclude Names already selected by the parameter from the candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewTestSet", names, entities, e => e.name!));
        }
    }

    protected override void ProcessRecord()
    {
        bool? enableCoverage;
        try
        {
            enableCoverage = TmPatch.ParseBool(EnableCoverage, nameof(EnableCoverage));
        }
        catch (ArgumentException ex)
        {
            ThrowTerminatingError(new ErrorRecord(ex, "InvalidEnableCoverage", ErrorCategory.InvalidArgument, EnableCoverage));
            return;
        }

        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath));
        var labels = Labels?.Where(l => !string.IsNullOrEmpty(l)).ToArray();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project) in drivesProjects)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(project.GetPSPath(), name);
                if (!ShouldProcess(target, "New TmTestSet"))
                {
                    continue;
                }

                TmTestSet? created;
                try
                {
                    created = drive.OrchAPISession.PostTmTestSet(project.id!, new TmCreateTestSetRequest
                    {
                        name = WildcardPattern.Unescape(name),
                        description = Description,
                        enableCoverage = enableCoverage,
                        projectId = project.id,
                    });
                    drive.TmTestSets.ClearCache(project.id);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "NewTmTestSetError", ErrorCategory.InvalidOperation, project));
                    continue;
                }
                if (created is null) continue;
                created.Path = project.GetPSPath();

                // The test set exists at this point; a label failure is reported but the
                // test set is still emitted, so the caller can see (and fix) what landed.
                if (labels is { Length: > 0 })
                {
                    try
                    {
                        drive.OrchAPISession.AddTmObjectLabels(project.id!, "testSet", created.id!, labels);
                        drive.TmLabels.ClearCache(project.id);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(created.GetPSPath(), "The test set was created, but its labels could not be added.", ex), "AddTmTestSetLabelError", ErrorCategory.InvalidOperation, created));
                    }
                }

                WriteObject(created);
            }
        }
    }
}
