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
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using UiPath.PowerShell.Positional;

// インストール方法
// 1. PowerShell 7 をインストール。PowerShell-7.x.x-win-x64.msi をダウンロード。PowerShell 5 と side by side でインストールできる。
// https://github.com/PowerShell/PowerShell/releases/
// 2. Install-Package UiPathOrch を実行。
// 3. Import-Package UiPathOrch を実行。画面の指示に従い、設定ファイルを記載。Orchestrator の管理画面で、外部アプリの ID を払い出す必要がある。
//
// もし、PSReadLine が無効化されている旨の警告が表示された場合は、Tab もしくは Ctrl+Space による補完が動作しない。
// Import-Module PSReadLine を実行すると、補完が動作するようになる。この恒久的な対処方法は、下記にある。
// https://iwasi.hatenablog.jp/entry/2020/12/13/161312
//
// pwsh.exe を起動時に自動で Import-Module UiPathOrch を実行するには、このコマンド行を pwshl.exe のプロファイルに追加する。
// プロファイルのパスは、pwsh コンソールで $profile と入力すると確認できる。

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
    // パラメータセット1: AppId + AppSecret
    private const string AppAuthParamSet = "AppAuth";

    // パラメータセット2: Username + Password
    private const string UserAuthParamSet = "UserAuth";

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
    public string? OAuthScope { get; set; }

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

    #region CmdletProvider overrides

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        //System.Diagnostics.Debugger.Launch();
        ProviderInfo ret = base.Start(providerInfo);
        OrchDriveInfo.SessionState = base.SessionState;
        return ret;
    }

    // 可能な場合には、必ず OrchDriveInfo よりも GetOrchDriveInfo() を使う必要がある。
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
            throw new Exception("something wrong.");
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
        else // Unix 系 (Linux / macOS)
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

            SaveResourceToFile($"OrchProvider.Resources.{lang}.UiPathOrchConfig.json", configFilePath);
        }
    }

    private void WarningPSDriveConfig(PSDrive drive)
    {
        // Scope に関する警告は、パスワードが設定されていない場合に限り出力する
        if (string.IsNullOrEmpty(drive.Password))
        {
            if (string.IsNullOrWhiteSpace(drive.Scope))
            {
                WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Scope is not specified!");
            }
            else
            {
                string lowerScope = drive.Scope?.ToLower() ?? "";

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

        if (string.IsNullOrWhiteSpace(drive.Root))
        {
            WriteWarning($"\"{drive.Name}:{System.IO.Path.DirectorySeparatorChar}\": Root is not specified!");
        }
        else if ((drive.Root.EndsWith("/orchestrator_/") || drive.Root.EndsWith("/orchestrator_")))
        {
            WriteWarning($"\"{drive.Name}\": The \"Root\" value in UiPathOrchConfig.json should not contain '/orchestrator_/'. Run the Edit-OrchConfig cmdlet to open the file and update it manually.");
        }

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

        // Username の指定がなく、AppSecret の指定もない場合には、RedirectUrl が指定されてないと。
        if (string.IsNullOrWhiteSpace(drive.Username) && string.IsNullOrWhiteSpace(drive.AppSecret) && string.IsNullOrWhiteSpace(drive.RedirectUrl))
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

                WriteWarning($"Please edit the '{configFilePath}'. Once edited, launch a new PS session and `Import-Module UiPathOrch` to mount your Orchestrator tenants as PSDrives.");

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
            return ret;
        }
        else
        {
            #region 環境変数 UIPATHORCH_SUPPRESS_CONFIG_CREATION が 1 であれば、設定ファイルを作成しない
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

                WriteWarning($"Please edit '{configFilePath}'. After saving your changes, restart the PowerShell session and run `Import-Module UiPathOrch` to mount your Orchestrator tenants as PSDrives.");
            }
            else
            {
                // Linux 環境ではエディタをうまく起動できない。。
                // ディレクトリを移動して、編集を促すメッセージを出力する。元のディレクトリに戻るには popd を実行する。
                string folder = System.IO.Path.GetDirectoryName(configFilePath);
                string fileName = System.IO.Path.GetFileName(configFilePath);

                // 現在のロケーションをデフォルトスタックにプッシュ
                // したいけど、ここではまだちゃんと動かないっぽい。。
                //SessionState.Path.PushCurrentLocation("default");

                // 設定ファイルがあるパスに移動
                SessionState.Path.SetLocation(folder);

                WriteWarning($"Please edit './{fileName}'. After saving your changes, restart the PowerShell session and run Import-Module UiPathOrch to mount your Orchestrator tenants as PSDrives.");
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
        // drive が OrchDriveInfo であれば、InitializeDefaultDrives() が実行されている (New-PSDrive cmdlet の実行ではない)
        if (drive is OrchDriveInfo orchDrive)
        {
            // プロバイダクラスのロード順によっては、このタイミングではまだ UiPathOrchTm プロバイダは未登録。
            // 下記の処理を、すべてのプロバイダの NewDrive で行うことで、UiPathOrch と UiPathOrchTm を確実に関連づけるようにする。
            #region Find and associate Du drives
            try
            {
                // var duProvider = SessionState.Provider.GetOne("UiPathOrchDu");
                // 例外が出なければ、tmProvider is not null になっている
                var duDrive = SessionState.Drive.Get(drive.Name + "Du") as OrchDuDriveInfo;
                duDrive!._parentDrive = (OrchDriveInfo)drive;
            }
            catch { } // ここでうまくいかない場合には、OrchDuDriveInfo.NewDrive が処理するはず
            #endregion


            // プロバイダクラスのロード順によっては、このタイミングではまだ UiPathOrchTm プロバイダは未登録。
            // 下記の処理を、すべてのプロバイダの NewDrive で行うことで、UiPathOrch と UiPathOrchTm を確実に関連づけるようにする。
            #region Find and associate Tm drives
            try
            {
                // var tmProvider = SessionState.Provider.GetOne("UiPathOrchTm");
                // 例外が出なければ、tmProvider is not null になっている
                var tmDrive = SessionState.Drive.Get(drive.Name + "Tm") as OrchTmDriveInfo;
                tmDrive!._parentDrive = (OrchDriveInfo)drive;
            }
            catch { } // ここでうまくいかない場合には、OrchTmDriveInfo.NewDrive が処理するはず
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

        // drive が PSDriveInfo であれば、InitializeDefaultDrives() ではなく New-PSDrive -PSProvider UiPathOrch が実行されている
        var parameters = DynamicParameters as NewDrive_Parameters;
        PSDrive psDrive = new()
        {
            Name = drive.Name, // mandatory なのでかならず -Name で渡されてくる
            Root = drive.Root, // mandatory なのでかならず -Root で渡されてくる
            Description = drive.Description,
            IdentityUrl = parameters?.IdentityUrl,
            AppId = parameters?.AppId,
            AppSecret = parameters?.AppSecret,
            RedirectUrl = parameters?.RedirectUrl,
            HttpListener = parameters?.HttpListener,
            Scope = parameters?.OAuthScope,
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

    // TODO: これ何か実装した方が良いのでは？ でも呼ばれたことがないような気がする。。
    protected override bool IsValidPath(string path)
    {
        //Tools.DebugFuncEntry("IsValidPath", path, SessionState);
        //Tools.DebugFuncExit("IsValidPath", true, SessionState);
        return true;
    }

    // たぶん実装完了
    // ItemExists メソッドは、ワイルドカードを処理する必要はないっぽい。
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

    // TODO: フォルダの Description を更新する実装。SetItemProperty に移動したい。一旦コメントアウト。
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

    // フォルダの深さ (含むスラッシュの数+1) を返す
    // "" の深さ: 0
    // "folder" の深さ: 1
    // "folder/sub" の深さ: 2
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

        // 各フォルダに対してデータ行を書き込む
        foreach (var folder in output.Where(f => f.FolderType != "Personal"))
        {
            string[] line = [
                OrchestratorPSCmdlet.EscapeCsvValue(folder.Path),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.DisplayName!),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.Description),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.FeedType)
            ];
            OrchestratorPSCmdlet.WriteCsvLine(writer, line);
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

        var parameters = DynamicParameters as GetChildItems_Parameters;
        if (parameters is not null && parameters.Reload.IsPresent)
        {
            drive._dicFolders = null;
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
                    uint folderDepth = FolderDepth(folder.FullyQualifiedName!);

                    if (folderDepth - (currentDepth + 1) <= depth)
                    {
                        string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                        string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);
                        //string psPathEscaped = drive.NameColon + WildcardPattern.Escape(psPath);
                        //string psPathEscaped = PathTools.EscapePSText2(psPath);

                        if (string.IsNullOrEmpty(parameters?.ExportCsv))
                        {
                            // 個人用ワークスペースと同名のフォルダが存在する可能性がある
                            // 自動補完が適切に動作できるように、同名のフォルダを複数出力することは避ける
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
                    if (!folder.FullyQualifiedName!.StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                        continue;

                    uint folderDepth = FolderDepth(folder.FullyQualifiedName!);

                    if (folderDepth - (currentDepth + 1) <= depth)
                    {
                        string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                        string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);
                        //string psPathEscaped = PathTools.EscapePSText2(psPath);

                        // 個人用ワークスペースと同名のフォルダを作成することができてしまう
                        // 自動補完が適切に動作できるように、同名のフォルダを複数出力することは避ける
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

    // GetChildnames は、オブジェクトでなく名前の string 値のみを WriteItemObject する必要がある。
    // このメソッドは、Get-ChildItem -Name を実行すると呼び出される。
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
                WriteItemObject(folder.DisplayName!, folder.DisplayName!, true); // ★★★
            }
        }
        else
        {
            Folder parentFolder = drive.GetFolder(ocPath);
            Int64 parentFolderId = parentFolder?.Id ?? 0;

            foreach (var folder in drive.GetFolders().Where(f => f.ParentId == parentFolderId))
            {
                WriteItemObject(folder.DisplayName!, folder.DisplayName!, true);
            }
        }
    }

    // 必ず true を返した方が、操作誤りで rmdir を実行しちゃう事故が減るような気がする。
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
                // TODO: Unescape() は必要か？？ ★★★★★★
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
                //}
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameFolderError", ErrorCategory.InvalidOperation, path);
                WriteError(errorRecord);
            }
        }
    }

    // TODO: Rename-Item で Description を変更するのは不自然なのでやめる。
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
                // 個人ワークスペースフォルダの場合には、所有者のワークスペースを無効に設定する
                // でないと、削除したワークスペースフォルダが自動ですぐに復活してしまう
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

                // いまいちなんだけど、GetFolders() の実装に合わせて、個人ワークスペースを削除した場合も _dicFolders をクリアしておく。。
                // これにより GetFolders() が正しく動作する。いつか直したい
                drive._dicFolders = null;

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
        // このプロバイダは、フォルダしか返さない（ファイルに相当するアイテムは返さない）
        return true;
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
