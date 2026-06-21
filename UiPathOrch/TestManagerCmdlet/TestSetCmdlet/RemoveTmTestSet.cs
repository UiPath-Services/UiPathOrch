using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "TmTestSet", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter))]
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
                var entity = drive.TmTestSets.Get(project);

                foreach (var testSet in entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance)
                    .WithProgressBar(this, $"Removing test sets in {project.GetPSPath()}", e => e.name)
                    .WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(testSet.GetPSPath(), "Remove TmTestSet"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveTmTestSet(project.id!, testSet.id!);
                            drive.TmTestSets.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "RemoveTmTestSetError", ErrorCategory.InvalidOperation, testSet));
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
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestSetError", ErrorCategory.InvalidOperation, project));
            }
        }
    }
}
