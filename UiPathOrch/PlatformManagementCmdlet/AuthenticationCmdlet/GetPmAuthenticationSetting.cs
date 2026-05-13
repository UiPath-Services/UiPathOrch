using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAuthenticationSetting")]
[OutputType(typeof(Entities.PmAuthenticationRoot))]
public class GetPmAuthenticationSettingCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            try
            {
                var entity = drive.PmAuthenticationSetting.Get();
                if (entity is not null) WriteObject(entity.WithPath(drive.NameColonSeparator));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetAuthenticationSettingError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
