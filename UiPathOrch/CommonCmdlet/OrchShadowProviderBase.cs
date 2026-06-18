using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Core;

// Shared base for the flat "shadow" providers (Document Understanding, Test Manager) that mirror
// their parent Orchestrator drive with a single level of projects. Both expose only containers
// (projects, no leaf items) and share identical path normalization, drive bootstrap, item
// enumeration, get/exists, browse, and remove logic. The ONLY per-provider differences are a
// small set of typed hooks (which drive/entity type, the project's key field, the project cache,
// the create/remove API calls, the config scope marker, the browse URL). The hierarchical
// OrchProvider deliberately does NOT derive from this (its folder model differs); keep it
// standalone.
//
// TDrive   — the shadow drive-info (OrchDuDriveInfo / OrchTmDriveInfo).
// TProject — the project entity (DuProject / TmProject); IShadowProject gives Path/FullName so the
//            base can stamp the drive-local navigation fields generically.
public abstract class OrchShadowProviderBase<TDrive, TProject> : NavigationCmdletProvider
    where TDrive : OrchDriveInfoBase
    where TProject : class, IShadowProject
{
    // ----- per-provider hooks -------------------------------------------------

    // The project's key/leaf field (DuProject.name / TmProject.projectPrefix).
    protected abstract string? GetName(TProject project);

    // Shallow copy of the entity (the typed ShallowClone()); the base then stamps Path/FullName.
    protected abstract TProject Clone(TProject project);

    // The per-tenant project list cache on the drive (drive.DuProjects / drive.TmProjects).
    protected abstract ListCachePerTenant<TProject> ProjectsCache(TDrive drive);

    // Resolve the shadow drives a path spec maps to (SessionState.EnumDuDrives / EnumTmDrives).
    protected abstract List<TDrive> EnumShadowDrives(string path);

    // Registered provider name, config scope marker, root suffix, and drive-name suffix used to
    // bootstrap the shadow drives from the shared config file.
    protected abstract string ProviderName { get; }   // "UiPathOrchDu" / "UiPathOrchTm"
    protected abstract string ScopeMarker { get; }     // "Du." / "TM."
    protected abstract string RootSuffix { get; }      // "du_" / "testmanager_"
    protected abstract string DriveSuffix { get; }     // "Du" / "Tm"

    protected abstract TDrive CreateShadowDrive(ProviderInfo provider, string name, string description, string root);
    protected abstract void LinkParentDrive(TDrive drive, OrchDriveInfo parent);

    // Web URL opened by Invoke-Item (browse the project), and the project-delete API call.
    protected abstract string BuildBrowseUrl(TDrive drive, TProject? project);
    protected abstract void RemoveProjectApi(TDrive drive, TProject project);

    // ----- shared helpers -----------------------------------------------------

    protected TDrive ShadowDrive => (TDrive)this.PSDriveInfo;

    protected TProject? GetProject(string path)
    {
        var leaf = System.IO.Path.GetFileName(path);
        if (leaf == "") return null;

        var projects = ProjectsCache(ShadowDrive).Get();
        return projects?.FirstOrDefault(p => string.Compare(GetName(p), leaf, StringComparison.OrdinalIgnoreCase) == 0);
    }

    // Per-call clone stamped with this drive's drive-qualified path, so the emitted object's
    // Path/FullName (hence GetPSPath()) are this drive's and never leak another drive's value.
    // Emitting the bare shared cache entity would leave FullName empty or carry a stale value.
    private (TProject clone, string fullName) CloneStamped(TProject project)
    {
        string sep = ShadowDrive.NameColonSeparator;
        string fullName = sep + GetName(project);
        var clone = Clone(project);
        clone.Path = sep;
        clone.FullName = fullName;
        return (clone, fullName);
    }

    // ----- path semantics (flat, container-only) ------------------------------

    // These providers only ever return containers; there is no leaf/file concept. Syntactic
    // validation is shared with OrchProvider via PathTools (reject null/empty and control chars,
    // accept the drive root). Reached from `Test-Path -IsValid` and the NormalizeRelativePath guard.
    protected override bool IsValidPath(string path) => PathTools.IsValidProviderPath(path);

    protected override bool IsItemContainer(string path) => true;

    // Re-root a bare drive for the drive-root leaf case, shared with OrchProvider via PathTools.
    protected override string GetChildName(string path) => PathTools.GetChildNameWithDriveRoot(path);

    // Symmetric parent-side re-rooting ("Du1:" -> "Du1:\"), shared with OrchProvider via PathTools,
    // so PSParentPath / Split-Path -Parent of a top-level item match FileSystemProvider.
    protected override string GetParentPath(string path, string root) => PathTools.ParentPathWithDriveRoot(base.GetParentPath(path, root));

    protected override string MakePath(string parent, string child)
    {
        string result = base.MakePath(parent, child);
        // Trim a trailing separator (but keep the drive root "X:\").
        if (result.EndsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1 && result[^2] != ':')
            result = result[..^1];
        return result;
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        // Input guard, mirroring OrchProvider / FileSystemProvider: reject a malformed path up
        // front (an empty path is the drive root, so let it through to the base).
        if (!string.IsNullOrEmpty(path) && !IsValidPath(path))
        {
            throw new ArgumentException(
                $"The path '{path}' is not a valid {ProviderInfo?.Name ?? "Orchestrator"} provider path.",
                nameof(path));
        }

        string result = base.NormalizeRelativePath(path, basePath);
        if (result.StartsWith(System.IO.Path.DirectorySeparatorChar) && result.Length > 1)
            result = result[1..];

        // Canonicalize the single-segment project name's casing from cache. Passive read — it
        // must NOT trigger a fetch (this runs on every path op, including tab completion).
        if (!string.IsNullOrEmpty(result))
        {
            var projects = PSDriveInfo is TDrive drive ? ProjectsCache(drive).CachedValue : null;
            var match = projects?.FirstOrDefault(p => string.Equals(GetName(p), result, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                result = GetName(match) ?? result;
        }

        return result ?? "";
    }

    // ----- DriveCmdletProvider overrides --------------------------------------

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
                    if (drive.Scope?.Contains(ScopeMarker) ?? false)
                    {
                        if (drive.Root is null) continue;
                        string root = drive.Root.TrimEnd('/') + "/" + RootSuffix;

                        var provider = SessionState.Provider.GetOne(ProviderName);

                        var shadowDrive = CreateShadowDrive(provider, drive.Name + DriveSuffix, drive?.Description ?? "", root);
                        SessionState.Drive.New(shadowDrive, scope: "Global");
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
        var shadowDrive = (TDrive)drive;

        // Depending on the provider class loading order, the UiPathOrch provider may not be
        // registered yet at this point. By linking the parent drive in every shadow provider's
        // NewDrive, we ensure UiPathOrch and the shadow drive are reliably associated.
        try
        {
            // Strip the 2-char drive suffix ("Du"/"Tm") to find the parent Orchestrator drive.
            var orchDrive = SessionState.Drive.Get(drive.Name.Substring(0, drive.Name.Length - 2)) as OrchDriveInfo;
            LinkParentDrive(shadowDrive, orchDrive!);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{DriveSuffix} drive parent linking failed for '{drive.Name}': {ex.Message}");
        }

        return drive;
    }

    // ----- ItemCmdletProvider overrides ---------------------------------------

    protected override void GetItem(string path)
    {
        var project = GetProject(path);
        if (project is not null)
        {
            var (clone, fullName) = CloneStamped(project);
            WriteItemObject(clone, fullName, true);
        }
        // Root path or non-existent path: output nothing (no error)
    }

    protected override void InvokeDefaultAction(string path)
    {
        var drives = EnumShadowDrives(path);
        if (drives is null)
        {
            return;
        }

        foreach (var drive in drives)
        {
            var project = GetProject(path);
            string endpoint = BuildBrowseUrl(drive, project);
            Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
        }
    }

    // The ItemExists method apparently does not need to handle wildcards.
    protected override bool ItemExists(string path)
    {
        var leaf = System.IO.Path.GetFileName(path);
        if (leaf == "") return true;

        return GetProject(path) is not null;
    }

    // ----- ContainerCmdletProvider overrides ----------------------------------

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
        foreach (var project in ProjectsCache(ShadowDrive).Get().OrderBy(GetName))
        {
            if (Stopping) return;
            var (clone, fullName) = CloneStamped(project);
            WriteItemObject(clone, fullName, true);
        }
    }

    // GetChildNames must call WriteItemObject with just the name string, not the object.
    // This method is invoked when running Get-ChildItem -Name and wildcard resolution (cd t*, rmdir *).
    // The first argument (name string) is matched against the wildcard pattern by LocationGlobber.
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        string sep = ShadowDrive.NameColonSeparator;
        foreach (var project in ProjectsCache(ShadowDrive).Get().OrderBy(GetName))
        {
            if (Stopping) return;
            var name = GetName(project);
            WriteItemObject(name, sep + name, true);
        }
    }

    protected override bool HasChildItems(string path)
        => path == ShadowDrive.NameColonSeparator;

    // ----- NavigationCmdletProvider overrides ---------------------------------

    // Delete a project through the per-provider API call (RemoveDuProject / RemoveTmProject).
    // Rename diverges per provider (TM renames in place; DU has no rename endpoint) and is left to
    // the concrete classes; New is DU-only.
    protected override void RemoveItem(string path, bool recurse)
    {
        var drives = EnumShadowDrives(path);
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
                    RemoveProjectApi(drive, project);
                    ProjectsCache(drive).ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(path, ex), $"Remove{DriveSuffix}ProjectError", ErrorCategory.InvalidOperation, project));
                    ProjectsCache(drive).ClearCache();
                }
            }
        }
    }
}
