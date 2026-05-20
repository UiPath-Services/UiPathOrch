//#undef DEBUG

using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchTm", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
public class OrchTmProvider : NavigationCmdletProvider
{
    protected OrchTmDriveInfo OrchTmDriveInfo => (OrchTmDriveInfo)this.PSDriveInfo;

    protected OrchDriveInfo OrchDriveInfo => ((OrchTmDriveInfo)this.PSDriveInfo).ParentDrive;
    //protected OrchDriveInfo? GetOrchDriveInfo(string path)

    TmProject? GetProject(string path)
    {
        var psPath = Path.GetFileName(path);
        if (psPath == "") return null;

        var projects = OrchTmDriveInfo.GetTmProjects();
        return projects?.FirstOrDefault(p => string.Compare(p.projectPrefix, psPath, StringComparison.OrdinalIgnoreCase) == 0);
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
                    if (drive.Scope?.Contains("TM.") ?? false)
                    {
                        if (drive.Root is null) continue;
                        string root = drive.Root.TrimEnd('/') + "/testmanager_";

                        var tmProvider = SessionState.Provider.GetOne("UiPathOrchTm");

                        var tmDrive = new OrchTmDriveInfo(tmProvider, drive.Name + "Tm", drive?.Description ?? "", root);
                        SessionState.Drive.New(tmDrive, scope: "Global");
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
        OrchTmDriveInfo tmDrive = (OrchTmDriveInfo)drive;

        // Depending on the provider class loading order, the UiPathOrch provider may not be registered yet at this point.
        // By performing the following in every provider's NewDrive, we ensure UiPathOrch and UiPathOrchTm are reliably associated.
        try
        {
            //var orchProvider = SessionState.Provider.GetOne("UiPathOrch");
            // If no exception is thrown, orchProvider is guaranteed to be not null
            var orchDrive = SessionState.Drive.Get(drive.Name.Substring(0, drive.Name.Length - 2)) as OrchDriveInfo;
            tmDrive.ParentDrive = orchDrive!;
        }
        catch { }

        return drive;
    }

    #endregion DriveCmdletProvider overrides

    #region ItemCmdletProvider overrides

    protected override void GetItem(string path)
    {
        var psPath = Path.GetFileName(path);
        if (psPath == "") return;

        var project = GetProject(path);
        if (project is not null)
        {
            WriteItemObject(project, path, true);
        }
    }

    protected override void InvokeDefaultAction(string path)
    {
        var drives = SessionState.EnumTmDrives([path]);
        if (drives is null)
        {
            return;
        }

        foreach (var drive in drives)
        {
            string endpoint = drive.OrchAPISession._base_url + "/testmanager_/";

            var project = GetProject(path);
            if (project is not null) { endpoint += $"{project.projectPrefix}/dashboard"; }

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
        var psPath = Path.GetFileName(path);
        if (psPath == "") return true;

        var projects = OrchTmDriveInfo.GetTmProjects();
        return projects?.Any(p => string.Compare(p.projectPrefix, psPath, StringComparison.OrdinalIgnoreCase) == 0) ?? false;
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
        var projects = OrchTmDriveInfo.GetTmProjects();
        foreach (var project in projects!.OrderBy(p => p.projectPrefix))
        {
            if (Stopping) return;
            string psPathEscaped = OrchTmDriveInfo.NameColonSeparator + project.projectPrefix;
            //string psPathEscaped = PathTools.EscapePSText2(psPath);
            WriteItemObject(project, psPathEscaped, true);
        }
    }

    // GetChildNames must call WriteItemObject with just the name string, not the object.
    // This method is invoked when running Get-ChildItem -Name and wildcard resolution (cd t*, rmdir *).
    // The first argument (name string) is matched against the wildcard pattern by LocationGlobber.
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        var projects = OrchTmDriveInfo.GetTmProjects();
        foreach (var project in projects!.OrderBy(p => p.projectPrefix))
        {
            if (Stopping) return;
            string fullPath = OrchTmDriveInfo.NameColonSeparator + project.projectPrefix;
            WriteItemObject(project.projectPrefix, fullPath, true);
        }
    }

    protected override bool HasChildItems(string path)
    {
        if (path == OrchTmDriveInfo.NameColonSeparator)
            return true;
        return false;
    }

    protected override void RenameItem(string path, string newName)
    {
        var drives = SessionState.EnumTmDrives([path]);
        if (drives is null)
        {
            return;
        }

        foreach (var drive in drives)
        {
            var project = GetProject(path);
            if (project is null) continue;

            if (ShouldProcess(path, "Rename Project"))
            {
                try
                {
                    var postingProject = OrchCollectionExtensions.DeepCopy(project);
                    // postingProject.Path = null; // Not needed because it has the JsonIgnore attribute
                    postingProject.projectPrefix = null;
                    postingProject.name = newName;

                    drive.OrchAPISession.PutTmProject(postingProject);
                    //drive.TmProjects.ClearCache();
                    drive.TmProjects.ClearCache();
                    //drive.ClearFolderCache(folder);
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameTmProjectError", ErrorCategory.InvalidOperation, project);
                    WriteError(errorRecord);
                    //drive.TmProjects.ClearCache();
                    drive.TmProjects.ClearCache();
                }
            }
        }
    }

    protected override void RemoveItem(string path, bool recurse)
    {
        var drives = SessionState.EnumTmDrives([path]);
        if (drives is null)
        {
            return;
        }

        foreach (var drive in drives)
        {
            var project = GetProject(path);
            if (project is null) continue;

            if (ShouldProcess(path, "Remove Project"))
            {
                try
                {
                    drive.OrchAPISession.RemoveTmProject(project.id!);
                    drive.TmProjects.ClearCache();
                    //drive.TmProjects.ClearCache();
                    //drive.ClearFolderCache(folder);
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(path, ex), "RemoveTmProjectError", ErrorCategory.InvalidOperation, project);
                    WriteError(errorRecord);
                    drive.TmProjects.ClearCache();
                    //drive.TmProjects.ClearCache();
                }
            }
        }
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
        if (result.StartsWith(Path.DirectorySeparatorChar) && result.Length > 1)
            result = result[1..];

        // Canonicalize project name casing from cache.
        var projects = PSDriveInfo is OrchTmDriveInfo drive ? drive.TmProjects.CachedValue : null;
        if (projects != null && !string.IsNullOrEmpty(result))
        {
            var project = projects.FirstOrDefault(p =>
                string.Equals(p.projectPrefix, result, StringComparison.OrdinalIgnoreCase));
            if (project != null)
                result = project.projectPrefix;
        }

        return result ?? "";
    }

    protected override string MakePath(string parent, string child)
    {
        string retNew = base.MakePath(parent, child);
        if (retNew.EndsWith(Path.DirectorySeparatorChar) && retNew.Length > 1 && retNew[retNew.Length - 2] != ':')
        {
            retNew = retNew.Substring(0, retNew.Length - 1);
        }
        return retNew;
    }

    #endregion

}
