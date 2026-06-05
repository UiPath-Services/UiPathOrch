using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLogLocation")]
[OutputType(typeof(string))]
public class GetOrchLogLocationCmdlet : OrchestratorPSCmdlet
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
        try
        {
            var drive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath));
            logFolderPath = drive.OrchAPISession.GetLogFolderPath();
        }
        catch
        {
            logFolderPath = UiPath.PowerShell.Core.OrchProvider.GetLogFolderBasePath();
        }

        WriteObject(logFolderPath);
    }
}
