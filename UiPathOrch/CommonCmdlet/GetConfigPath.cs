using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchConfigPath")]
[OutputType(typeof(string))]
public class GetConfigPathCommand : PSCmdlet
{
    protected override void ProcessRecord()
    {
        Core.OrchProvider.EnsureDefaultConfigFileExists();
        WriteObject(Core.OrchProvider.GetConfigFilePath());
    }
}
