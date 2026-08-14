using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchConfigPath")]
[OutputType(typeof(string))]
public class GetConfigPathCmdlet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        var resolution = Core.OrchProvider.ResolveConfigPath();
        if (resolution.Warning is not null) WriteWarning(resolution.Warning);

        // No-op when the location is overridden -- see OrchProvider.EnsureDefaultConfigFileExists.
        Core.OrchProvider.EnsureDefaultConfigFileExists();

        // The origin goes to the verbose stream rather than the output: this cmdlet's contract is
        // a bare path string, and callers pipe it straight into Get-Content / an editor.
        WriteVerbose(resolution.IsOverride
            ? $"Location comes from the {Core.OrchProvider.ConfigPathEnvVar} environment variable."
            : "Location is the built-in default (no override in effect).");

        // Reports the file that would actually be loaded, which for a location that could name
        // either a file or the folder holding it means reading to find out.
        WriteObject(Core.OrchProvider.GetConfigFilePath());
    }
}
