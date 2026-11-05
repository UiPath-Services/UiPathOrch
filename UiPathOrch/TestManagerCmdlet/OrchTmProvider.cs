//#undef DEBUG

using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchTm", ProviderCapabilities.ShouldProcess)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
[OutputType(typeof(TmProject), ProviderCmdlet = ProviderCmdlet.GetItem)]
public class OrchTmProvider : OrchShadowProviderBase<OrchTmDriveInfo, TmProject>, IWritableHost
{
    // IWritableHost: as OrchProvider (the shared TmCopy stages write through it).
    public bool RendersWideProgress
    {
        get
        {
            try { return ProgressRendering.HostRendersWideProgress(Host?.Version); }
            catch { return false; }
        }
    }

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

    protected override string BuildBrowseUrl(OrchTmDriveInfo drive, TmProject? project)
    {
        string endpoint = drive.OrchAPISession._base_url + "/testmanager_/";
        if (project is not null) { endpoint += $"{project.projectPrefix}/dashboard"; }
        return endpoint;
    }

    protected override void RemoveProjectApi(OrchTmDriveInfo drive, TmProject project)
        => drive.OrchAPISession.RemoveTmProject(project.id!);

    // ----- create: New-Item Tm:\PREFIX ----------------------------------------

    // The leaf of the path is the project prefix (the drive's item name, see GetName); the
    // display name is a separate field and defaults to the prefix when omitted.
    public class NewItem_DynamicParameters
    {
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        public string? ProjectName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<DescriptionHere>))]
        public string? Description { get; set; }
    }

    protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        => new NewItem_DynamicParameters();

    protected override void NewItem(string path, string itemTypeName, object newItemValue)
    {
        var drive = ResolveShadowDrive(path);
        if (drive is null) return;

        // A flat drive: the project is the single segment under the root (as in GetProject).
        string prefix = OrchDriveInfo.PSPathToOrchPath(path);
        if (prefix.Length == 0 || prefix.Contains('/'))
        {
            WriteError(new ErrorRecord(new OrchException(path, $"'{path}' is not a valid project path. Supply the project prefix directly under the drive root. Example: New-Item {drive.NameColonSeparator}MYPROJ -ProjectName 'My Project'."), "NewTmProjectError", ErrorCategory.InvalidArgument, path));
            return;
        }

        if (!ShouldProcess(path, "New Project"))
        {
            return;
        }

        var dynamicParameters = DynamicParameters as NewItem_DynamicParameters ?? new();
        try
        {
            var created = drive.OrchAPISession.PostTmProject(new TmCreateProjectRequest
            {
                name = dynamicParameters.ProjectName ?? prefix,
                description = dynamicParameters.Description ?? "",
                projectPrefix = prefix,
                isActive = true,
            });
            drive.TmProjects.ClearCache();

            if (created is not null)
            {
                var (clone, fullName) = CloneStamped(drive, created);
                WriteItemObject(clone, fullName, true);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(path, ex), "NewTmProjectError", ErrorCategory.InvalidOperation, path));
            drive.TmProjects.ClearCache();
        }
    }

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

    // ----- copy: Copy-Item Tm1:\PRJ Tm2:\[NEWPREFIX] ---------------------------

    public class CopyItem_DynamicParameters
    {
        // Create (or reuse) the destination project only; copy none of its contents.
        [Parameter]
        public SwitchParameter ExcludeEntities { get; set; }
    }

    protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        => new CopyItem_DynamicParameters();

    // The dynamic-parameters object of the Copy-Item in progress, used only for its identity:
    // PowerShell makes one per invocation and hands it to every per-path call (a wildcard source
    // calls CopyItem once per match). See OrchProvider._copyInvocationToken.
    private object? _copyInvocationToken;

    // The stages, in the order of Test Manager's own menu. Each entity's stage does all of its own
    // work, which keeps them in dependency order: a requirement exists before the test cases are
    // assigned to it (in the test case stage), and the test cases before a test set's assignments.
    // The steps of one stage share its bar, each restarting the count. Each label is padded to
    // 16, the length of the longest ("Project settings"), so the child bars start their "[" in
    // the same column (as in OrchProvider.CopyItemRecurse; the trailing spaces are load-bearing).
    private static (string Label, Action<TmCopyContext, ProgressReporter> Run)[] CopyStages =>
    [
        ("Requirements    ", (c, r) =>
            {
                TmCopy.CopyRequirements(c, null, r);
                TmCopy.CopyLabels(c, r, TmCopy.RequirementType);
            }),
        ("Test cases      ", (c, r) =>
            {
                TmCopy.CopyTestCases(c, null, r);
                TmCopy.CopyTestSteps(c, r);
                TmCopy.CopyLabels(c, r, TmCopy.TestCaseType);
                TmCopy.CopyAssignments(c, TmRequirementAssignmentKind.Instance, c.Requirements, r);
            }),
        ("Test sets       ", (c, r) =>
            {
                TmCopy.CopyTestSets(c, null, r);
                TmCopy.CopyLabels(c, r, TmCopy.TestSetType);
                TmCopy.CopyAssignments(c, TmTestSetAssignmentKind.Instance, c.TestSets, r);
            }),
        ("Project settings", TmCopy.CopyProjectSettings),
    ];

    // The destination is a project path: `Tm2:\` keeps the source's prefix, `Tm2:\NEW` names it.
    // An existing project there is reused and the contents are added to it (as OrchProvider
    // reuses an existing folder); see TmCopy for how a re-run avoids duplicating them.
    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        bool excludeEntities = (DynamicParameters as CopyItem_DynamicParameters)?.ExcludeEntities.IsPresent ?? false;
        bool firstPathOfInvocation = !ReferenceEquals(DynamicParameters, _copyInvocationToken);
        if (firstPathOfInvocation) _copyInvocationToken = DynamicParameters;

        var srcDrive = ResolveShadowDrive(path);
        var srcProject = GetProject(path);
        if (srcDrive is null) return;
        if (srcProject is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"'{path}' is not a project. Copy-Item copies one project: Copy-Item {srcDrive.NameColonSeparator}PRJ <TmDrive>:\\[NEWPREFIX]."), "CopyTmProjectError", ErrorCategory.InvalidArgument, path));
            return;
        }

        // The destination's drive by name: the destination project may not exist yet, so the path
        // cannot be resolved through the session the way an existing item's can.
        int colon = copyPath.IndexOf(':');
        OrchTmDriveInfo? dstDrive = null;
        try
        {
            if (colon > 0) dstDrive = SessionState.Drive.Get(copyPath[..colon]) as OrchTmDriveInfo;
        }
        catch (System.Management.Automation.DriveNotFoundException) { }
        if (dstDrive is null)
        {
            WriteError(new ErrorRecord(new OrchException(copyPath, $"'{copyPath}' is not on a Test Manager drive."), "CopyTmProjectError", ErrorCategory.InvalidArgument, copyPath));
            return;
        }

        string dstPrefix = OrchDriveInfo.PSPathToOrchPath(copyPath[(colon + 1)..]);
        bool namedPrefix = dstPrefix.Length > 0;
        if (dstPrefix.Contains('/'))
        {
            WriteError(new ErrorRecord(new OrchException(copyPath, $"'{copyPath}' is not a valid project path. Supply the drive root, or a project prefix directly under it."), "CopyTmProjectError", ErrorCategory.InvalidArgument, copyPath));
            return;
        }
        if (!namedPrefix) dstPrefix = srcProject.projectPrefix!;

        // A wildcard source with a named destination would pour several projects into one: the
        // first path is copied, the later ones of the same invocation are refused.
        string dstKey = dstDrive.NameColonSeparator + dstPrefix;
        if (namedPrefix && !firstPathOfInvocation)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"Several projects cannot be copied into one project ({dstKey}). Copy them to the drive root, or one at a time."), "CopyTmProjectError", ErrorCategory.InvalidArgument, path));
            return;
        }

        TmProject? dstProject;
        try
        {
            dstProject = dstDrive.TmProjects.Get().FirstOrDefault(p => string.Equals(p.projectPrefix, dstPrefix, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "CopyTmProjectError", ErrorCategory.InvalidOperation, dstDrive));
            return;
        }
        if (dstProject?.id == srcProject.id)
        {
            WriteError(new ErrorRecord(new OrchException(path, "Cannot copy a project to itself. Name a new prefix: Copy-Item <src> <TmDrive>:\\NEWPREFIX."), "CopyTmProjectError", ErrorCategory.InvalidOperation, path));
            return;
        }

        if (!ShouldProcess($"Item: '{srcProject.GetPSPath()}' Destination: '{dstKey}'", "Copy Project"))
        {
            return;
        }

        if (dstProject is null)
        {
            try
            {
                var created = dstDrive.OrchAPISession.PostTmProject(new TmCreateProjectRequest
                {
                    name = srcProject.name,
                    description = srcProject.description ?? "",
                    projectPrefix = dstPrefix,
                    isActive = true,
                });
                dstDrive.TmProjects.ClearCache();
                if (created is null) return;
                var (clone, fullName) = CloneStamped(dstDrive, created);
                WriteItemObject(clone, fullName, true);
                dstProject = clone;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstKey, ex), "CopyTmProjectError", ErrorCategory.InvalidOperation, dstKey));
                dstDrive.TmProjects.ClearCache();
                return;
            }
        }
        else
        {
            // Stamped, so the messages below name the destination drive's path.
            dstProject = CloneStamped(dstDrive, dstProject).clone;
        }

        if (excludeEntities) return;

        using var cancelHandler = new ConsoleCancelHandler();
        var ctx = new TmCopyContext(this, srcDrive, CloneStamped(srcDrive, srcProject).clone, dstDrive, dstProject, true, Force.IsPresent, cancelHandler.Token);

        var stages = CopyStages;
        using var projectReporter = new ProgressReporter(this, stages.Length, "Project")
        {
            Context = $"{srcProject.GetPSPath()} -> {dstProject.GetPSPath()}",
        };
        var childReporters = new List<ProgressReporter>(stages.Length);
        try
        {
            int stageIndex = 0;
            foreach (var (label, run) in stages.WithCancellation(cancelHandler.Token))
            {
                projectReporter.WriteProgress(++stageIndex);
                var childReporter = new ProgressReporter(this, null, label, projectReporter);
                childReporters.Add(childReporter);
                run(ctx, childReporter);
                childReporter.WriteCompleted();
            }
        }
        finally
        {
            for (int i = childReporters.Count - 1; i >= 0; i--)
            {
                childReporters[i].Dispose();
            }
            ctx.ClearDstCaches();
        }
    }
}
