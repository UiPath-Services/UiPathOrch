using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchProductVersion")]
[OutputType(typeof(Entities.OrchProductVersion))]
public class GetOrchProductVersionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.ProductVersion.Get()
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var info = result.GetResult(cancelHandler.Token);
                if (info is null) continue;

                // ProductVersion caches one shared singleton per org; stamp the
                // drive-local Path on a per-emit ShallowClone copy.
                var copy = info.ShallowClone();
                copy.Path = result.Source.NameColonSeparator;
                WriteObject(copy);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetOrchProductVersionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
