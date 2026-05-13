using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Open, "OrchLogLocation")]
public class OpenOrchLogLocationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        string logFolderPath;
        try
        {
            var drive = SessionState.GetOrchDrive(Path);
            logFolderPath = drive.OrchAPISession.GetLogFolderPath();
        }
        catch
        {
            logFolderPath = UiPath.PowerShell.Core.OrchProvider.GetLogFolderBasePath();
        }

        if (string.IsNullOrEmpty(logFolderPath))
        {
            WriteError(new ErrorRecord(
                new Exception("Log folder path is null or empty."),
                "InvalidLogFolderPath",
                ErrorCategory.InvalidArgument,
                null));
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: open the log folder with File Explorer.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = logFolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "OpenFolderFailed", ErrorCategory.OpenError, logFolderPath));
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: change current location to log folder location.
            try
            {
                SessionState.Path.PushCurrentLocation("default");
                SessionState.Path.SetLocation(logFolderPath);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ChangeLocationFailed", ErrorCategory.InvalidOperation, logFolderPath));
            }
        }
        else
        {
            WriteWarning("This platform is not supported for opening log location.");
        }
    }
}
