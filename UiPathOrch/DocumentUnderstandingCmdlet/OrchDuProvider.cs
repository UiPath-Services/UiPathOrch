//#undef DEBUG

using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchDu", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(DuProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
public class OrchDuProvider : OrchShadowProviderBase<OrchDuDriveInfo, DuProject>
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

    // ----- per-provider hooks -------------------------------------------------

    protected override string? GetName(DuProject project) => project.name;
    protected override DuProject Clone(DuProject project) => project.ShallowClone();
    protected override ListCachePerTenant<DuProject> ProjectsCache(OrchDuDriveInfo drive) => drive.DuProjects;
    protected override List<OrchDuDriveInfo> EnumShadowDrives(string path) => SessionState.EnumDuDrives([path]);

    protected override string ProviderName => "UiPathOrchDu";
    protected override string ScopeMarker => "Du.";
    protected override string RootSuffix => "du_";
    protected override string DriveSuffix => "Du";

    protected override OrchDuDriveInfo CreateShadowDrive(ProviderInfo provider, string name, string description, string root)
        => new(provider, name, description, root);

    protected override void LinkParentDrive(OrchDuDriveInfo drive, OrchDriveInfo parent) => drive.ParentDrive = parent;

    protected override string BuildBrowseUrl(OrchDuDriveInfo drive, DuProject? project)
    {
        string endpoint = drive.OrchAPISession._base_url + "/du_/projects/";
        if (project is not null) { endpoint += $"{project.id}/details"; }
        return endpoint;
    }

    protected override void RemoveProjectApi(OrchDuDriveInfo drive, DuProject project)
        => drive.OrchAPISession.RemoveDuProject(project.id!);

    // ----- DU-only operations -------------------------------------------------

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
                drive.DuProjects.ClearCache();

                // Re-fetch to get the created project and output it
                var projects = drive.GetDuProjects();
                var created = projects?.FirstOrDefault(p =>
                    string.Equals(p.name, projectName, StringComparison.OrdinalIgnoreCase));
                if (created is not null)
                {
                    // Per-drive ShallowClone() with drive-local Path /
                    // FullName stamped (uniform DU pattern).
                    var pathPrefix = drive.NameColonSeparator;
                    var clone = created.ShallowClone();
                    clone.Path = pathPrefix;
                    clone.FullName = pathPrefix + created.name;
                    // Emit the canonical stamped FullName (drive-qualified), not the raw
                    // input path — matches this provider's GetChildItems / Get-Item output.
                    WriteItemObject(clone, clone.FullName, true);
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

    // Document Understanding projects cannot be renamed: the DU API exposes no project-rename
    // endpoint, so this stays an explicit "not supported" error rather than a generic fallthrough.
    protected override void RenameItem(string path, string newName)
    {
        WriteError(new ErrorRecord(
            new OrchException(path, "Document Understanding projects cannot be renamed with Rename-Item: the DU API exposes no project-rename endpoint. Rename the project from the Document Understanding web app instead."),
            "RenameDuProjectNotSupported", ErrorCategory.NotImplemented, path));
    }
}
