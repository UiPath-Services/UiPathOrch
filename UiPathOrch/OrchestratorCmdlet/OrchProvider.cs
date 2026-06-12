//#undef DEBUG

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

// Installation instructions
// 1. Install PowerShell 7. Download PowerShell-7.x.x-win-x64.msi. It can be installed side by side with PowerShell 5.
// https://github.com/PowerShell/PowerShell/releases/
// 2. Run Install-Package UiPathOrch.
// 3. Run Import-Package UiPathOrch. Follow the on-screen instructions to configure the settings file. You need to create an external app ID in the Orchestrator admin console.
//
// If a warning is displayed that PSReadLine is disabled, Tab or Ctrl+Space completion will not work.
// Running Import-Module PSReadLine will enable completion. A permanent fix is described at:
// https://iwasi.hatenablog.jp/entry/2020/12/13/161312
//
// To automatically run Import-Module UiPathOrch when pwsh.exe starts, add this command line to the pwsh.exe profile.
// You can check the profile path by typing $profile in the pwsh console.

namespace UiPath.PowerShell.Core;

public class GetChildItems_Parameters
{
    [Parameter]
    public SwitchParameter Reload { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }
}

public class NewDrive_Parameters
{
    // Parameter set 1: AppId + AppSecret
    private const string AppAuthParamSet = "AppAuth";

    // Parameter set 2: Username + Password
    private const string UserAuthParamSet = "UserAuth";

    // Parameter set 3: AccessToken
    private const string TokenAuthParamSet = "TokenAuth";

    [Parameter(ParameterSetName = AppAuthParamSet)]
    public string? IdentityUrl { get; set; }

    [Parameter(ParameterSetName = AppAuthParamSet)]
    public string? AppId { get; set; }

    [Parameter(ParameterSetName = AppAuthParamSet)]
    public string? AppSecret { get; set; }

    [Parameter(ParameterSetName = AppAuthParamSet)]
    public string? RedirectUrl { get; set; }

    [Parameter(ParameterSetName = AppAuthParamSet)]
    public string? HttpListener { get; set; }

    [Parameter(ParameterSetName = AppAuthParamSet)]
    [Parameter(ParameterSetName = TokenAuthParamSet)]
    public string? OAuthScope { get; set; }

    [Parameter(ParameterSetName = TokenAuthParamSet)]
    public string? AccessToken { get; set; }

    [Parameter(ParameterSetName = UserAuthParamSet)]
    public string? Username { get; set; }

    [Parameter(ParameterSetName = UserAuthParamSet)]
    public string? Password { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public bool? IgnoreSslErrors { get; set; }
}

[CmdletProvider("UiPathOrch", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(Folder), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(Folder), ProviderCmdlet = ProviderCmdlet.GetItem)]
public partial class OrchProvider : NavigationCmdletProvider, IPropertyCmdletProvider
{
    private static UiPathOrchConfig? _config;

    /// <summary>
    /// Called from Import-OrchConfig. Updates _config when the configuration file is reloaded.
    /// </summary>
    internal static void SetConfig(UiPathOrchConfig config) => _config = config;

    /// <summary>
    /// The last write time of the configuration file when InitializeDefaultDrives() mounted drives.
    /// Import-OrchConfig compares this value with the current file's last write time,
    /// and skips remounting if they are the same.
    /// </summary>
    internal static DateTime? ConfigLastWriteTimeUtc { get; set; }

    #region CmdletProvider overrides

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        //System.Diagnostics.Debugger.Launch();
        ProviderInfo ret = base.Start(providerInfo);
        OrchDriveInfo.SessionState = base.SessionState;
        return ret;
    }

    // Always use GetOrchDriveInfo() instead of OrchDriveInfo whenever possible.
    protected OrchDriveInfo OrchDriveInfo => (OrchDriveInfo)this.PSDriveInfo;
    protected OrchDriveInfo? GetOrchDriveInfo(string path)
    {
        if (OrchDriveInfo is not null)
        {
            return OrchDriveInfo;
        }
        string driveName = OrchDriveInfo.ExtractDriveName(path);
        return SessionState.Drive.Get(driveName) as OrchDriveInfo;
    }

    protected OrchDriveInfo? ExtractOrchDriveInfo(string path)
    {
        string driveName = OrchDriveInfo.ExtractDriveName(path);
        return SessionState.Drive.Get(driveName) as OrchDriveInfo;
    }

