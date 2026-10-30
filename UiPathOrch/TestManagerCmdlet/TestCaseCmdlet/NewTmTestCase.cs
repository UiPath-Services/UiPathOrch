using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "TmTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(TmTestCase))]
public class NewTmTestCaseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewTmTestCaseNameCompleter))]
    public string[]? Name { get; set; }

    // Free text, optional: the server accepts a test case without one (version stays null).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // Label names. The test case DTO carries no labels; they are added by a second call
    // (objectlabels/testCase/addlabels) once the test case exists.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmLabelNameCompleter))]
    public string[]? Labels { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class NewTmTestCaseNameCompleter : OrchArgumentCompleter
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
            var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.TmTestCases.Get(dp.project));

            // Exclude Names already selected by the parameter from the candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewTestCase", names, entities, e => e.name!));
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
                if (!ShouldProcess(target, "New TmTestCase"))
                {
                    continue;
                }

                TmTestCase? created;
                try
                {
                    created = drive.OrchAPISession.PostTmTestCase(project.id!, new TmCreateTestCaseRequest
                    {
                        name = WildcardPattern.Unescape(name),
                        version = Version,
                        description = Description,
                        projectId = project.id,
                    });
                    drive.TmTestCases.ClearCache(project.id);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "NewTmTestCaseError", ErrorCategory.InvalidOperation, project));
                    continue;
                }
                if (created is null) continue;
                created.Path = project.GetPSPath();

                // The test case exists at this point; a label failure is reported but the
                // test case is still emitted, so the caller can see (and fix) what landed.
                if (labels is { Length: > 0 })
                {
                    try
                    {
                        drive.OrchAPISession.AddTmObjectLabels(project.id!, "testCase", created.id!, labels);
                        drive.TmLabels.ClearCache(project.id);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(created.GetPSPath(), "The test case was created, but its labels could not be added.", ex), "AddTmTestCaseLabelError", ErrorCategory.InvalidOperation, created));
                    }
                }

                WriteObject(created);
            }
        }
    }
}
