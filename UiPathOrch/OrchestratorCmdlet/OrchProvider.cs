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
        string configFilePath = GetConfigFilePath();
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            try
            {
                _config = JsonSerializer.Deserialize<UiPathOrchConfig>(json, JsonTools.jsonAllowComments) ?? throw new Exception("Deserialization resulted in a null object.");
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

    // Contract: report whether the path is *syntactically* well-formed for this
    // provider. This is NOT an existence check (that is ItemExists' job) — it
    // only rejects input that could never name a folder. Mirrors the built-in
    // providers: FileSystemProvider rejects null/empty plus invalid filename
    // chars; RegistryProvider/SessionStateProviderBase reject null/empty.
    //
    // Two callers reach this override:
    //  (1) The engine, ONLY via the cmdlet `Test-Path -IsValid <path>`. Plain
    //      Test-Path (no -IsValid), Get-Item, Get-ChildItem, Set-Location,
    //      Resolve-Path, Remove-Item, etc. never call it — they go through
    //      ItemExists / GetItem instead. The chain is:
    //        Test-Path -IsValid            (TestPathCommand: if (IsValid) ...)
    //          -> SessionState.Path.IsValid(path, context)        (PathIntrinsics)
    //            -> SessionStateInternal.IsValidPath(path, context)
    //              -> ItemCmdletProvider.IsValidPath(path, context)  (internal wrapper)
    //                -> this override
    //  (2) Our own NormalizeRelativePath, as an input guard — mirroring
    //      FileSystemProvider, which is the one built-in provider that calls its
    //      own IsValidPath internally. That path-resolution chain (Resolve-Path,
    //      relative navigation, tab completion) is what exercises this method in
    //      normal use; the -IsValid switch alone would leave it nearly dormant.
    // Shared with the DU/TM shadow providers via PathTools so the syntactic rule lives in one place.
    protected override bool IsValidPath(string path) => PathTools.IsValidProviderPath(path);

    // Probably implementation complete
    // The ItemExists method does not seem to need to handle wildcards.
    protected override bool ItemExists(string path)
    {
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

                // A folder with the same name as a personal workspace may exist; suppress
                // duplicate names on BOTH paths so `dir` output and an exported CSV stay
                // consistent (duplicate CSV rows would also collide on Import-Csv | New-Item).
                if (!dupCheck.Add(psPathEscaped))
                {
                    WriteWarning($"The folder name '{folder.GetPSPath()}' (Id = {folder.Id}) is duplicated. This folder won't be listed.");
                    continue;
                }

                if (string.IsNullOrEmpty(parameters?.ExportCsv))
                {
                    WriteItemObject(folder, psPathEscaped, true);
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
            // csvOutput stays null when nothing matched (e.g. an empty folder). Export a
            // header-only CSV in that case instead of NRE-ing inside ExportCsvFile's Where().
            string? csvPath = ExportCsvFile(drive, parameters.ExportCsv, parameters.CsvEncoding,
                csvOutput ?? Enumerable.Empty<Folder>());
            if (csvPath is not null)
            {
                WriteWarning($"CSV has been exported as '{csvPath}'.");
            }
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
        OrchDriveInfo? drive = GetOrchDriveInfo(path);
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

    #endregion

    #region NavigationCmdletProvider overrides

    protected override bool IsItemContainer(string path)
    {
        // This provider only returns folders (no file-equivalent items)
        return true;
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        // FileSystemProvider invokes its own IsValidPath here as an input guard
        // (see FileSystemProvider.NormalizeRelativePath: the only place the engine
        // calls IsValidPath internally — every other use is the user typing
        // `Test-Path -IsValid`). We mirror that so IsValidPath is actually
        // exercised on the normal path-resolution chain (Resolve-Path, relative
        // navigation, tab completion), not just that rare cmdlet switch. The base
        // NavigationCmdletProvider.NormalizeRelativePath does NOT call IsValidPath,
        // so without this the override would never fire here.
        //
        // Deviation from FileSystemProvider: an empty path is let through to the
        // base (which maps it to ""), because an empty Orchestrator path denotes
        // the drive root — a valid location — whereas FileSystemProvider rejects
        // empty outright.
        if (!string.IsNullOrEmpty(path) && !IsValidPath(path))
        {
            throw new ArgumentException(
                $"The path '{path}' is not a valid {ProviderInfo?.Name ?? "Orchestrator"} provider path.",
                nameof(path));
        }

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

    // Mirror FileSystemProvider.GetChildName. The base NavigationCmdletProvider
    // implementation trims the trailing separator but does NOT re-root a bare
    // drive, so `Split-Path Orch1:\ -Leaf` returns "Orch1:" where the FileSystem
    // provider returns the rooted "Orch1:\" (its GetChildName calls
    // EnsureDriveIsRooted for the no-separator case). We replicate that so a
    // drive-root leaf round-trips as a usable drive path. Every non-root case
    // produces the same result as the base (verified via Split-Path against the
    // FileSystem provider), so this override only corrects the drive-root edge.
    // Shared with the DU/TM shadow providers via PathTools (re-roots a bare drive for the
    // drive-root leaf case; non-root cases match the base NavigationCmdletProvider).
    protected override string GetChildName(string path) => PathTools.GetChildNameWithDriveRoot(path);

    #endregion

}
