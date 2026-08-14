using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Core;

// DriveCmdletProvider: drive lifecycle + config/log path & default-config helpers.
public partial class OrchProvider
{
    private static void SaveResourceToFile(string resourceName, string outputPath)
    {
        string folderPath = System.IO.Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath!);
        }

        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using FileStream fileStream = new(outputPath, FileMode.CreateNew);
        stream!.CopyTo(fileStream);
    }

    public static string GetBasePath()
    {
        string moduleName = "UiPathOrch";
        if (OperatingSystem.IsWindows())
        {
            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(documents, "PowerShell", "Modules", moduleName);
        }
        else // Unix-based (Linux / macOS)
        {
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            return System.IO.Path.Combine(home, ".local", "share", "powershell", "Modules", moduleName);
        }
    }

    /// <summary>
    /// Environment variable that relocates the configuration file. PROCESS scope only -- the
    /// module never writes a persistent (User / Machine) variable. Set it from $PROFILE, from a
    /// user or system variable, or for the current process via `Import-OrchConfig -ConfigPath`.
    /// </summary>
    internal const string ConfigPathEnvVar = "UIPATHORCH_CONFIG_PATH";

    internal const string ConfigFileName = "UiPathOrchConfig.json";

    /// <summary>Where the configuration file lives when nothing overrides it.</summary>
    public static string GetDefaultConfigFilePath()
        => System.IO.Path.Combine(GetBasePath(), ConfigFileName);

    /// <summary>
    /// Outcome of resolving the configuration file location. <see cref="IsOverride"/> is what
    /// callers branch on: an overridden location is never auto-created from the template, never
    /// opened in an editor, and never falls back to the DEFAULT file when it cannot be read.
    /// <para>
    /// The value may name either the file or the folder holding it, and the two are told apart by
    /// trying rather than by guessing from the spelling: <see cref="Path"/> is attempted first and
    /// <see cref="FolderCandidate"/> -- the same value with the standard file name appended -- is
    /// attempted only if the first is NOT FOUND. Any other failure (a timeout on an unreachable
    /// share, access denied, malformed JSON) is reported as-is, because a second read would either
    /// cost another full timeout or answer the same question twice.
    /// </para>
    /// </summary>
    internal readonly struct ConfigPathResolution
    {
        /// <summary>The path to try first: the value itself, read as a file.</summary>
        internal string Path { get; init; }

        /// <summary>
        /// The path to try if <see cref="Path"/> does not exist, or null when there is nothing to
        /// fall back to -- the default location, and a value already written as a folder.
        /// </summary>
        internal string? FolderCandidate { get; init; }

        internal bool IsOverride { get; init; }
        internal string? Warning { get; init; }
    }

    /// <summary>
    /// Pure resolver for the configuration file location, split out from the environment so it
    /// can be unit tested. Deliberately does NOT touch the file system -- it runs during provider
    /// initialization, where a probe against an unreachable share would block with no way to
    /// report why. It produces the candidates; the read decides between them.
    /// </summary>
    internal static ConfigPathResolution ResolveConfigPath(string? rawValue, string defaultPath)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new ConfigPathResolution { Path = defaultPath, IsOverride = false };
        }

        // A value stored as REG_SZ -- which is what SetEnvironmentVariable writes -- is NOT
        // expanded by the OS at process start, unlike a REG_EXPAND_SZ value entered through the
        // System Properties UI. Expand here so both spellings behave identically.
        string value = System.Environment.ExpandEnvironmentVariables(rawValue.Trim());

        // No SessionState exists during provider initialization to resolve a relative path
        // against, and "relative to what" would differ anyway: the current location is very often
        // an Orch drive rather than a file system one. Require a rooted path.
        if (!System.IO.Path.IsPathRooted(value))
        {
            return new ConfigPathResolution
            {
                Path = defaultPath,
                IsOverride = false,
                Warning = $"{ConfigPathEnvVar} is set to \"{rawValue}\", which is not a rooted path. It was ignored and \"{defaultPath}\" is used instead. Specify a full path -- a UNC path is recommended, because a mapped drive letter is not visible to services or to another logon session.",
            };
        }

        // A trailing separator says "folder" outright, so there is nothing left to disambiguate.
        if (System.IO.Path.EndsInDirectorySeparator(value))
        {
            return new ConfigPathResolution
            {
                Path = System.IO.Path.Combine(value, ConfigFileName),
                IsOverride = true,
            };
        }

        return new ConfigPathResolution
        {
            Path = value,
            FolderCandidate = System.IO.Path.Combine(value, ConfigFileName),
            IsOverride = true,
        };
    }

    internal static ConfigPathResolution ResolveConfigPath()
        => ResolveConfigPath(System.Environment.GetEnvironmentVariable(ConfigPathEnvVar), GetDefaultConfigFilePath());

    /// <summary>
    /// The configuration file in effect. When the location could name either a file or a folder
    /// this reads to find out, so it can block for as long as <see cref="TryReadConfigFile"/>
    /// does; the read is memoized, so the read that usually follows is free.
    /// </summary>
    public static string GetConfigFilePath()
    {
        var resolution = ResolveConfigPath();
        if (resolution.FolderCandidate is null) return resolution.Path;

        TryReadConfigFile(resolution, out _, out string effectivePath, out _);
        return effectivePath;
    }

    // An offline share blocks File.Exists / ReadAllText until the SMB (or DFS) client gives up,
    // which can take a minute or more. Module autoloading means that hang lands on the user's
    // first UiPathOrch command with no visible cause, so reads go through a bounded wait.
    private const int ConfigReadTimeoutMs = 10_000;

    // OrchProvider, OrchDuProvider and OrchTmProvider each initialize their default drives during
    // module load and each needs the same file. Memoize briefly so one load costs one read (and,
    // on a dead share, one timeout instead of three). The window is far shorter than any
    // edit-then-reload cycle, and Import-OrchConfig bypasses it outright.
    private const int ConfigReadMemoMs = 5_000;
    private static readonly object _configReadLock = new();
    private static string? _configReadKey;
    private static string _configReadEffectivePath = "";
    private static string? _configReadError;
    private static bool _configReadNotFound;
    private static long _configReadAt;

    /// <summary>
    /// Read the configuration file, resolving the file-or-folder question by trying rather than
    /// guessing. <paramref name="effectivePath"/> is the path that actually answered -- or the
    /// primary candidate when nothing did, so callers always have something to name.
    /// Returns false with a human-readable <paramref name="error"/> when the file is missing,
    /// unreadable, or the read timed out; <paramref name="notFound"/> separates "nothing is
    /// there" from "something went wrong reading it", which is the difference between offering
    /// to create a template and refusing to touch the path.
    /// </summary>
    internal static bool TryReadConfigFile(
        ConfigPathResolution resolution,
        out string? json,
        out string effectivePath,
        out string? error,
        bool bypassMemo = false)
        => TryReadConfigFile(resolution, out json, out effectivePath, out error, out _, bypassMemo);

    /// <inheritdoc cref="TryReadConfigFile(ConfigPathResolution, out string?, out string, out string?, bool)"/>
    internal static bool TryReadConfigFile(
        ConfigPathResolution resolution,
        out string? json,
        out string effectivePath,
        out string? error,
        out bool notFound,
        bool bypassMemo = false)
    {
        string key = resolution.Path + " " + (resolution.FolderCandidate ?? "");

        lock (_configReadLock)
        {
            // The memo remembers only WHICH path answered and how it failed -- never the file's
            // content. That content carries plaintext AppSecret / Password / PAT values, and a
            // static would keep a copy of them reachable for the rest of the process. Re-reading
            // a file that just answered costs microseconds; what the memo is actually worth is
            // the dead-share case, where the three providers would otherwise each pay the full
            // timeout during one module load -- and there is no content to cache there anyway.
            bool memoHit = !bypassMemo
                && string.Equals(_configReadKey, key, StringComparison.OrdinalIgnoreCase)
                && System.Environment.TickCount64 - _configReadAt < ConfigReadMemoMs;

            if (memoHit && _configReadError is not null)
            {
                json = null;
                effectivePath = _configReadEffectivePath;
                error = _configReadError;
                notFound = _configReadNotFound;
                return false;
            }

            if (memoHit && TryReadOne(_configReadEffectivePath, out json, out error, out notFound))
            {
                effectivePath = _configReadEffectivePath;
                return true;
            }

            // No memo, or the remembered path stopped answering -- resolve from scratch.
            effectivePath = resolution.Path;
            bool ok = TryReadOne(resolution.Path, out json, out error, out notFound);

            // Fall back to the folder reading ONLY on not-found. A timeout means the share is
            // unreachable, so a second attempt buys nothing and costs another full timeout;
            // access-denied and malformed JSON are answers, not reasons to look elsewhere.
            if (!ok && notFound && resolution.FolderCandidate is not null)
            {
                if (TryReadOne(resolution.FolderCandidate, out json, out string? folderError, out bool folderNotFound))
                {
                    effectivePath = resolution.FolderCandidate;
                    error = null;
                    ok = true;
                }
                else
                {
                    notFound = folderNotFound;
                    error = $"No configuration file at \"{resolution.Path}\" (read as a file) or \"{resolution.FolderCandidate}\" (read as a folder). {folderError}";
                }
            }

            _configReadKey = key;
            _configReadEffectivePath = effectivePath;
            _configReadError = error;
            _configReadNotFound = notFound;
            _configReadAt = System.Environment.TickCount64;

            return ok;
        }
    }

    private static bool TryReadOne(string path, out string? json, out string? error, out bool notFound)
    {
        json = null;
        error = null;
        notFound = false;
        try
        {
            // On timeout the worker stays blocked until the OS gives up, so it must NOT be a
            // thread-pool thread: GetConfigFilePath is reachable from Get-OrchConfigPath, and a
            // loop run against a dead share would otherwise park one pool thread per call and
            // starve unrelated async work. LongRunning gives it a dedicated thread instead.
            var task = Task.Factory.StartNew(
                () => File.Exists(path) ? File.ReadAllText(path) : (string?)null,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            if (!task.Wait(ConfigReadTimeoutMs))
            {
                error = $"Timed out after {ConfigReadTimeoutMs / 1000} seconds while reading the configuration file. The path may be on a network share that is currently unreachable.";
            }
            else if (task.Result is null)
            {
                notFound = true;
                error = "The configuration file was not found.";
            }
            else
            {
                json = task.Result;
            }
        }
        catch (Exception ex)
        {
            error = (ex is AggregateException agg ? agg.GetBaseException() : ex).Message;
        }

        return error is null;
    }

    private static string? _logFolderPath = null;
    public static string GetLogFolderBasePath()
    {
        if (string.IsNullOrEmpty(_logFolderPath))
        {
            _logFolderPath = System.IO.Path.Combine(GetBasePath(), "Logs");
            // Owner-only: HTTP bodies land here, and those include credentials submitted by
            // cmdlets (the drive warns about exactly this when logging is enabled).
            OwnerOnlyPath.CreateRestrictedDirectory(_logFolderPath);
            //string logFileName = $"{DateTime.Today:yyyy-MM-dd}.log";
            //return System.IO.Path.Combine(driveDirectory, logFileName);
        }
        return _logFolderPath;
    }

    private static readonly string[] configFileLanguages = ["de", "en", "fr", "ja", "ko", "ro", "tr"];

    public static void EnsureDefaultConfigFileExists()
    {
        var resolution = ResolveConfigPath();

        // Never materialize a template at an overridden location. That location is typically a
        // share several people point at, and dropping a fresh empty config onto it -- because one
        // machine happened to reach it a moment before it came online, say -- is destructive in a
        // way the default per-user path never is. An overridden file is the user's to create.
        if (resolution.IsOverride) return;

        string configFilePath = resolution.Path;
        if (!System.IO.File.Exists(configFilePath))
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!configFileLanguages.Contains(lang)) lang = "en";

            SaveResourceToFile($"UiPathOrch.Resources.{lang}.UiPathOrchConfig.json", configFilePath);

            // The config file holds plaintext credentials (AppSecret / PAT / Password), so on
            // Unix restrict it to owner read/write — the default FileMode.CreateNew would
            // otherwise inherit the umask. Shared with the log paths, which carry the same class
            // of secret in their request/response bodies; see OwnerOnlyPath.
            OwnerOnlyPath.RestrictFile(configFilePath);
        }
    }

    private void WarningPSDriveConfig(PSDrive drive)
    {
        // Only output Scope warnings when no password is set. PAT drives are
        // also exempt: a personal access token's scopes are fixed server-side
        // when the token is created, so the config Scope cannot change what
        // the token is authorized to do — a "fix your Scope" warning would be
        // misleading. (Scope on a PAT drive remains meaningful only as the
        // opt-in declaration that mounts the Du*/Tm* companion drives.)
        if (string.IsNullOrEmpty(drive.Password) && string.IsNullOrEmpty(drive.AccessToken))
        {
            if (string.IsNullOrWhiteSpace(drive.Scope))
            {
                WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Scope is not specified!");
            }
            else
            {
                string lowerScope = drive.Scope?.ToLowerInvariant() ?? "";

                if (lowerScope.Contains("or."))
                {
                    if (!lowerScope.Contains("or.folders"))
                    {
                        WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Ensure the \"OR.Folders.Read\" scope is included to retrieve folder information.");
                    }

                    if (!lowerScope.Contains("or.settings"))
                    {
                        WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Ensure the \"OR.Settings.Read\" scope is included to retrieve the API version needed to properly call Orchestrator APIs.");
                    }

                    if (string.IsNullOrEmpty(drive.AppSecret) && !lowerScope.Contains("or.users"))
                    {
                        WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Ensure the \"OR.Users.Read\" scope is included to access your personal workspace folder.");
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(drive.Root))
        {
            WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Root is not specified!");
        }
        else if ((drive.Root.EndsWith("/orchestrator_/") || drive.Root.EndsWith("/orchestrator_")))
        {
            WriteWarning($"\"{drive.Name}\": The \"Root\" value in UiPathOrchConfig.json should not contain '/orchestrator_/'. Run the Edit-OrchConfig cmdlet to open the file and update it manually.");
        }

        if (string.IsNullOrEmpty(drive.AccessToken) && string.IsNullOrEmpty(drive.Username))
        {
            if (string.IsNullOrWhiteSpace(drive.AppId))
            {
                WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": AppId is not specified!");
            }
            else
            {
                try
                {
                    Guid test = new(drive.AppId);
                }
                catch
                {
                    WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": AppId is invalid!");
                }
            }
        }

        // If Username is not specified, AppSecret is not specified, and AccessToken is not specified,
        // then RedirectUrl must be specified.
        if (string.IsNullOrWhiteSpace(drive.Username) &&
            string.IsNullOrWhiteSpace(drive.AppSecret) &&
            string.IsNullOrEmpty(drive.AccessToken) &&
            string.IsNullOrWhiteSpace(drive.RedirectUrl))
        {
            WriteWarning($"\"{drive.Name}\": The \"RedirectUrl\" value should be specified.");
        }
    }

    protected override Collection<PSDriveInfo>? InitializeDefaultDrives()
    {
        var resolution = ResolveConfigPath();
        if (resolution.Warning is not null) WriteWarning(resolution.Warning);

        // Both locations go through the same bounded, guarded read. The default one is not
        // reliably local either: GetBasePath builds on the Documents folder, which under
        // enterprise Folder Redirection is routinely a UNC path -- and the shadow providers
        // already read it this way, so anything else here would mount the Orch drives while
        // their Du/Tm companions silently gave up.
        bool read = TryReadConfigFile(
            resolution, out string? json, out string configFilePath, out string? readError, out bool notFound);

        if (!read && resolution.IsOverride)
        {
            // No fallback to the default file: mounting a different set of tenants than the one
            // that was asked for -- silently, because a share happened to be offline -- is worse
            // than mounting none.
            WriteWarning($"\"{configFilePath}\": {readError}");
            WriteWarning($"The configuration file location comes from the {ConfigPathEnvVar} environment variable. No drives were mounted; the default configuration file was deliberately not used as a fallback.");
            return null;
        }

        if (!read && !notFound)
        {
            // The default file exists but could not be read -- locked by an editor, or denied.
            // Creating a template over it would destroy the drive definitions it still holds.
            WriteWarning($"\"{configFilePath}\": {readError}");
            return null;
        }

        if (json is not null)
        {
            try
            {
                _config = JsonSerializer.Deserialize<UiPathOrchConfig>(json, JsonTools.jsonAllowComments) ?? throw new Exception("Deserialization resulted in a null object.");
            }
            catch (Exception ex)
            {
                WriteWarning($"\"{configFilePath}\": {ex.Message}");

                // Never open an overridden config in an editor. It is typically shared, so every
                // user hitting the same parse error would get it opened on their machine -- and a
                // partial read while someone else is saving is enough to trigger that, turning one
                // person's edit into a room full of concurrent editors racing to overwrite it.
                if (!resolution.IsOverride && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var startInfo = new ProcessStartInfo("notepad.exe")
                    {
                        Arguments = configFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }

                WriteWarning($"Please edit '{configFilePath}'. After saving your changes, run `Import-OrchConfig` to reload the configuration.");

                return null;
            }

            Collection<PSDriveInfo> ret = base.InitializeDefaultDrives();

            if (_config!.Proxy is not null)
            {
                _config.Proxy.Enabled ??= true;
            }
            _config!.Enabled ??= true;

            // A config with no "PSDrives" array deserializes fine and has nothing to mount.
            // Import-OrchConfig reports that case; here just mount nothing rather than throwing a
            // raw NullReferenceException out of provider initialization. Reachable now that
            // UIPATHORCH_CONFIG_PATH can point this at an arbitrary hand-written file -- the
            // shadow providers guard the same loop for the same reason.
            foreach (var drive in _config!.PSDrives ?? [])
            {
                drive.CascadePSDriveFromGlobalSettings(_config);
                if (!drive.Enabled.GetValueOrDefault()) continue;

                WarningPSDriveConfig(drive);

                try
                {
                    var orchDrive = new OrchDriveInfo(ProviderInfo, drive);
                    ret.Add(orchDrive);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.Name, ex),
                        "NewPSDriveError", ErrorCategory.InvalidData, drive.Name));
                }
            }
            return ret;
        }
        else
        {
            #region Do not create config file if env var UIPATHORCH_SUPPRESS_CONFIG_CREATION is 1
            var suppressConfigCreation = System.Environment.GetEnvironmentVariable("UIPATHORCH_SUPPRESS_CONFIG_CREATION");
            bool shouldSuppress =
                suppressConfigCreation?.Equals("1", StringComparison.OrdinalIgnoreCase) == true ||
                suppressConfigCreation?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (shouldSuppress) return null;
            #endregion

            EnsureDefaultConfigFileExists();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var startInfo = new ProcessStartInfo("notepad.exe")
                {
                    Arguments = configFilePath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                WriteWarning($"Please edit '{configFilePath}'. After saving your changes, run `Import-OrchConfig` to reload the configuration.");
            }
            else
            {
                // Cannot reliably launch an editor in Linux environments..
                // Move to the directory and output a message prompting the user to edit. Run popd to return to the original directory.
                string folder = System.IO.Path.GetDirectoryName(configFilePath);
                string fileName = System.IO.Path.GetFileName(configFilePath);

                // Would like to push the current location to the default stack,
                // but it doesn't seem to work properly at this point..
                //SessionState.Path.PushCurrentLocation("default");

                // Move to the path where the config file is located
                SessionState.Path.SetLocation(folder);

                WriteWarning($"Please edit './{fileName}'. After saving your changes, run Import-OrchConfig to reload the configuration.");
            }
            return null;
        }
    }

    protected override object NewDriveDynamicParameters()
    {
        return new NewDrive_Parameters();
    }

    protected override PSDriveInfo? NewDrive(PSDriveInfo drive)
    {
        // If drive is an OrchDriveInfo, InitializeDefaultDrives() was executed (not a New-PSDrive cmdlet call)
        if (drive is OrchDriveInfo orchDrive)
        {
            // Depending on the provider class loading order, the UiPathOrchTm provider may not be registered yet at this point.
            // By performing the following logic in NewDrive for all providers, we ensure UiPathOrch and UiPathOrchTm are reliably associated.
            #region Find and associate Du drives
            try
            {
                // var duProvider = SessionState.Provider.GetOne("UiPathOrchDu");
                // If no exception is thrown, tmProvider is not null
                var duDrive = SessionState.Drive.Get(drive.Name + "Du") as OrchDuDriveInfo;
                duDrive!.ParentDrive = (OrchDriveInfo)drive;
            }
            catch { } // If this fails, OrchDuDriveInfo.NewDrive should handle it
            #endregion


            // Depending on the provider class loading order, the UiPathOrchTm provider may not be registered yet at this point.
            // By performing the following logic in NewDrive for all providers, we ensure UiPathOrch and UiPathOrchTm are reliably associated.
            #region Find and associate Tm drives
            try
            {
                // var tmProvider = SessionState.Provider.GetOne("UiPathOrchTm");
                // If no exception is thrown, tmProvider is not null
                var tmDrive = SessionState.Drive.Get(drive.Name + "Tm") as OrchTmDriveInfo;
                tmDrive!.ParentDrive = (OrchDriveInfo)drive;
            }
            catch { } // If this fails, OrchTmDriveInfo.NewDrive should handle it
            #endregion

            #region adding library feed drive
            //try
            //{
            //    var providerInfo = this.SessionState.Provider.GetOne("UiPathOrchLib");
            //    var driveInfo = new LibraryDriveInfo((drive as OrchDriveInfo)!, providerInfo);
            //    SessionState.Drive.New(driveInfo, scope: "Global");
            //}
            //catch { }

            #endregion

            return orchDrive;
        }

        // If drive is a PSDriveInfo, New-PSDrive -PSProvider UiPathOrch was executed (not InitializeDefaultDrives())
        var parameters = DynamicParameters as NewDrive_Parameters;
        PSDrive psDrive = new()
        {
            Name = drive.Name, // Mandatory, so it is always passed via -Name
            Root = drive.Root, // Mandatory, so it is always passed via -Root
            Description = drive.Description,
            IdentityUrl = parameters?.IdentityUrl,
            AppId = parameters?.AppId,
            AppSecret = parameters?.AppSecret,
            RedirectUrl = parameters?.RedirectUrl,
            HttpListener = parameters?.HttpListener,
            Scope = parameters?.OAuthScope,
            AccessToken = parameters?.AccessToken,
            Username = parameters?.Username,
            Password = parameters?.Password,
            IgnoreSslErrors = parameters?.IgnoreSslErrors,
            Enabled = true
        };

        psDrive.CascadePSDriveFromGlobalSettings(_config);
        WarningPSDriveConfig(psDrive);

        return new OrchDriveInfo(ProviderInfo, psDrive);
    }

    protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        ((OrchDriveInfo)drive).ClearAllCache();
        return base.RemoveDrive(drive);
    }
}
