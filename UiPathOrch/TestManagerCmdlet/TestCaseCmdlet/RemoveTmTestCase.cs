using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmTestCaseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var driveProject in drivesProjects.WithCancellation(cancelHandler.Token))
        {
            var (drive, project) = driveProject;

            try
            {
                var entities = drive.TmTestCases.Get(project);

                foreach (var testCase in entities
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(testCase.GetPSPath(), "Remove TmTestCase"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmTestCase(project.id!, testCase.id!);
                            drive.TmTestCases.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "RemoveTmTestCaseError", ErrorCategory.InvalidOperation, testCase));
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
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseError", ErrorCategory.InvalidOperation, project));
            }
        }
    }
}
