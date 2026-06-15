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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        string logFolderPath;
        bool driveResolved;
        try
        {
            var drive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath));
            logFolderPath = drive.OrchAPISession.GetLogFolderPath();
            driveResolved = true;
        }
        catch
        {
            logFolderPath = UiPath.PowerShell.Core.OrchProvider.GetLogFolderBasePath();
            driveResolved = false;
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
            // Windows: when invoked from an Orch drive, reveal that drive's log
            // subfolder selected inside its parent (/select); otherwise open the
            // base log folder directly.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = driveResolved ? $"/select,\"{logFolderPath}\"" : logFolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "OpenFolderFailed", ErrorCategory.OpenError, logFolderPath));
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: when invoked from an Orch drive, reveal that drive's log
            // subfolder selected in Finder (open -R); otherwise open the base log
            // folder. ArgumentList keeps a path with spaces intact.
            try
            {
                var psi = new ProcessStartInfo { FileName = "open", UseShellExecute = false };
                if (driveResolved) psi.ArgumentList.Add("-R");
                psi.ArgumentList.Add(logFolderPath);
                Process.Start(psi);
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
