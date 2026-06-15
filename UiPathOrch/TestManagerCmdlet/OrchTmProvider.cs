//#undef DEBUG

using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchTm", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
public class OrchTmProvider : OrchShadowProviderBase<OrchTmDriveInfo, TmProject>
{
    // ----- per-provider hooks -------------------------------------------------

    protected override string? GetName(TmProject project) => project.projectPrefix;
    protected override TmProject Clone(TmProject project) => project.ShallowClone();
    protected override ListCachePerTenant<TmProject> ProjectsCache(OrchTmDriveInfo drive) => drive.TmProjects;
    protected override List<OrchTmDriveInfo> EnumShadowDrives(string path) => SessionState.EnumTmDrives([path]);

    protected override string ProviderName => "UiPathOrchTm";
    protected override string ScopeMarker => "TM.";
    protected override string RootSuffix => "testmanager_";
    protected override string DriveSuffix => "Tm";

    protected override OrchTmDriveInfo CreateShadowDrive(ProviderInfo provider, string name, string description, string root)
        => new(provider, name, description, root);

    protected override void LinkParentDrive(OrchTmDriveInfo drive, OrchDriveInfo parent) => drive.ParentDrive = parent;

    protected override string BuildBrowseUrl(OrchTmDriveInfo drive, TmProject? project)
    {
        string endpoint = drive.OrchAPISession._base_url + "/testmanager_/";
        if (project is not null) { endpoint += $"{project.projectPrefix}/dashboard"; }
        return endpoint;
    }

    protected override void RemoveProjectApi(OrchTmDriveInfo drive, TmProject project)
        => drive.OrchAPISession.RemoveTmProject(project.id!);

    // ----- TM-only operation: rename in place ---------------------------------

    protected override void RenameItem(string path, string newName)
    {
        var drives = EnumShadowDrives(path);
        if (drives is null)
        {
            return;
        }

        // -NewName must be a leaf, not a path (Rename-Item renames in place, it does not move).
        // Reduce ".\Proj2" -> "Proj2"; reject names that point elsewhere (e.g. "..\Proj2").
        string? leaf = PathTools.RenameLeaf(path, newName);
        if (leaf is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"'{newName}' is not a valid new project name. Supply a leaf name, not a path. Example: Rename-Item .\\MyProject MyProject2."), "RenameTmProjectError", ErrorCategory.InvalidArgument, path));
            return;
        }
        newName = leaf;

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
                    drive.TmProjects.ClearCache();
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameTmProjectError", ErrorCategory.InvalidOperation, project);
                    WriteError(errorRecord);
                    drive.TmProjects.ClearCache();
                }
            }
        }
    }
}
