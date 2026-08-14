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
public class ImportOrchConfigCmdlet : PSCmdlet
{
    /// <summary>
    /// Configuration file to load instead of the one currently in effect. A folder is accepted
    /// as well as a file. On success this becomes the session's configuration file by setting
    /// UIPATHORCH_CONFIG_PATH in the CURRENT PROCESS -- nothing is written to the user's or the
    /// machine's persistent environment. For a standing setup, set the variable in $PROFILE (or
    /// as a user / system variable) instead, so that module autoloading picks it up before any
    /// drive is mounted.
    /// </summary>
    // Without this, an empty argument -- an unset variable, a blank cell in a CSV -- would fall
    // through to "no switch" and quietly re-import the config already in effect, reporting
    // success while the caller believes it switched.
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? ConfigPath { get; set; }

    // Resolve -ConfigPath to file-system candidates. Returns false having written the error.
    private bool TryResolveConfigPathArgument(out Core.OrchProvider.ConfigPathResolution resolved)
    {
        resolved = default;
        string expanded = System.Environment.ExpandEnvironmentVariables(ConfigPath!.Trim());

        string path;
        ProviderInfo provider;
        try
        {
            path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(expanded, out provider, out _);
        }
        catch (System.Exception ex)
        {
            WriteError(new ErrorRecord(ex, "ConfigPathInvalid", ErrorCategory.InvalidArgument, ConfigPath));
            return false;
        }

        // The current location is usually an Orch drive, so a relative path would otherwise be
        // resolved against the Orchestrator provider and yield a nonsense file path.
        if (!provider.Name.Equals("FileSystem", System.StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new System.ArgumentException(
                    $"-ConfigPath resolved to the '{provider.Name}' provider. Specify a file-system path; a relative path is resolved against the current location, which is on a '{provider.Name}' drive."),
                "ConfigPathNotFileSystem", ErrorCategory.InvalidArgument, ConfigPath));
            return false;
        }

        // Accept a folder as well as a file, exactly the way the environment variable does: the
        // file reading is tried first and the folder reading only if nothing is there. No
        // Directory.Exists probe to shortcut it -- that call blocks for the full SMB/DFS connect
        // timeout against an unreachable share, which is precisely the hang the bounded read
        // downstream exists to prevent, and it would run before that bound applied.
        resolved = System.IO.Path.EndsInDirectorySeparator(expanded)
            ? new Core.OrchProvider.ConfigPathResolution
            {
                Path = System.IO.Path.Combine(path, Core.OrchProvider.ConfigFileName),
                IsOverride = true,
            }
            : new Core.OrchProvider.ConfigPathResolution
            {
                Path = path,
                FolderCandidate = System.IO.Path.Combine(path, Core.OrchProvider.ConfigFileName),
                IsOverride = true,
            };

        return true;
    }

    protected override void ProcessRecord()
    {
        // Always re-read the config and re-mount. A prior optimization skipped this
        // when the file was unchanged since the last mount (to avoid a redundant
        // second read right after Import-Module's InitializeDefaultDrives), but the
        // silent no-op confused users — running Import-OrchConfig and seeing nothing
        // happen — and the saved work is negligible. Re-mounting recreates the drives,
        // which clears their cached sign-ins; the next use of each drive
        // re-authenticates. That is intentional: it is how a user picks up a fresh
        // sign-in (e.g. after signing in to the org's directory in the browser).
        bool switching = !string.IsNullOrWhiteSpace(ConfigPath);
        Core.OrchProvider.ConfigPathResolution resolution;

        if (switching)
        {
            if (!TryResolveConfigPathArgument(out resolution)) return;
        }
        else
        {
            resolution = Core.OrchProvider.ResolveConfigPath();
            if (resolution.Warning is not null) WriteWarning(resolution.Warning);
        }

        // Everything up to ShouldProcess is pre-flight: read and parse before touching any
        // session state, so that a bad -ConfigPath leaves the session exactly as it was --
        // drives still mounted, environment variable untouched.
        if (!Core.OrchProvider.TryReadConfigFile(
                resolution, out string? json, out string configFilePath, out string? readError, bypassMemo: true))
        {
            // An explicitly named file that cannot be read is an ERROR, not a warning. A warning
            // leaves $? true, does not stop under -ErrorAction Stop, and is not catchable -- so a
            // startup script that switches to a shared config would sail past an offline share
            // and keep running against the drives from the PREVIOUS config, silently targeting
            // the wrong tenant. That is the same failure the no-fallback rule exists to prevent.
            if (switching)
            {
                WriteError(new ErrorRecord(
                    new System.IO.IOException($"{readError} (-ConfigPath: {configFilePath})"),
                    "ConfigFileNotAvailable", ErrorCategory.OpenError, ConfigPath));
                return;
            }

            WriteWarning($"\"{configFilePath}\": {readError}");
            WriteWarning("Run Edit-OrchConfig to create and edit the configuration file.");
            return;
        }

        UiPathOrchConfig config;
        try
        {
            config = JsonSerializer.Deserialize<UiPathOrchConfig>(json!, JsonTools.jsonAllowComments)!;
            if (config is null) throw new System.Exception("Deserialization resulted in a null object.");
        }
        catch (System.Exception ex)
        {
            WriteError(new ErrorRecord(ex, "ConfigDeserializationError",
                ErrorCategory.InvalidData, configFilePath));
            return;
        }

        // A hand-edited config without a "PSDrives" array deserializes fine but has no
        // drives to mount — fail clearly instead of NRE-ing in the mount loop below.
        if (config.PSDrives is null)
        {
            WriteError(new ErrorRecord(
                new System.Exception("The configuration file has no \"PSDrives\" array. Run Edit-OrchConfig to define at least one drive."),
                "ConfigMissingPSDrives", ErrorCategory.InvalidData, configFilePath));
            return;
        }

        if (!ShouldProcess(configFilePath, "Import OrchConfig"))
        {
            return;
        }

        // Only now does the file become the session's configuration file. Switching is a side
        // effect, so it must not happen under -WhatIf, nor when the file turned out unusable.
        if (switching)
        {
            System.Environment.SetEnvironmentVariable(Core.OrchProvider.ConfigPathEnvVar, configFilePath);
            WriteVerbose($"{Core.OrchProvider.ConfigPathEnvVar} set to '{configFilePath}' for this process. Child processes inherit it; nothing was written to the persistent environment.");
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
                catch (Exception ex)
                {
                    WriteWarning($"Failed to create DU drive '{psDrive.Name}Du': {ex.Message}");
                }
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
                catch (Exception ex)
                {
                    WriteWarning($"Failed to create TM drive '{psDrive.Name}Tm': {ex.Message}");
                }
            }
        }

        WriteVerbose($"{driveCount} drive(s) mounted from '{configFilePath}'.");
    }
}
