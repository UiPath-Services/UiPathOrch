using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Imports the UiPathOrch configuration file and creates PSDrives.
/// All existing OrchDriveInfo instances and their caches are destroyed and re-created.
/// </summary>
[Cmdlet(VerbsData.Import, "OrchConfig", SupportsShouldProcess = true)]
public class ImportOrchConfigCommand : PSCmdlet
{
    [Parameter]
    public SwitchParameter Force { get; set; }

    protected override void ProcessRecord()
    {
        // If -Force is not specified, skip if the config file has not changed since the last mount
        string configFilePath = Core.OrchProvider.GetConfigFilePath();
        if (!Force
            && Core.OrchProvider.ConfigLastWriteTimeUtc is not null
            && System.IO.File.Exists(configFilePath)
            && System.IO.File.GetLastWriteTimeUtc(configFilePath) == Core.OrchProvider.ConfigLastWriteTimeUtc)
        {
            WriteVerbose("Configuration file has not changed since the last mount. Skipping. Use -Force to override.");
            return;
        }

        if (!System.IO.File.Exists(configFilePath))
        {
            WriteWarning($"Configuration file not found: {configFilePath}");
            WriteWarning("Run Edit-OrchConfig to create and edit the configuration file.");
            return;
        }

        // Read and deserialize the configuration file
        string json = System.IO.File.ReadAllText(configFilePath);
        UiPathOrchConfig config;
        try
        {
            config = JsonSerializer.Deserialize<UiPathOrchConfig>(json, JsonTools.jsonAllowComments)!;
            if (config is null) throw new System.Exception("Deserialization resulted in a null object.");
        }
        catch (System.Exception ex)
        {
            WriteError(new ErrorRecord(ex, "ConfigDeserializationError",
                ErrorCategory.InvalidData, configFilePath));
            return;
        }

        if (!ShouldProcess(configFilePath, "Import-OrchConfig"))
        {
            return;
        }

        // If the current location is on an Orch drive, switch to C: since we cannot remove a drive while it is current
        var currentDrive = SessionState.Drive.Current;
        if (currentDrive is OrchDriveInfo or OrchDuDriveInfo or OrchTmDriveInfo)
        {
            SessionState.Path.SetLocation(@"C:");
        }

        // Remove all existing drives (caches are also cleared)
        // Remove Du/Tm first, then Orch, because Du/Tm depend on Orch
        foreach (var drive in SessionState.EnumAllDuDrives().ToList())
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        foreach (var drive in SessionState.EnumAllTmDrives().ToList())
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        foreach (var drive in SessionState.EnumAllOrchDrives().ToList())
        {
            SessionState.Drive.Remove(drive.Name, true, null);
        }

        // Apply global settings
        if (config.Proxy is not null)
        {
            config.Proxy.Enabled ??= true;
        }
        config.Enabled ??= true;

        // Update _config
        Core.OrchProvider.SetConfig(config);

        // Create Orch drives
        ProviderInfo orchProvider;
        try
        {
            orchProvider = SessionState.Provider.GetOne("UiPathOrch");
        }
        catch
        {
            WriteError(new ErrorRecord(
                new System.InvalidOperationException("UiPathOrch provider is not loaded."),
                "ProviderNotFound", ErrorCategory.ObjectNotFound, "UiPathOrch"));
            return;
        }

        int driveCount = 0;

        foreach (var psDrive in config.PSDrives!)
        {
            psDrive.CascadePSDriveFromGlobalSettings(config);
            if (!psDrive.Enabled.GetValueOrDefault()) continue;

            try
            {
                var orchDrive = new OrchDriveInfo(orchProvider, psDrive);
                SessionState.Drive.New(orchDrive, scope: "Global");
                driveCount++;
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(psDrive.Name, ex),
                    "NewPSDriveError", ErrorCategory.InvalidData, psDrive.Name));
            }
        }

        // Create Du drives
        foreach (var psDrive in config.PSDrives!)
        {
            if (psDrive.Enabled is not null && !psDrive.Enabled.GetValueOrDefault()) continue;

            if (psDrive.Scope?.Contains("Du.") ?? false)
            {
                try
                {
                    string root = psDrive.Root?.TrimEnd('/') + "/du_";
                    var duProvider = SessionState.Provider.GetOne("UiPathOrchDu");
                    var duDrive = new OrchDuDriveInfo(duProvider, psDrive.Name + "Du", psDrive.Description ?? "", root);
                    SessionState.Drive.New(duDrive, scope: "Global");
                }
                catch { }
            }
        }

        // Create Tm drives
        foreach (var psDrive in config.PSDrives!)
        {
            if (psDrive.Enabled is not null && !psDrive.Enabled.GetValueOrDefault()) continue;

            if (psDrive.Scope?.Contains("TM.") ?? false)
            {
                try
                {
                    string root = psDrive.Root?.TrimEnd('/') + "/testmanager_";
                    var tmProvider = SessionState.Provider.GetOne("UiPathOrchTm");
                    var tmDrive = new OrchTmDriveInfo(tmProvider, psDrive.Name + "Tm", psDrive.Description ?? "", root);
                    SessionState.Drive.New(tmDrive, scope: "Global");
                }
                catch { }
            }
        }

        Core.OrchProvider.ConfigLastWriteTimeUtc = System.IO.File.GetLastWriteTimeUtc(configFilePath);
        WriteVerbose($"{driveCount} drive(s) mounted from '{configFilePath}'.");
    }
}
