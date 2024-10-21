//#undef DEBUG

using Microsoft.Management.Infrastructure.Options;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core
{
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

        #region CmdletProvider overrides

        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            OrchTmDriveInfo.SessionState = base.SessionState;
            return base.Start(providerInfo);
        }

        #endregion CmdletProvider overrides

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
                    config = JsonSerializer.Deserialize<UiPathOrchConfig>(json);
                    if (config == null) return null;
                }
                catch
                {
                    // 設定ファイルが不正なケースは、OrchProvider が処理する
                    return null;
                }

                Collection<PSDriveInfo> ret = base.InitializeDefaultDrives();
                foreach (var drive in config!.PSDrives!)
                {
                    if (drive.Enabled == null || drive.Enabled.GetValueOrDefault())
                    {
                        if ((drive.Scope?.Contains("TM.") ?? false) && (drive.Root?.Contains("uipath.com") ?? false))
                        {
                            string root = drive.Root.TrimEnd('/') + "/testmanager_";

                            var tmProvider = SessionState.Provider.GetOne("UiPathOrchTm");

                            var tmDrive = new OrchTmDriveInfo(tmProvider, drive.Name + "Tm", drive?.Description ?? "", root);
                            SessionState.Drive.New(tmDrive, scope: "Global");
                        }
                    }
                }
                return ret;
            }

            // 設定ファイルが存在しないケースは、OrchProvider が処理する
            return null;
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            OrchTmDriveInfo tmDrive = (OrchTmDriveInfo)drive;

            // プロバイダクラスのロード順によっては、このタイミングではまだ UiPathOrch プロバイダは未登録。
            // 下記の処理を、すべてのプロバイダの NewDrive で行うことで、UiPathOrch と UiPathOrchTm を確実に関連づけるようにする。
            try
            {
                //var orchProvider = SessionState.Provider.GetOne("UiPathOrch");
                // 例外が出なければ、orchProvider != null になっている
                var orchDrive = SessionState.Drive.Get(drive.Name.Substring(0, drive.Name.Length - 2)) as OrchDriveInfo;
                tmDrive._parentDrive = orchDrive;
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

            var projects = OrchTmDriveInfo.GetTmProjects();
            var project = projects?.FirstOrDefault(p => string.Compare(p.projectPrefix, psPath, StringComparison.OrdinalIgnoreCase) == 0);
            WriteItemObject(project, path, true);
        }

        protected override void InvokeDefaultAction(string path)
        {
            var drives = OrchTmDriveInfo.EnumOrchTmDrives([path]);
            if (drives == null)
            {
                return;
            }

            foreach (var drive in drives)
            {
                string endpoint = drive.OrchAPISession._base_url + "/testmanager_/";

                var project = GetProject(path);
                if (project != null) { endpoint += $"{project.projectPrefix}/dashboard"; }

                Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
            }
        }

        // TODO: これ何か実装した方が良いのでは？ でも呼ばれたことがないような気がする。。
        protected override bool IsValidPath(string path)
        {
            return true;
        }

        // ItemExists メソッドは、ワイルドカードを処理する必要はないっぽい。
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
            if (!path.EndsWith('\\'))
            {
                return;
            }
            var projects = OrchTmDriveInfo.GetTmProjects();
            foreach (var project in projects!.OrderBy(p => p.projectPrefix))
            {
                string psPathEscaped = OrchTmDriveInfo.NameColonSeparator + project.projectPrefix;
                //string psPathEscaped = PathTools.EscapePSText2(psPath);
                WriteItemObject(project, psPathEscaped, true);
            }
        }

        // GetChildnames は、オブジェクトでなく名前の string 値のみを WriteItemObject する必要がある。
        // このメソッドは、Get-ChildItem -Name を実行すると呼び出される。
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var projects = OrchTmDriveInfo.GetTmProjects();
            foreach (var project in projects!.OrderBy(p => p.name))
            {
                string psPathEscaped = OrchTmDriveInfo.NameColonSeparator + project.name;
                //string psPathEscaped = PathTools.EscapePSText2(psPath);
                WriteItemObject(psPathEscaped, psPathEscaped, false);
            }
        }

        protected override bool HasChildItems(string path)
        {
            return false;
        }

        protected override void RenameItem(string path, string newName)
        {
            var drives = OrchTmDriveInfo.EnumOrchTmDrives([path]);
            if (drives == null)
            {
                return;
            }

            foreach (var drive in drives)
            {
                var project = GetProject(path);
                if (project == null) continue;

                if (ShouldProcess(path, "Rename Project"))
                {
                    try
                    {
                        var postingProject = OrchCollectionExtensions.DeepCopy(project);
                        // postingProject.Path = null; // JsonIgnore 属性がついているので不要
                        postingProject.projectPrefix = null;
                        postingProject.name = newName;

                        drive.OrchAPISession.PutTmProject(postingProject);
                        drive._dicTmProjects = null;
                        //drive.ClearFolderCache(folder);
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameTmProjectError", ErrorCategory.InvalidOperation, project);
                        WriteError(errorRecord);
                        drive._dicTmProjects = null;
                    }
                }
            }
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            var drives = OrchTmDriveInfo.EnumOrchTmDrives([path]);
            if (drives == null)
            {
                return;
            }

            foreach (var drive in drives)
            {
                var project = GetProject(path);
                if (project == null) continue;

                if (ShouldProcess(path, "Remove Project"))
                {
                    try
                    {
                        drive.OrchAPISession.RemoveTmProject(project.id!);
                        drive._dicTmProjects = null;
                        //drive.ClearFolderCache(folder);
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(path, ex), "RemoveTmProjectError", ErrorCategory.InvalidOperation, project);
                        WriteError(errorRecord);
                        drive._dicTmProjects = null;
                    }
                }
            }
        }

        #endregion

        #region NavigationCmdletProvider overrides

        protected override bool IsItemContainer(string path)
        {
            // このプロバイダは、コンテナしか返さない（ファイルに相当するアイテムは返さない）
            return true;
        }

        protected override string MakePath(string parent, string child)
        {
            string retNew = base.MakePath(parent, child);
            if (retNew.EndsWith(Path.DirectorySeparatorChar) && retNew.Length > 1 && retNew[retNew.Length - 2] != Path.VolumeSeparatorChar)
            {
                retNew = retNew.Substring(0, retNew.Length - 1);
            }
            return retNew;
        }

        #endregion

    }
}