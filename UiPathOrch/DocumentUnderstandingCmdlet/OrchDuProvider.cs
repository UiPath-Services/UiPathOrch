//#undef DEBUG

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchDu", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
public class OrchDuProvider : NavigationCmdletProvider
{
    protected OrchDriveInfo OrchDriveInfo => ((OrchDuDriveInfo)this.PSDriveInfo).ParentDrive;
    protected OrchDuDriveInfo OrchDuDriveInfo => (OrchDuDriveInfo)this.PSDriveInfo;

    protected OrchDuDriveInfo? GetOrchDuDriveInfo(string path)
    {
        if (OrchDriveInfo is not null)
        {
            return OrchDuDriveInfo;
        }
        string driveName = OrchDriveInfo.ExtractDriveName(path);
        return SessionState.Drive.Get(driveName) as OrchDuDriveInfo;
    }

    DuProject? GetProject(string path)
    {
        var psPath = System.IO.Path.GetFileName(path);
        if (psPath == "") return null;

        var projects = OrchDuDriveInfo.GetDuProjects();
        return projects?.FirstOrDefault(p => string.Compare(p.name, psPath, StringComparison.OrdinalIgnoreCase) == 0);
    }

    #region DriveCmdletProvider overrides

    protected override Collection<PSDriveInfo>? InitializeDefaultDrives()
    {
        string configFilePath = OrchProvider.GetConfigFilePath();
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            UiPathOrchConfig config;
            try
            {
                config = JsonSerializer.Deserialize<UiPathOrchConfig>(json, JsonTools.jsonAllowComments);
                if (config is null) return null;
            }
            catch
            {
                // Invalid config file cases are handled by OrchProvider
                return null;
            }

            Collection<PSDriveInfo> ret = base.InitializeDefaultDrives();
            foreach (var drive in config!.PSDrives!)
            {
                if (drive.Enabled is null || drive.Enabled.GetValueOrDefault())
                {
                    if (drive.Scope?.Contains("Du.") ?? false)
                    {
                        if (drive.Root is null) continue;
                        string root = drive.Root.TrimEnd('/') + "/du_";

                        var duProvider = SessionState.Provider.GetOne("UiPathOrchDu");

                        var duDrive = new OrchDuDriveInfo(duProvider, drive.Name + "Du", drive?.Description ?? "", root);
                        SessionState.Drive.New(duDrive, scope: "Global");
                    }
                }
            }
            return ret;
        }

