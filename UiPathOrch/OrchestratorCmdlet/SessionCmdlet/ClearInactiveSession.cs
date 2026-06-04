using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Bulk-deletes Disconnected / Unresponsive unattended sessions across the tenant via
// POST /odata/Sessions/.../DeleteInactiveUnattendedSessions. Useful as a maintenance
// sweep before re-deploying robots, or to free runtime slots stuck on dead sessions.
//
// Tenant-level: -Path selects the drive(s) but does not scope to folders (the API itself
// has no X-UIPATH-OrganizationUnitId header in v20 swagger). Inactive = Status equals
// "Disconnected" OR IsUnresponsive == true. Each ShouldProcess confirmation is per-drive
// (one API call per drive for the whole batch); deleted MachineSessionRuntime entities
// are written to the pipeline so the caller can log or pipe further.
[Cmdlet(VerbsCommon.Clear, "OrchInactiveSession", SupportsShouldProcess = true)]
[OutputType(typeof(MachineSessionRuntime))]
public class ClearInactiveSessionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var sessions = drive.MachineSessionRuntimes.Fetch();
                var inactive = sessions
                    .Where(s => s.SessionId is not null
                        && (string.Equals(s.Status, "Disconnected", StringComparison.OrdinalIgnoreCase)
                            || s.IsUnresponsive == true))
                    .ToList();

                if (inactive.Count == 0) continue;

                string target = $"{drive.NameColonSeparator} ({inactive.Count} inactive session(s))";
                if (!ShouldProcess(target, "Clear InactiveSession")) continue;

                cancelHandler.Token.ThrowIfCancellationRequested();
                try
                {
                    drive.OrchAPISession.DeleteInactiveSessions(inactive.Select(s => s.SessionId!.Value));
                    drive.MachineSessionRuntimes.ClearCache();
                    foreach (var s in inactive)
                    {
                        WriteObject(s);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex),
                        "ClearInactiveSessionError", ErrorCategory.InvalidOperation, drive));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex),
                    "GetSessionError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
