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
public partial class OrchProvider : NavigationCmdletProvider
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
        // Only output Scope warnings when no password is set
        if (string.IsNullOrEmpty(drive.Password))
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
                duDrive!._parentDrive = (OrchDriveInfo)drive;
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

        string orchPath = OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(path));
        Folder f = drive.GetFolder(orchPath);
        if (f is not null)
        {
            string psPath = OrchDriveInfo.OrchProviderPathToPSPath(f!.FullyQualifiedName!);
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

    //protected override void SetItem(string path, object value)
    //{
    //    if (ShouldProcess(path, "Set Folder"))
    //    {
    //        try
    //        {
    //            if (DynamicParameters is RuntimeDefinedParameterDictionary parameters)
    //            {
    //                string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
    //                Folder? folder = OrchDriveInfo?.GetFolder(orchPath);
    //                if (folder is null)
    //                    return;

    //                string name = parameters["Name"].Value as string;
    //                string description = parameters["Description"].Value as string;
    //                if (folder.DisplayName == name && folder.Description == description)
    //                    return;

    //                if (name is null)
    //                    name = folder.DisplayName;
    //                if (description is null)
    //                    description = folder.Description;

    //                OrchDriveInfo!.OrchAPI.EditFolder(folder, name!, description!);
    //                OrchDriveInfo._folderListCache = null;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            var errorRecord = new ErrorRecord(ex, "SetFolderError", ErrorCategory.InvalidOperation, null);
    //            WriteError(errorRecord);
    //        }
    //    }
    //}

    //public class FolderCompleter : OrchArgumentCompleter
    //{
    //    public override IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters)
    //    {
    //        //if (OrchProvider._this is null)
    //        //    yield break;

    //        if (!fakeBoundParameters.Contains("Path"))
    //            yield break;

    //        string path = fakeBoundParameters["Path"]?.ToString();
    //        if (string.IsNullOrEmpty(path))
    //            yield break;

    //        string parentPath = "\\" + OrchProvider._this.OrchDriveInfo!.CurrentLocation;
    //        string targetPath = OrchProvider._this.MakePath(parentPath, path);
    //        string normalizedPath = OrchProvider._this.NormalizeRelativePath(targetPath, "\\");

    //        if (normalizedPath.StartsWith(OrchProvider._this.OrchDriveInfo.NameColon))
    //        {
    //            normalizedPath = normalizedPath.Substring(OrchProvider._this.OrchDriveInfo.Name.Length + 1);
    //        }

    //        string orchPath = OrchDriveInfo.PSPathToOrchPath(normalizedPath);

    //        Folder? folder = OrchProvider._this.OrchDriveInfo?.GetFolder(orchPath);
    //        if (folder is null || folder.Id == 0)
    //            yield break;

    //        switch (parameterName)
    //        {
    //            case "Name":
    //                yield return new CompletionResult(PathTools.EscapePSText(folder.DisplayName!));
    //                break;
    //            case "Description":
    //                if (!string.IsNullOrEmpty(folder.Description))
    //                    yield return new CompletionResult(PathTools.EscapePSText(folder.Description));
    //                else
    //                    yield break;
    //                break;
    //            default:
    //                yield break;
    //        }
    //    }
    //}

    // TODO: Implementation to update the folder's Description. Want to move this to SetItemProperty. Commented out for now.
    //protected override object SetItemDynamicParameters(string path, object value)
    //{
    //_this = this;

    //var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();

    //#region create Name parameter
    //var attrName = new Collection<Attribute>
    //{
    //    new ParameterAttribute { HelpMessage = "New Name." },
    //    new ArgumentCompleterAttribute(typeof(FolderCompleter))
    //};
    //runtimeDefinedParameterDictionary.Add("Name", new RuntimeDefinedParameter("Name", typeof(string), attrName));
    //#endregion

    //#region create Description parameter
    //var attrDescription = new Collection<Attribute>
    //{
    //    new ParameterAttribute { HelpMessage = "Description." },
    //    new ArgumentCompleterAttribute(typeof(FolderCompleter))
    //};
    //runtimeDefinedParameterDictionary.Add("Description", new RuntimeDefinedParameter("Description", typeof(string), attrDescription));
    //#endregion

    //return runtimeDefinedParameterDictionary;
    //}

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

    private string? ExportCsvFile(string exportCsv, Encoding? csvEncoding, IEnumerable<Folder> output)
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
                OrchestratorPSCmdlet.EscapeCsvValue(folder.Path),
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
            catch {} // Swallow - don't block navigation for a warning
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

        try
        {
            if (orchPath == "")
            {
                foreach (var folder in drive.GetFolders())
                {
                    if (Stopping) return;
                    uint folderDepth = FolderDepth(folder.FullyQualifiedName!);

                    if (folderDepth - (currentDepth + 1) <= depth)
                    {
                        string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                        string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);
                        //string psPathEscaped = drive.NameColon + WildcardPattern.Escape(psPath);
                        //string psPathEscaped = PathTools.EscapePSText2(psPath);

                        if (string.IsNullOrEmpty(parameters?.ExportCsv))
                        {
                            // A folder with the same name as a personal workspace may exist
                            // To ensure auto-completion works properly, avoid outputting multiple folders with the same name
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
            }
            else
            {
                string orchPathStart = orchPath + "/";
                //string orchPathEnd   = orchPath + "/\uffff";// the max value of Unicode

                foreach (var folder in drive!.GetFolders())
                {
                    if (Stopping) return;
                    if (!folder.FullyQualifiedName!.StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                        continue;

                    uint folderDepth = FolderDepth(folder.FullyQualifiedName!);

                    if (folderDepth - (currentDepth + 1) <= depth)
                    {
                        string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                        string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);
                        //string psPathEscaped = PathTools.EscapePSText2(psPath);

                        // It is possible to create a folder with the same name as a personal workspace
                        // To ensure auto-completion works properly, avoid outputting multiple folders with the same name
                        if (string.IsNullOrEmpty(parameters?.ExportCsv))
                        {
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
            }
        }
        catch (Exception ex)
        {
            var errorRecord = new ErrorRecord(new OrchException(path, ex), "GetChildItemsError", ErrorCategory.InvalidOperation, path);
            WriteError(errorRecord);
        }

        if (!string.IsNullOrEmpty(parameters?.ExportCsv))
        {
            string csvPath = ExportCsvFile(parameters.ExportCsv, parameters.CsvEncoding, csvOutput!);
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

    // GetChildNames must call WriteItemObject with only the name string value, not the object.
    // This method is called when Get-ChildItem -Name is executed.
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
            foreach (var folder in drive.GetFolders().Where(f => !f.ParentId.HasValue))// || f.FolderType == "Personal"))
            {
                if (Stopping) return;
                string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.DisplayName!);
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
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return false;
        }
        return true;

        //if (path == drive.NameColonSeparator)
        //    return true;

        //string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        //string orchPathStart = orchPath + "/";

        ////bool ret = OrchDriveInfo!.FoldersCache.GetViewBetween(start, end).Any();
        //bool ret = drive.GetFolders().Any(f => f.FullyQualifiedName!.StartsWith(orchPathStart));
        //return ret;
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
                    WriteError(new ErrorRecord(new OrchException($"{path} ({dynamicParameters.FeedType})", "FeedType must be 'Processes' or 'FolderHierarchy'."), "NewFolderError", ErrorCategory.InvalidOperation, path));
                    //dynamicParameters.FeedType = null;
                }

                Folder f = drive.OrchAPISession.CreateFolder(displayName,
                    dynamicParameters?.Description,
                    (parentPathId is null || parentPathId == 0) ? dynamicParameters?.FeedType : "Processes",
                    parentPathId);
                if (f is not null)
                {
                    if (parentPath == drive.NameColon) parentPath = drive.NameColonSeparator;
                    f.Path = parentPath;
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

    // TODO: Changing Description via Rename-Item is unnatural, so removing this.
    //protected override object RenameItemDynamicParameters(string path, string newName)
    //{
    //    _this = this;

    //    #region create Description parameter
    //    var attrDescription = new Collection<Attribute>
    //    {
    //        new ParameterAttribute { HelpMessage = "Description." },
    //        new ArgumentCompleterAttribute(typeof(FolderCompleter))
    //    };

    //    var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary
    //    {
    //        { "Description", new RuntimeDefinedParameter("Description", typeof(string), attrDescription) }
    //    };
    //    #endregion

    //    return runtimeDefinedParameterDictionary;
    //}

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

                // Not ideal, but to match the GetFolders() implementation, clear _dicFolders when a personal workspace is deleted too..
                // This ensures GetFolders() works correctly. Want to fix this eventually
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
        if (retNew.EndsWith(System.IO.Path.DirectorySeparatorChar) && retNew.Length > 1 && retNew[retNew.Length-2] != ':')
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

        if (ShouldProcess(path, "Move Folder"))
        {
            Folder srcFolder = null;
            try
            {
                string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
                srcFolder = drive.GetFolder(ocPath);

                string ocDestination = OrchDriveInfo.PSPathToOrchPath(destination);
                Int64? dstId = drive.GetFolder(ocDestination)!.Id ?? null;

                drive.OrchAPISession.MoveFolder(srcFolder?.Id ?? 0, dstId);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;
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

    #endregion
}
