using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLogLocation")]
[OutputType(typeof(string))]
public class GetOrchLogLocationCommand : OrchestratorPSCmdlet
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

        WriteObject(logFolderPath);
    }
}
