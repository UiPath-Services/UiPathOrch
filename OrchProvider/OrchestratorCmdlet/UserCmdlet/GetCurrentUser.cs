using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchCurrentUser")]
[OutputType(typeof(Entities.User))]
public class GetCurrentUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.GetCurrentUser());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var currentUser = result.GetResult(cancelHandler.Token);
                if (currentUser is null) continue;

                WriteObject(currentUser);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetCurrentUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