    protected (OrchDriveInfo?, Folder?) ExtractOrchDriveAndFolder(string path)
    {
        string driveName = OrchDriveInfo.ExtractDriveName(path);
        OrchDriveInfo drive = SessionState.Drive.Get(driveName) as OrchDriveInfo;
        if (drive is null)
        {
            return (null, null);
        }
        int colonIndex = path.IndexOf(':');
        if (colonIndex == -1)
        {
            throw new ArgumentException($"Invalid path format: '{path}'. Expected a drive-qualified path (e.g., 'DriveName:\\Path').", nameof(path));
        }
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path.Substring(colonIndex + 1));
        Folder folder = drive.GetFolder(orchPath);
        return (drive, folder);
    }

    #endregion CmdletProvider overrides

    #region DriveCmdletProvider overrides

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

    public static string GetConfigFilePath()
    {
        string configFileName = "UiPathOrchConfig.json";
        return System.IO.Path.Combine(GetBasePath(), configFileName);
    }

    private static string? _logFolderPath = null;
    public static string GetLogFolderBasePath()
    {
        if (string.IsNullOrEmpty(_logFolderPath))
        {
            _logFolderPath = System.IO.Path.Combine(GetBasePath(), "Logs");
            Directory.CreateDirectory(_logFolderPath);
            //string logFileName = $"{DateTime.Today:yyyy-MM-dd}.log";
            //return System.IO.Path.Combine(driveDirectory, logFileName);
        }
        return _logFolderPath;
    }

    private static readonly string[] configFileLanguages = ["de", "en", "fr", "ja", "ko", "ro", "tr"];

    public static void EnsureDefaultConfigFileExists()
    {
        string configFilePath = GetConfigFilePath();
        if (!System.IO.File.Exists(configFilePath))
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!configFileLanguages.Contains(lang)) lang = "en";

            SaveResourceToFile($"UiPathOrch.Resources.{lang}.UiPathOrchConfig.json", configFilePath);
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
                string lowerScope = drive.Scope?.ToLower() ?? "";

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
        string configFilePath = GetConfigFilePath();
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            try
            {
                _config = JsonSerializer.Deserialize<UiPathOrchConfig>(json, JsonTools.jsonAllowComments);
                if (_config is null) throw new Exception("Deserialization resulted in a null object.");
            }
            catch (Exception ex)
            {
                WriteWarning($"\"{configFilePath}\": {ex.Message}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

                //else
                //{
                //    string folder = System.IO.Path.GetDirectoryName(configFilePath);
                //    string fileName = System.IO.Path.GetFileName(configFilePath);

                //    SessionState.Path.SetLocation(folder);
                //    WriteWarning($"Please edit ./{fileName}. Once edited, launch a new PS session and `Import-Module UiPathOrch` to mount your Orchestrator tenants as PSDrives.");
                //}
            }

            Collection<PSDriveInfo> ret = base.InitializeDefaultDrives();

            if (_config!.Proxy is not null)
            {
                _config.Proxy.Enabled ??= true;
            }
            _config!.Enabled ??= true;

            foreach (var drive in _config!.PSDrives!)
            {
                drive.CascadePSDriveFromGlobalSettings(_config);
                if (!drive.Enabled.GetValueOrDefault()) continue;

                WarningPSDriveConfig(drive);

                //if (string.IsNullOrEmpty(drive.IdentityUrl))
                //{
                //    drive.IdentityUrl = _baseUrl.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase)
                //        ? _baseUrl + "/identity_/connect/token"
                //        : _baseUrl + "/identity/connect/token";
                //}

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
            ConfigLastWriteTimeUtc = File.GetLastWriteTimeUtc(configFilePath);
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

    #endregion DriveCmdletProvider overrides

    #region ItemCmdletProvider overrides

    protected override void GetItem(string path)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        // Resolve the path literally, exactly like every other single-item resolution site
        // (InvokeDefaultAction/GetProperty/SetProperty/Rename/Move all use PSPathToOrchPath(path)
        // with no Unescape). A WildcardPattern.Unescape here corrupted folder names that contain
        // a literal backtick: -LiteralPath 'Orch1:\Foo`bar' was unescaped to 'Foobar' and GetItem
        // found nothing, while Get-ItemProperty on the same path worked. -Path wildcard expansion
        // is the engine's job (via ItemExists/GetChildNames), so a concrete path reaching GetItem
        // is already either literal (-LiteralPath) or a resolved raw child name — never an escaped
        // pattern that needs unescaping here.
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        Folder f = drive.GetFolder(orchPath);
        if (f is not null)
        {
            // PSPath must be drive-qualified ("Orch1:\..."), identical to GetChildItems:
            // prepend the drive name and escape wildcard metachars. Otherwise Get-Item's
            // PSPath loses the drive ("::\Autopilot") and -LiteralPath/PSPath binding from
            // Get-Item output fails to resolve.
            string psPath = drive.NameColon + PathTools.EscapePSText2(OrchDriveInfo.OrchProviderPathToPSPath(f!.FullyQualifiedName!));
            WriteItemObject(f, psPath, true);
        }
    }

    protected override void InvokeDefaultAction(string path)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        string endpoint = drive.OrchAPISession._base_url;

        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        Folder folder = drive.GetFolder(orchPath);

        var (tenantId, _) = drive.GetTenantId();
        bool bQuery = false;
        //if (drive.OrchAPISession.ApiVersion < 12 && tenantId.HasValue)
        if (tenantId.HasValue)
        {
            endpoint += $"?tid={tenantId.Value}";
            bQuery = true;
        }

        if (folder is not null && folder.Id.HasValue && folder.Id! != 0)
        {
            endpoint += bQuery ? '&' : '?';
            endpoint += $"fid={folder.Id}";
        }

        Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
    }

    // TODO: Should we implement something here? Though it seems like this is never called..
    protected override bool IsValidPath(string path)
    {
        //Tools.DebugFuncEntry("IsValidPath", path, SessionState);
        //Tools.DebugFuncExit("IsValidPath", true, SessionState);
        return true;
    }

    // Probably implementation complete
    // The ItemExists method does not seem to need to handle wildcards.
    protected override bool ItemExists(string path)
    {
        //var (drive, folder) = ExtractOrchDriveAndFolder(path);
        //return folder is not null;

        if (path is null)
        {
            return false;
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
        if (ocPath == "")
        {
            return true;
        }
        else
        {
            var drive = GetOrchDriveInfo(path);
            if (drive is null)
            {
                return false;
            }

            Folder folder = drive.GetFolder(ocPath);
            return folder is not null;
        }
    }

    #endregion ItemCmdletProvider overrides

    #region ContainerCmdletProvider overrides

    protected override void GetChildItems(string path, bool recurse)
    {
        GetChildItems(path, recurse, 0);
    }

    // Returns the depth of the folder (number of slashes + 1)
    // Depth of "": 0
    // Depth of "folder": 1
    // Depth of "folder/sub": 2
    public static uint FolderDepth(string orchestratorPath)
    {
        if (string.IsNullOrEmpty(orchestratorPath))
        {
            return 0;
        }
        return (uint)orchestratorPath.Count(c => c == '/') + 1;
    }

    private static readonly string DefaultCsvName = "ExportedFolders.csv";

    // The Path column holds the PARENT path so Import-Csv | New-Item recreates each folder
    // under its parent (New-Item -Path <parent> -Name <leaf>). folder.FullName is the folder's
    // own path now, so derive the parent from FullyQualifiedName here.
    private static string FolderParentPsPath(OrchDriveInfo drive, Folder folder)
    {
        int idx = folder.FullyQualifiedName!.LastIndexOf('/');
        return idx != -1
            ? drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName.Substring(0, idx))
            : drive.NameColonSeparator;
    }

    private string? ExportCsvFile(OrchDriveInfo drive, string exportCsv, Encoding? csvEncoding, IEnumerable<Folder> output)
    {
        Encoding encoding = csvEncoding ?? Encoding.Default;
        string[] headers = ["Path", "Name", "Description", "FeedType"];

        var (physicalCsvPath, providerCsvPath) = OrchestratorPSCmdlet.GenerateCsvFilePath(exportCsv, SessionState, DefaultCsvName);
        using var writer = OrchestratorPSCmdlet.WriteCsvHeader(physicalCsvPath, encoding, headers);
        if (writer is null) return null;

        // Write a data row for each folder
        foreach (var folder in output.Where(f => f.FolderType != "Personal"))
        {
            string[] line = [
                OrchestratorPSCmdlet.EscapeCsvValue(FolderParentPsPath(drive, folder)),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.DisplayName!),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.Description),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.FeedType)
            ];
            writer.WriteCsvLine(line);
        }

        return providerCsvPath;
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        drive.OrchAPISession.EnsureAuthenticated();

        // Check Entra ID warning (once per drive session).
        // The warning is set to PendingWarning (not displayed here) because GetChildItems
        // is also called during tab completion, where WriteWarning output would be silently lost.
        // PendingWarning is flushed by OrchCmdlets.BeginProcessing, which also sets EntraIdWarningChecked.
        if (!drive.OrchAPISession.EntraIdWarningChecked && drive.OrchAPISession.PendingWarning is null)
        {
            try
            {
                if (drive.OrchAPISession.AuthManager.IsNonEntraIdUser())
                {
                    var prtId = drive.GetPartitionGlobalId();
                    if (prtId is not null)
                    {
                        var authSetting = drive.PmAuthenticationSetting.Get();
                        if (authSetting?.authenticationSettingType == "aad")
                        {
                            drive.OrchAPISession.PendingWarning = $"[{drive.NameColon}] You are not signed in to the organization via Entra ID. Some operations may require organization-level access. Use Switch-OrchCurrentUser to sign in with a different account.";
                        }
                        else
                        {
                            drive.OrchAPISession.EntraIdWarningChecked = true;
                        }
                    }
                }
                else
                {
                    drive.OrchAPISession.EntraIdWarningChecked = true;
                }
            }
            catch { } // Swallow - don't block navigation for a warning
        }

        var parameters = DynamicParameters as GetChildItems_Parameters;
        if (parameters is not null && parameters.Reload.IsPresent)
        {
            drive._dicFolders = null;
            drive._dicFoldersForEnumFolders = null;
            drive.PersonalWorkspaces.ClearCache();
        }

        if (!recurse)
        {
            depth = 0;
        }

        //string orchPath = OrchDriveInfo.PSPathToOrchPath(path).ToLower();
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        uint currentDepth = FolderDepth(orchPath);

        HashSet<string> dupCheck = [];

        List<Folder> csvOutput = null;

        // Returns the parent portion of a FullyQualifiedName ("A/B/C" -> "A/B", "X" -> "").
        // Used to group siblings together so Format-Table's GroupBy keeps each Directory
        // section contiguous under -Recurse, instead of interleaving grandchildren between
        // sibling folders the way a flat alphabetical sort does.
        static string ParentOf(string fqn)
        {
            int idx = fqn.LastIndexOf('/');
            return idx < 0 ? "" : fqn[..idx];
        }

        try
        {
            // Collect matching folders first, then re-emit grouped by parent. Sort is stable
            // and only on parent path — sibling order within each parent comes from the
            // original _dicFolders sequence, which intentionally puts personal workspaces
            // before regular folders to match the Orchestrator web UI.
            var matched = new List<Folder>();
            string? orchPathStart = orchPath == "" ? null : orchPath + "/";

            foreach (var folder in drive.GetFolders())
            {
                if (Stopping) return;
                if (orchPathStart is not null &&
                    !folder.FullyQualifiedName!.StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                    continue;

                uint folderDepth = FolderDepth(folder.FullyQualifiedName!);
                if (folderDepth - (currentDepth + 1) <= depth)
                {
                    matched.Add(folder);
                }
            }

            foreach (var folder in matched.OrderBy(
                f => ParentOf(f.FullyQualifiedName!), StringComparer.OrdinalIgnoreCase))
            {
                if (Stopping) return;

                string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);

                if (string.IsNullOrEmpty(parameters?.ExportCsv))
                {
                    // A folder with the same name as a personal workspace may exist; to keep
                    // tab-completion and dir output predictable, suppress duplicate names.
                    if (dupCheck.Add(psPathEscaped))
                    {
                        WriteItemObject(folder, psPathEscaped, true);
                    }
                    else
                    {
                        WriteWarning($"The folder name '{folder.GetPSPath()}' (Id = {folder.Id}) is duplicated. This folder won't be listed.");
                    }
                }
                else
                {
                    csvOutput ??= [];
                    csvOutput.Add(folder);
                }
            }
        }
        catch (PipelineStoppedException)
        {
            // `dir | Select -First N`, Ctrl+C, etc. PowerShell stops the upstream
            // by throwing this; surfacing it as an ErrorRecord would emit a stray
            // "pipeline has been stopped" message after the data the caller wanted.
            throw;
        }
        catch (Exception ex)
        {
            var errorRecord = new ErrorRecord(new OrchException(path, ex), "GetChildItemsError", ErrorCategory.InvalidOperation, path);
            WriteError(errorRecord);
        }

        if (!string.IsNullOrEmpty(parameters?.ExportCsv))
        {
            string csvPath = ExportCsvFile(drive, parameters.ExportCsv, parameters.CsvEncoding, csvOutput!);
            WriteWarning($"CSV has been exported as '{csvPath}'.");
        }
    }

    protected override object GetChildItemsDynamicParameters(string path, bool recurse)
    {
        return new GetChildItems_Parameters();
    }

    //private static string EscapeWildcard(string path)
    //{
    //    return path
    //        .Replace("`", "``")
    //        .Replace("*", "`*")
    //        .Replace("?", "`?");
    //        //.Replace("[", "`[") // no need to escape [ and ]
    //        //.Replace("]", "`]");
    //}

    private static string UnescapeWildcard(string path)
    {
        return path
            //.Replace("``", "`")
            .Replace("`*", "*")
            .Replace("`?", "?");
        //.Replace("[", "`[") // no need to unescape [ and ]
        //.Replace("]", "`]");
    }

    // GetChildNames backs `Get-ChildItem -Name` and wildcard resolution (`cd t*`, `rmdir *`).
    // Per the provider contract it writes ONLY the child's name string as the item (not the
    // Folder object); the second WriteItemObject argument is the path the engine uses to build
    // the result's PSPath note-property.
    //
    // That path is emitted RAW (unescaped), mirroring the authoritative FileSystem provider,
    // whose GetChildNames does `WriteItemObject(fsinfo.Name, fsinfo.FullName, ...)` with the
    // unescaped FullName (PowerShell's FileSystemProvider.cs). Do NOT EscapePSText2 /
    // WildcardPattern.Escape it here: the PSPath built from this path binds to `-LiteralPath`
    // (`[Alias("PSPath")]`), and `EffectivePath` re-applies WildcardPattern.Escape on bind — so
    // a pre-escaped path would be escaped twice (e.g. a folder named `Fin*ce`) and fail to
    // resolve literally. Left raw, it round-trips: `dir | <cmdlet> -LiteralPath` and
    // `Get-Item -LiteralPath $f.PSPath` both resolve correctly. (EscapePSText2 in GetChildItems /
    // GetItem only escapes `* ?` and predates this; matching IT here would be the wrong target.)
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        OrchDriveInfo drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);

        if (ocPath == "")
        {
            // Direct children of the drive root = folders at depth 1, filtered the same way as
            // GetChildItems (by depth) so the two enumeration methods stay consistent. The
            // equivalent "!ParentId.HasValue" test also works here (GetFolders() masks every
            // top-level folder's ParentId to null), but matching GetChildItems is clearer.
            foreach (var folder in drive.GetFolders().Where(f =>
                f.FullyQualifiedName is not null && FolderDepth(f.FullyQualifiedName) == 1))
            {
                if (Stopping) return;
                string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                WriteItemObject(folder.DisplayName!, fullPath, true);
            }
        }
        else
        {
            Folder parentFolder = drive.GetFolder(ocPath);
            Int64 parentFolderId = parentFolder?.Id ?? 0;

            foreach (var folder in drive.GetFolders().Where(f => f.ParentId == parentFolderId))
            {
                if (Stopping) return;
                string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                WriteItemObject(folder.DisplayName!, fullPath, true);
            }
        }
    }

    // Always returning true seems to reduce accidental rmdir operations due to user error.
    protected override bool HasChildItems(string path)
    {
        // Report whether the folder actually has SUBFOLDERS. This must be accurate, not a constant:
        //  * PowerShell's wildcard path globber (Resolve-Path Orch1:\Shar*, Get-ChildItem Orch1:\*,
        //    and -Path <wildcard> which resolves through it) only enumerates a container's children
        //    when HasChildItems(container) is true — returning false here breaks all wildcard
        //    resolution.
        //  * Remove-Item's generic "...has children and the Recurse parameter was not specified"
        //    prompt is also driven by this; returning true unconditionally (the old behavior) made
        //    that prompt fire for empty folders too. With an accurate value, empty folders delete
        //    without that prompt, and RemoveItem adds its own content-aware confirmation.
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
            return false;

        return HasSubfolders(drive, OrchDriveInfo.PSPathToOrchPath(path));
    }

    // True if the folder at the given fully-qualified Orchestrator path has any direct subfolder.
    // "" is the drive root, whose direct children are the depth-1 folders.
    private static bool HasSubfolders(OrchDriveInfo drive, string fqn)
    {
        uint childDepth = FolderDepth(fqn) + 1;
        string start = fqn + "/";
        return drive.GetFolders().Any(f =>
            f.FullyQualifiedName is not null &&
            FolderDepth(f.FullyQualifiedName) == childDepth &&
            (fqn.Length == 0 || (f.FullyQualifiedName + "/").StartsWith(start, StringComparison.OrdinalIgnoreCase)));
    }

    public class NewItem_DynamicParameters
    {
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Processes_FolderHierarchy>))]
        public string? FeedType { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<DescriptionHere>))]
        public string? Description { get; set; }
    }

    protected override void NewItem(string path, string itemTypeName, object newItemValue)
    {
        //path = UnescapeWildcard(path);

        var dynamicParameters = DynamicParameters as NewItem_DynamicParameters;
        if (dynamicParameters is not null && string.IsNullOrEmpty(dynamicParameters.FeedType))
        {
            dynamicParameters!.FeedType = "Processes";
        }

        if (newItemValue is not null)
        {
            string name = (newItemValue as PSObject)?.Properties["Name"]?.Value as string;
            if (name is not null)
            {
                // TODO: Is Unescape() needed??
                //path = System.IO.Path.Combine(path, WildcardPattern.Unescape(name));
                path = System.IO.Path.Combine(path, name);
            }
        }

        if (ShouldProcess(path, "New Folder"))
        {
            var drive = GetOrchDriveInfo(path);

            if (drive is null)
            {
                return;
            }

            string parentPath = GetParentPath(path, "");
            if (!ItemExists(parentPath))
            {
                WriteError(new ErrorRecord(new OrchException(path, $"{parentPath} does not exist."), "NewItem", ErrorCategory.InvalidOperation, drive));
                return;
            }

            try
            {
                Int64? parentPathId;
                string displayName;

                if (parentPath == System.IO.Path.DirectorySeparatorChar.ToString())
                {
                    displayName = path.Substring(parentPath.Length);
                    parentPathId = null;
                }
                else
                {
                    displayName = path.Substring(parentPath.Length + 1);
                    string orchParentPath = OrchDriveInfo.PSPathToOrchPath(parentPath);
                    parentPathId = drive.GetFolder(orchParentPath)?.Id;
                }

                if (!string.IsNullOrEmpty(dynamicParameters?.FeedType) && (dynamicParameters.FeedType != "Processes" && dynamicParameters.FeedType != "FolderHierarchy"))
                {
                    WriteError(new ErrorRecord(new OrchException($"{path} ({dynamicParameters.FeedType})", "FeedType must be 'Processes' or 'FolderHierarchy'."), "NewFolderError", ErrorCategory.InvalidArgument, path));
                    return; // don't fall through to CreateFolder with an invalid FeedType (was a confusing double error)
                }

                Folder f = drive.OrchAPISession.CreateFolder(displayName,
                    dynamicParameters?.Description,
                    (parentPathId is null || parentPathId == 0) ? dynamicParameters?.FeedType : "Processes",
                    parentPathId);
                if (f is not null)
                {
                    f.FullName = path;
                    WriteItemObject(f, path, true);
                }
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "NewFolderError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return new NewItem_DynamicParameters();
    }

    protected override void RenameItem(string path, string newName)
    {
        //path = UnescapeWildcard(path);
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        // -NewName must be a leaf, not a path (Rename-Item renames in place, it does not move).
        // Reduce ".\Shared2" -> "Shared2"; reject names that point elsewhere (e.g. "..\Shared2").
        string? leaf = PathTools.RenameLeaf(path, newName);
        if (leaf is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"'{newName}' is not a valid new folder name. Supply a leaf name, not a path (Rename-Item renames in place; use Move-Item to move). Example: Rename-Item .\\Shared Shared2."), "RenameFolderError", ErrorCategory.InvalidArgument, path));
            return;
        }
        newName = leaf;

        string target = $"Item: {path} Destination: {System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path) ?? "", newName)}";
        if (ShouldProcess(target, "Rename Folder"))
        {
            try
            {
                string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
                Folder? folder = drive.GetFolder(orchPath);
                if (folder is null)
                    return;
                drive.OrchAPISession.EditFolder(folder, newName!);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;

                //if (DynamicParameters is RuntimeDefinedParameterDictionary parameters)
                //{
                //    string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
                //    Folder? folder = drive.GetFolder(orchPath);
                //    if (folder is null)
                //        return;

                //    string description = parameters["Description"].Value as string;
                //    if (folder.DisplayName == newName && folder.Description == description)
                //        return;

                //    newName ??= folder.DisplayName;
                //    description ??= folder.Description;

                //    drive.OrchAPISession.EditFolder(folder, newName!, description!);
                //    drive._dicFolders = null;
                //    drive._dicFoldersForEnumFolders = null;
                //}
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameFolderError", ErrorCategory.InvalidOperation, path);
                WriteError(errorRecord);
            }
        }
    }

    protected override void RemoveItem(string path, bool recurse)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        int index = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
        if (index != -1)
        {
            string parentPart = path.Substring(0, index);
            string childPart = path.Substring(index);
            path = parentPart + UnescapeWildcard(childPart);
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
        Folder folder = drive.GetFolder(ocPath);
        if (folder is null)
        {
            drive.ClearAllCache();
            folder = drive.GetFolder(ocPath);
        }
        if (folder is null) return;

        if (ShouldProcess(path, "Remove Folder"))
        {
            // Content-aware confirmation, like the Orchestrator web delete dialog ("this folder
            // contains N assets, M processes... really delete?"). Skipped for -Force / -Recurse
            // (explicit opt-out) and for empty folders (no prompt at all). Counting only runs on
            // this interactive path, so scripts using -Recurse / -Force incur no extra API calls.
            // A folder that has subfolders already triggered PowerShell's generic "...has children
            // and the Recurse parameter was not specified" prompt (HasChildItems is true for it),
            // so don't ask again here. A folder with no subfolders leaves the engine silent, so warn
            // about the resources it contains (like the Orchestrator web delete dialog) before they
            // are removed without notice. -Force / -Recurse opt out.
            if (!Force && !recurse && !HasSubfolders(drive, folder.FullyQualifiedName ?? string.Empty))
            {
                string? summary = DescribeFolderContents(drive, folder);
                if (summary is not null &&
                    !ShouldContinue(summary + " Are you sure you want to continue?", "Confirm folder deletion"))
                {
                    return;
                }
            }

            try
            {
                // For personal workspace folders, disable the owner's workspace first
                // Otherwise, the deleted workspace folder will be automatically recreated immediately
                if (folder.FolderType == "Personal")
                {
                    var personalWorkspaces = drive.PersonalWorkspaces.Get();
                    var targetPersonalWorkspace = personalWorkspaces.FirstOrDefault(p => p.Id == folder.Id);
                    if (targetPersonalWorkspace is not null)
                    {
                        drive.DisablePersonalWorkspace(targetPersonalWorkspace.OwnerId);
                    }
                }

                drive.OrchAPISession.RemoveFolder(folder.Id ?? 0);

                if (folder.FolderType == "Personal")
                {
                    drive.PersonalWorkspaces.ClearCache();
                }

                // Folder structure changed — invalidate both caches; the next GetFolders
                // rebuilds atomically (lock + Volatile.Write) so concurrent readers cannot
                // observe a partial state.
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;

                drive.ClearFolderCache(folder);
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(path, ex), "RemoveFolderError", ErrorCategory.InvalidOperation, folder);
                WriteError(errorRecord);
            }
        }
    }

    // Best-effort one-line summary of a folder's contents for the deletion confirmation, in the
    // spirit of the Orchestrator web delete dialog. Returns null when the folder is empty (no
    // subfolders and no counted resources) so the caller skips the prompt. Counts are best-effort:
    // a resource type that can't be read (permissions, unsupported API version) is skipped rather
    // than blocking deletion. Only the folder's direct resources are counted; deleting the folder
    // also removes everything in its subfolders.
    // Best-effort summary of a folder's contained RESOURCES for the deletion confirmation, in the
    // spirit of the Orchestrator web delete dialog. Subfolders are intentionally not counted here:
    // this is only consulted for folders with no subfolders (folders that have subfolders take
    // PowerShell's own "...has children..." prompt instead), so subfolders would always be zero.
    // Returns null when the folder holds none of the counted resources, so the caller skips the
    // prompt. Counts are best-effort: a resource type that can't be read (permissions, unsupported
    // API version) is skipped rather than blocking deletion.
    private string? DescribeFolderContents(OrchDriveInfo drive, Folder folder)
    {
        int SafeCount(Func<int> counter)
        {
            try { return counter(); }
            catch { return 0; /* best-effort: a resource type we can't read shouldn't block deletion */ }
        }

        int processes = SafeCount(() => drive.Releases.Get(folder).Count);
        int triggers = SafeCount(() => drive.Triggers.Get(folder).Count);
        int assets = SafeCount(() => drive.Assets.Get(folder).Count);
        int buckets = SafeCount(() => drive.Buckets.Get(folder).Count);
        int queues = SafeCount(() => drive.Queues.Get(folder).Count);
        int actionCatalogs = SafeCount(() => drive.ActionCatalogs.Get(folder).Count);

        if (processes + triggers + assets + buckets + queues + actionCatalogs == 0)
            return null;

        // Fixed inventory in the requested order: processes, triggers, assets, buckets, queues,
        // action catalogs. Every count is shown, including zeros.
        string counts =
            $"Processes: {processes}, Triggers: {triggers}, Assets: {assets}, " +
            $"Buckets: {buckets}, Queues: {queues}, Action Catalogs: {actionCatalogs}";
        return $"The folder '{folder.GetPSPath()}' is not empty ({counts}). " +
               "Deleting it permanently removes the folder and all of its contents.";
    }

    #endregion

    #region NavigationCmdletProvider overrides

    protected override bool IsItemContainer(string path)
    {
        // This provider only returns folders (no file-equivalent items)
        return true;
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        string result = base.NormalizeRelativePath(path, basePath);
        if (result.StartsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1)
            result = result[1..];

        // Canonicalize folder name casing from cache (like FileSystemProvider.NormalizeThePath).
        // Only when folders are already cached — avoid triggering API calls before authentication.
        if (PSDriveInfo is OrchDriveInfo drive && drive._dicFolders != null && !string.IsNullOrEmpty(result))
        {
            string orchPath = result.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            var folder = drive.GetFolder(orchPath);
            if (folder != null)
            {
                result = folder.FullyQualifiedName?.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
        }

        return result ?? "";
    }

    protected override string MakePath(string parent, string child)
    {
        string retNew = base.MakePath(parent, child);
        if (retNew.EndsWith(System.IO.Path.DirectorySeparatorChar) && retNew.Length > 1 && retNew[retNew.Length - 2] != ':')
        {
            retNew = retNew.Substring(0, retNew.Length - 1);
        }
        return retNew;
    }

    protected override void MoveItem(string path, string destination)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        // Move-Item is a single same-drive operation (MoveFolder is one API call) — unlike
        // Copy-Item, which supports cross-drive transfers. If the destination explicitly names a
        // different drive, reject it: otherwise PSPathToOrchPath strips the drive qualifier and the
        // destination is silently reinterpreted on the source drive — a no-such-folder error at
        // best, a move into a same-named folder on the wrong drive at worst.
        var dstDrive = ExtractOrchDriveInfo(destination);
        if (dstDrive is not null && IsCrossDriveMovePure(drive.Name, dstDrive.Name))
        {
            WriteError(new ErrorRecord(new OrchException(destination, $"Cannot move across drives: '{path}' is on {drive.NameColon} but destination '{destination}' is on {dstDrive.NameColon}. Move-Item works within a single drive; use Copy-Item for cross-drive transfers."), "MoveItemError", ErrorCategory.InvalidArgument, destination));
            return;
        }

        if (ShouldProcess(path, "Move Folder"))
        {
            Folder srcFolder = null;
            try
            {
                string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
                srcFolder = drive.GetFolder(ocPath);
                if (srcFolder is null)
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "MoveItemError", ErrorCategory.ObjectNotFound, path));
                    return;
                }

                // The destination is the folder that becomes the new parent. It must already exist
                // (Move-Item does not create it). Resolve it on the source drive — cross-drive moves
                // aren't a single API call, so a different-drive destination won't resolve here and
                // surfaces as this clear error rather than an NRE.
                string ocDestination = OrchDriveInfo.PSPathToOrchPath(destination);
                Folder dstFolder = drive.GetFolder(ocDestination);
                if (dstFolder is null)
                {
                    WriteError(new ErrorRecord(new OrchException(destination, $"{drive.NameColon} does not have destination folder '{destination}'."), "MoveItemError", ErrorCategory.ObjectNotFound, destination));
                    return;
                }

                // Reject moving a folder into itself or one of its own descendants — either would
                // create a cycle (the moved subtree would become its own ancestor).
                bool intoSelfOrDescendant = srcFolder == dstFolder
                    || IsMoveIntoSelfOrDescendantPure(srcFolder.FullyQualifiedName, dstFolder.FullyQualifiedName);
                if (intoSelfOrDescendant)
                {
                    WriteError(new ErrorRecord(new OrchException(destination, $"Cannot move folder '{path}' into itself or one of its descendants."), "MoveItemError", ErrorCategory.InvalidOperation, destination));
                    return;
                }

                drive.OrchAPISession.MoveFolder(srcFolder.Id ?? 0, dstFolder.Id);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;
                drive.ClearFolderCache(srcFolder);
            }
            catch (Exception ex)
            {
                //int index = path.LastIndexOf('\\');
                //string pathName;
                //if (index != -1)
                //    pathName = path.Substring(index);
                //else
                //    pathName = path;
                WriteError(new ErrorRecord(new OrchException(path, ex), "MoveItemError", ErrorCategory.InvalidOperation, srcFolder));
            }
        }
    }

    // Pure cross-drive test for MoveItem (extracted so it is unit-testable without a live drive).
    // Move-Item is single-drive, unlike Copy-Item: a move is cross-drive only when the destination
    // explicitly names a drive different from the source. A null/empty destination drive name
    // (unqualified path / unknown drive) is treated as same-drive and resolved on the source drive.
    internal static bool IsCrossDriveMovePure(string srcDriveName, string? dstDriveName)
        => !string.IsNullOrEmpty(dstDriveName)
            && !string.Equals(dstDriveName, srcDriveName, StringComparison.OrdinalIgnoreCase);

    // Pure self/descendant test for MoveItem (extracted so it is unit-testable). True when the
    // destination folder is the source folder itself or one of its descendants, compared by
    // fully-qualified name with a '/' boundary so a sibling like "Foo2" is not mistaken for a
    // descendant of "Foo". A null/empty source FQN (e.g. root) is never a match here.
    internal static bool IsMoveIntoSelfOrDescendantPure(string? srcFqn, string? dstFqn)
        => !string.IsNullOrEmpty(srcFqn) && dstFqn is not null
            && (string.Equals(dstFqn, srcFqn, StringComparison.OrdinalIgnoreCase)
                || dstFqn.StartsWith(srcFqn + "/", StringComparison.OrdinalIgnoreCase));

    #endregion

    #region IPropertyCmdletProvider (folder Description)

    // Folders expose two text fields: DisplayName (read-only here — change it with Rename-Item)
    // and Description. Only Description is settable, because the Orchestrator folder PUT accepts
    // changes to DisplayName and Description only and DisplayName already has a dedicated verb.

    public void GetProperty(string path, Collection<string>? providerSpecificPickList)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        (string Name, object? Value)[] available =
        [
            ("Description", folder.Description),
            ("DisplayName", folder.DisplayName),
        ];

        var result = new PSObject();
        bool any = false;
        if (providerSpecificPickList is null || providerSpecificPickList.Count == 0)
        {
            foreach (var (name, value) in available) { result.Properties.Add(new PSNoteProperty(name, value)); any = true; }
        }
        else
        {
            foreach (var requested in providerSpecificPickList)
            {
                if (string.IsNullOrEmpty(requested)) continue;
                var match = available.FirstOrDefault(p => string.Equals(p.Name, requested, StringComparison.OrdinalIgnoreCase));
                if (match.Name is not null)
                {
                    result.Properties.Add(new PSNoteProperty(match.Name, match.Value));
                    any = true;
                }
                else
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"A folder has no '{requested}' property. Available: Description, DisplayName."), "PropertyNotFound", ErrorCategory.InvalidArgument, requested));
                }
            }
        }

        if (any) WritePropertyObject(result, path);
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        if (propertyValue is null) return;

        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        string? newDescription = null;
        bool found = false;
        foreach (PSMemberInfo property in propertyValue.Properties)
        {
            if (string.Equals(property.Name, "Description", StringComparison.OrdinalIgnoreCase))
            {
                newDescription = property.Value as string ?? property.Value?.ToString() ?? string.Empty;
                found = true;
            }
            else if (string.Equals(property.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                WriteError(new ErrorRecord(new OrchException(path, "A folder's DisplayName is changed with Rename-Item, not Set-ItemProperty."), "PropertyNotSettable", ErrorCategory.InvalidArgument, property.Name));
            }
            else
            {
                WriteError(new ErrorRecord(new OrchException(path, $"A folder's '{property.Name}' property cannot be set. Only Description is settable."), "PropertyNotSettable", ErrorCategory.InvalidArgument, property.Name));
            }
        }

        if (!found) return;

        if (string.Equals(folder.FolderType, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new OrchException(path, "A personal workspace folder's Description cannot be set — Orchestrator does not allow editing a personal workspace through the folder API."),
                "PersonalWorkspaceNotEditable", ErrorCategory.InvalidOperation, path));
            return;
        }

        if (ShouldProcess(path, "Set Description"))
        {
            try
            {
                drive.OrchAPISession.EditFolder(folder, folder.DisplayName!, newDescription ?? string.Empty);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Description", newDescription));
                WritePropertyObject(result, path);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "SetPropertyError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    public void ClearProperty(string path, Collection<string> propertyToClear)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        // Default (no pick list) clears Description. Any other named property is rejected.
        bool clearDescription = providerSpecificPickListIsEmptyOrHasDescription(propertyToClear);
        if (propertyToClear is not null)
        {
            foreach (var p in propertyToClear)
            {
                if (!string.IsNullOrEmpty(p) && !string.Equals(p, "Description", StringComparison.OrdinalIgnoreCase))
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"A folder's '{p}' property cannot be cleared. Only Description is clearable."), "PropertyNotClearable", ErrorCategory.InvalidArgument, p));
                }
            }
        }

        if (!clearDescription) return;

        if (string.Equals(folder.FolderType, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new OrchException(path, "A personal workspace folder's Description cannot be cleared — Orchestrator does not allow editing a personal workspace through the folder API."),
                "PersonalWorkspaceNotEditable", ErrorCategory.InvalidOperation, path));
            return;
        }

        if (ShouldProcess(path, "Clear Description"))
        {
            try
            {
                drive.OrchAPISession.EditFolder(folder, folder.DisplayName!, string.Empty);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Description", string.Empty));
                WritePropertyObject(result, path);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "ClearPropertyError", ErrorCategory.InvalidOperation, path));
            }
        }

        static bool providerSpecificPickListIsEmptyOrHasDescription(Collection<string>? list) =>
            list is null || list.Count == 0 ||
            list.Any(p => string.Equals(p, "Description", StringComparison.OrdinalIgnoreCase));
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string>? providerSpecificPickList) => null;

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue) => null;

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear) => null;

    #endregion
}