        // Missing config file cases are handled by OrchProvider
        return null;
    }

    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        OrchDuDriveInfo duDrive = (OrchDuDriveInfo)drive;

        // Depending on the provider class loading order, the UiPathOrch provider may not be registered yet at this point.
        // By performing the following in every provider's NewDrive, we ensure UiPathOrch and UiPathOrchDu are reliably associated.
        try
        {
            // var orchProvider = SessionState.Provider.GetOne("UiPathOrch");
            // If no exception is thrown, orchProvider is guaranteed to be not null
            var orchDrive = SessionState.Drive.Get(drive.Name.Substring(0, drive.Name.Length - 2)) as OrchDriveInfo;
            duDrive._parentDrive = orchDrive;
        }
        catch { }

        return drive;
    }

    #endregion DriveCmdletProvider overrides

    #region ItemCmdletProvider overrides

    protected override void GetItem(string path)
    {
        var project = GetProject(path);
        if (project is not null)
        {
            WriteItemObject(project, path, true);
        }
        // Root path or non-existent path: output nothing (no error)
    }

    protected override void InvokeDefaultAction(string path)
    {
        var drives = SessionState.EnumDuDrives([path]);
        if (drives is null)
        {
            return;
        }

        foreach (var drive in drives)
        {
            string endpoint = drive.OrchAPISession._base_url + "/du_/projects/";

            var project = GetProject(path);
            if (project is not null) { endpoint += $"{project.id}/details"; }

            Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
        }
    }

    // TODO: Should we implement something here? But it seems like this has never been called..
    protected override bool IsValidPath(string path)
    {
        return true;
    }

    // The ItemExists method apparently does not need to handle wildcards.
    protected override bool ItemExists(string path)
    {
        string path2 = System.IO.Path.GetFileName(path);
        if (path2 == "") return true;

        return GetProject(path) is not null;
    }

    #endregion ItemCmdletProvider overrides

    #region ContainerCmdletProvider overrides

    protected override void GetChildItems(string path, bool recurse)
    {
        GetChildItems(path, recurse, 0);
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar))
        {
            return;
        }
        var projects = OrchDuDriveInfo.GetDuProjects();
        foreach (var project in projects!)
        {
            if (Stopping) return;
            string psPathEscaped = OrchDuDriveInfo.NameColonSeparator + project.name;
            //string psPathEscaped = PathTools.EscapePSText2(psPath);
            WriteItemObject(project, psPathEscaped, true);
        }
    }

    // GetChildNames must call WriteItemObject with just the name string, not the object.
    // This method is invoked when running Get-ChildItem -Name and wildcard resolution (cd t*, rmdir *).
    // The first argument (name string) is matched against the wildcard pattern by LocationGlobber.
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        var projects = OrchDuDriveInfo.GetDuProjects();
        foreach (var project in projects!)
        {
            if (Stopping) return;
            string fullPath = OrchDuDriveInfo.NameColonSeparator + project.name;
            WriteItemObject(project.name, fullPath, true);
        }
    }

    protected override bool HasChildItems(string path)
    {
        if (path == OrchDuDriveInfo.NameColonSeparator)
            return true;
        return false;
    }

    public class NewItem_DynamicParameters
    {
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<DescriptionHere>))]
        public string? Description { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        //[ArgumentCompleter(typeof(StaticTextsCompleter<Processes_FolderHierarchy>))]
        public string? OcrMethod { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? OcrUrl { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? ForceApplyOcr { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Modern_Classic>))]
        public string? ProjectType { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public bool? Helix { get; set; }
    }

    protected override void NewItem(string path, string itemTypeName, object newItemValue)
    {
        if (ShouldProcess(path, "New Project"))
        {
            var drive = GetOrchDuDriveInfo(path);
            if (drive is null) return;

            var dynamicParameters = DynamicParameters as NewItem_DynamicParameters;
            dynamicParameters ??= new();
            dynamicParameters.OcrMethod ??= "uipath";
            dynamicParameters.OcrUrl ??= ""; // TODO
            dynamicParameters.ForceApplyOcr ??= "Auto";
            dynamicParameters.ProjectType ??= "Modern";
            dynamicParameters.Helix ??= false;

            try
            {
                var projectName = System.IO.Path.GetFileName(path);
                CreateDuProjectCmd payload = new()
                {
                    name = projectName,
                    description = dynamicParameters.Description,
                    ocrMethod = dynamicParameters.OcrMethod,
                    ocrUrl = dynamicParameters.OcrUrl,
                    forceApplyOcr = dynamicParameters.ForceApplyOcr,
                    type = dynamicParameters.ProjectType,
                    helix = dynamicParameters.Helix
                };

                payload.description ??= "";
                //payload.ocrUrl = "https://staging.uipath.com/ytsuda/DefaultTenant/ocr_/ocr";
                payload.ocrUrl ??= ""; // TODO

                drive.OrchAPISession.CreateDuProjects(payload);
                drive._dicDuProjects = null;

                // Re-fetch to get the created project and output it
                var projects = drive.GetDuProjects();
                var created = projects?.FirstOrDefault(p =>
                    string.Equals(p.name, projectName, StringComparison.OrdinalIgnoreCase));
                if (created is not null)
                {
                    WriteItemObject(created, path, true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "NewProjectError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return new NewItem_DynamicParameters();
    }

    #endregion

    #region NavigationCmdletProvider overrides

    protected override bool IsItemContainer(string path)
    {
        // This provider only returns containers (it does not return items equivalent to files)
        return true;
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        string result = base.NormalizeRelativePath(path, basePath);
        if (result.StartsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1)
            result = result[1..];

        // Canonicalize project name casing from cache.
        var projects = PSDriveInfo is OrchDuDriveInfo drive ? drive._dicDuProjects : null;
        if (projects != null && !string.IsNullOrEmpty(result))
        {
            var project = projects.FirstOrDefault(p =>
                string.Equals(p.name, result, StringComparison.OrdinalIgnoreCase));
            if (project != null)
                result = project.name;
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

    #endregion

}
