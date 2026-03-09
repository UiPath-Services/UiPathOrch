using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

// WIP
[Cmdlet(VerbsCommon.Get, "TmTestExecutionResult")]
[OutputType(typeof(Entities.TmTestExecutionResult))]
class GetTmTestExecutionResultCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestExecutionNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ParameterSetName = "Filter")]
    public SwitchParameter OrderAscending { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        // First retrieve TmTestExecutions to build a list of (drive, project, testExecution)
        var testExecutionsList = new List<(OrchTmDriveInfo drive, Entities.TmProject project, Entities.TmTestExecution testExecution)>();
        foreach (var (drive, project) in drivesProjects)
        {
            try
            {
                var testExecutions = drive.TmTestExecutions.Get(project);
                foreach (var te in testExecutions.FilterByWildcards(te => te?.name, wpName))
                {
                    testExecutionsList.Add((drive, project, te));
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestExecutionResultError", ErrorCategory.InvalidOperation, project));
            }
        }

        // Retrieve TmTestExecutionResult in parallel
        using var results = OrchThreadPool.RunForEach(testExecutionsList,
            item => item.project.GetPSPath(),
            item => item.testExecution,
            item => item.drive.TmTestExecutionResults.Fetch(item.project, item.testExecution.id!));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestExecutionResultError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Single-threaded version
        //foreach (var (drive, project) in drivesProjects)
        //{
        //    try
        //    {
        //        var testExecutions = drive.TmTestExecutions.Get(project);
        //        foreach (var testExecution in testExecutions.FilterByWildcards(te => te?.name, wpName))
        //        {
        //            var results = drive.TmTestExecutionResults.Fetch(project, testExecution.id!);
        //            WriteObject(results, true);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestExecutionResultError", ErrorCategory.InvalidOperation, project));
        //    }
        //}
    }
}
