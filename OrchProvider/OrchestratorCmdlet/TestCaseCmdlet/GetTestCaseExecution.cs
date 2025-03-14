using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseExecution")]
[OutputType(typeof(Entities.TestCaseExecution))]
public class GetTestCaseExecutionCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestCaseNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    //[Parameter(Position = 1)]
    //[ArgumentCompleter(typeof(PackageIdentifierCompleter))]
    //[SupportsWildcards]
    //public string[]? PackageIdentifier { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.TestCaseExecutions.Get(df.folder)
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities,
                    //.FilterByWildcards(tc => tc.Name!, wpName)
                    //.OrderBy(tc => tc.PackageIdentifier)
                    //.ThenBy(tc => tc.Name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestCaseExecutionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
