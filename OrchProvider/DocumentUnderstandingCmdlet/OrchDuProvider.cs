//#undef DEBUG

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.Json;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core
{
    [CmdletProvider("UiPathOrchDu", ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
    public class OrchDuProvider : NavigationCmdletProvider
    {
        protected OrchDuDriveInfo OrchDuDriveInfo => (OrchDuDriveInfo)this.PSDriveInfo;

        protected OrchDriveInfo OrchDriveInfo => ((OrchDuDriveInfo)this.PSDriveInfo).ParentDrive;
        //protected OrchDriveInfo? GetOrchDriveInfo(string path)

        DuProject? GetProject(string path)
        {
            var psPath = Path.GetFileName(path);
            if (psPath == "") return null;

            var projects = OrchDuDriveInfo.GetDuProjects();
            return projects?.FirstOrDefault(p => string.Compare(p.name, psPath, StringComparison.OrdinalIgnoreCase) == 0);
        }

        #region CmdletProvider overrides

        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            OrchDuDriveInfo.SessionState = base.SessionState;
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
                        if ((drive.Scope?.Contains("Du.") ?? false) && (drive.Root?.Contains("uipath.com") ?? false))
                        {
                            string root = drive.Root.TrimEnd('/') + "/du_";

                            var duProvider = SessionState.Provider.GetOne("UiPathOrchDu");

                            var duDrive = new OrchDuDriveInfo(duProvider, drive.Name + "Du", drive?.Description ?? "", root);
                            SessionState.Drive.New(duDrive, scope: "Global");
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
            OrchDuDriveInfo duDrive = (OrchDuDriveInfo)drive;

            // プロバイダクラスのロード順によっては、このタイミングではまだ UiPathOrch プロバイダは未登録。
            // 下記の処理を、すべてのプロバイダの NewDrive で行うことで、UiPathOrch と UiPathOrchDu を確実に関連づけるようにする。
            try
            {
                // var orchProvider = SessionState.Provider.GetOne("UiPathOrch");
                // 例外が出なければ、orchProvider != null になっている
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
            WriteItemObject(GetProject(path), path, true);
        }

        protected override void InvokeDefaultAction(string path)
        {
            var drives = OrchDuDriveInfo.EnumOrchDrives([path]);
            if (drives == null)
            {
                return;
            }

            foreach (var drive in drives)
            {
                string endpoint = drive.OrchAPISession._base_url + "/du_/projects/";

                var project = GetProject(path);
                if (project != null) { endpoint += $"{project.id}/details"; }

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
            string path2 = Path.GetFileName(path);
            if (path2 == "") return true;

            return GetProject(path) != null;
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
            var projects = OrchDuDriveInfo.GetDuProjects();
            foreach (var project in projects!)
            {
                string psPathEscaped = OrchDuDriveInfo.NameColonSeparator + project.name;
                //string psPathEscaped = PathTools.EscapePSText2(psPath);
                WriteItemObject(project, psPathEscaped, true);
            }
        }

        // GetChildnames は、オブジェクトでなく名前の string 値のみを WriteItemObject する必要がある。
        // このメソッドは、Get-ChildItem -Name を実行すると呼び出される。
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var projects = OrchDuDriveInfo.GetDuProjects();
            foreach (var project in projects!)
            {
                string psPathEscaped = OrchDuDriveInfo.NameColonSeparator + project.name;
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
        }

        protected override void RemoveItem(string path, bool recurse)
        {
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
            if (retNew.EndsWith(Path.DirectorySeparatorChar) && retNew.Length > 1 && retNew[retNew.Length-2] != Path.VolumeSeparatorChar)
            {
                retNew = retNew.Substring(0, retNew.Length - 1);
            }
            return retNew;
        }

        #endregion

    }
}