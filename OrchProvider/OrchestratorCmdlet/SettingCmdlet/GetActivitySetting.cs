using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchActivitySetting")]
[OutputType(typeof(ActivitySettings))]
public class GetActivitySettingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.ActivitySettings.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetActivitySettingError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
