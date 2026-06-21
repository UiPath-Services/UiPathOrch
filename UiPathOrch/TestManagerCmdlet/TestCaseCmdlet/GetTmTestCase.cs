using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmTestCase")]
[OutputType(typeof(Entities.TmTestCase))]
public class GetTmTestCaseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
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

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.TmTestCases.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting test cases");
        foreach (var result in results)
        {
            try
            {
                var entity = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
