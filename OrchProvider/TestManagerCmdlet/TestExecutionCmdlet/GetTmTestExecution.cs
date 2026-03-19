using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmTestExecution")]
[OutputType(typeof(Entities.TmTestExecution))]
public class GetTmTestExecutionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestExecutionNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        //foreach (var (drive, project) in drivesProjects)
        //{
        //    try
        //    {
        //        var e = drive.TmTestExecutions.Get(project);
        //        WriteObject(e, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new OrchException(project.GetPSPath(), ex);
        //    }
        //}

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.TmTestExecutions.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name!),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestExecutionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
