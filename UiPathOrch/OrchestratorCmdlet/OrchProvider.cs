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
            // PSPath must be drive-qualified ("Orch1:\..."), identical to GetChildItems / GetChildNames:
            // prepend the drive name (otherwise the PSPath loses the drive, "::\Autopilot", and
            // -LiteralPath/PSPath binding from Get-Item output fails to resolve). Emit it RAW — do NOT
            // wildcard-escape — because -LiteralPath ([Alias("PSPath")]) re-applies WildcardPattern.Escape
            // on bind; pre-escaping here double-escapes a name like "Fin*ce" so `Get-Item -LiteralPath
            // $f.PSPath` would fail to resolve back to the same folder.
            string psPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(f!.FullyQualifiedName!);
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
        if (PSDriveInfo is OrchDriveInfo drive && drive.IsFolderCatalogPopulated && !string.IsNullOrEmpty(result))
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

    // Keep the drive root's separator on a top-level item's parent ("Orch1:" -> "Orch1:\") so
    // PSParentPath (and the Folder view's "Directory:" group header) and Split-Path -Parent match
    // FileSystemProvider. Symmetric to the GetChildName re-rooting above. See PathTools.
    protected override string GetParentPath(string path, string root) => PathTools.ParentPathWithDriveRoot(base.GetParentPath(path, root));

    #endregion

}
