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

    // The parent Orchestrator drive is NOT linked here. It is resolved by name on first use
    // (OrchDriveInfoBase.ResolveParentDriveByName) precisely because provider initialization order
    // is not controllable -- see NewDrive.
    protected abstract TDrive CreateShadowDrive(ProviderInfo provider, string name, string description, string root);

    // Web URL opened by Invoke-Item (browse the project), and the project-delete API call.
    protected abstract string BuildBrowseUrl(TDrive drive, TProject? project);
    protected abstract void RemoveProjectApi(TDrive drive, TProject project);

    // ----- shared helpers -----------------------------------------------------

    // PSDriveInfo can be null in some engine contexts — the hierarchical OrchProvider
    // documents the same hazard and routes everything through GetOrchDriveInfo(path).
    // Mirror that here: fall back to resolving the drive from the path spec instead of
    // letting an unguarded PSDriveInfo cast surface as a raw NullReferenceException.
    protected TDrive? ResolveShadowDrive(string path)
        => this.PSDriveInfo as TDrive ?? EnumShadowDrives(path)?.FirstOrDefault();

    protected TProject? GetProject(string path)
    {
        var drive = ResolveShadowDrive(path);
        if (drive is null) return null;

        // Reduce to the drive-relative Orchestrator path (qualifier stripped, separators
        // normalized/trimmed) — the same reduction IsValidProviderPath uses. A flat
        // provider's item path has exactly ONE segment under the drive root; matching
        // only the leaf accepted nested garbage — `Test-Path Du1:\NoSuchThing\RealProject`
        // returned true (and cd succeeded) because the leaf matched a cached project.
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        if (orchPath.Length == 0) return null;   // drive root: not a project
        if (orchPath.Contains('/')) return null; // nested path: no such item on a flat drive

        var projects = ProjectsCache(drive).Get();
        return projects?.FirstOrDefault(p => string.Compare(GetName(p), orchPath, StringComparison.OrdinalIgnoreCase) == 0);
    }

    // Per-call clone stamped with this drive's drive-qualified path, so the emitted object's
    // Path/FullName (hence GetPSPath()) are this drive's and never leak another drive's value.
    // Emitting the bare shared cache entity would leave FullName empty or carry a stale value.
    private (TProject clone, string fullName) CloneStamped(TDrive drive, TProject project)
    {
        string sep = drive.NameColonSeparator;
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

        // The engine passes a NULL basePath in some contexts even though the inherited signature
        // types it as non-nullable; the base NavigationCmdletProvider defends with the same `??=`.
        basePath ??= string.Empty;

        char sep = System.IO.Path.DirectorySeparatorChar;

        // Relativize against the drive root by string prefix — the base provider's parent-walk
        // cannot, because we re-root the parent of a top-level item (see PathTools). On a flat
        // provider every item is a single segment under the root, so the drive-relative path IS
        // the relative path. Null = not the drive-root case; the base handles it.
        string? result = PathTools.RelativizeFromDriveRoot(path, basePath);

        if (result is null)
        {
            result = base.NormalizeRelativePath(path, basePath);
            if (result.StartsWith(sep) && result.Length > 1)
                result = result[1..];
        }

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
            // A config without a "PSDrives" array is Import-OrchConfig's problem to
            // report; here just mount nothing instead of NRE-ing during provider init.
            foreach (var drive in config!.PSDrives ?? [])
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
        // InitializeDefaultDrives builds the typed drive itself and hands it back here. But
        // `New-PSDrive -PSProvider UiPathOrchDu` comes through the ENGINE, which has no way to
        // construct our type and passes a plain PSDriveInfo -- and casting that threw
        // InvalidCastException, so the config file was the only way a DU/TM drive could ever
        // exist. Build the typed drive from the engine's, as OrchProvider does for its own.
        var shadowDrive = drive as TDrive
            ?? CreateShadowDrive(drive.Provider, drive.Name, drive.Description ?? "", EnsureServiceRoot(drive.Root));

        // The parent is NOT required here. Each provider mounts its own default drives and the
        // order PowerShell initializes the providers in is not ours to control, so the parent
        // Orchestrator drive may not exist yet at this moment -- refusing to mount, or mounting a
        // permanently parentless drive, would make DU/TM drives come and go with the load order.
        // The drives resolve their parent lazily on first use instead (OrchDuDriveInfo.ParentDrive
        // -> OrchDriveInfoBase.ResolveParentDriveByName), which is correct under either order.
        return shadowDrive;
    }

    // The gateway routes a shadow service by a segment under the tenant root ("/du_", "/testmanager_"),
    // which InitializeDefaultDrives appends to the configured root. Do the same for a root typed at
    // New-PSDrive, so both entry points build the same drive whether or not the caller spelled it.
    private string EnsureServiceRoot(string? root)
    {
        string r = (root ?? "").TrimEnd('/');
        return r.EndsWith("/" + RootSuffix, StringComparison.OrdinalIgnoreCase) ? r : r + "/" + RootSuffix;
    }

    // ----- ItemCmdletProvider overrides ---------------------------------------

    protected override void GetItem(string path)
    {
        var drive = ResolveShadowDrive(path);
        if (drive is null) return;

        var project = GetProject(path);
        if (project is not null)
        {
            var (clone, fullName) = CloneStamped(drive, project);
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
        // uint.MaxValue is the engine's own encoding of "plain -Recurse" in the 3-arg
        // overload — delegate faithfully. (Depth is moot for this flat provider, but a
        // hardcoded 0 would silently mean "no recursion" to any future caller.)
        GetChildItems(path, recurse, uint.MaxValue);
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar))
        {
            return;
        }
        var drive = ResolveShadowDrive(path);
        if (drive is null) return;
        foreach (var project in ProjectsCache(drive).Get().OrderBy(GetName))
        {
            if (Stopping) return;
            var (clone, fullName) = CloneStamped(drive, project);
            WriteItemObject(clone, fullName, true);
        }
    }

    // GetChildNames must call WriteItemObject with just the name string, not the object.
    // This method is invoked when running Get-ChildItem -Name and wildcard resolution (cd t*, rmdir *).
    // The first argument (name string) is matched against the wildcard pattern by LocationGlobber.
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        var drive = ResolveShadowDrive(path);
        if (drive is null) return;
        string sep = drive.NameColonSeparator;
        foreach (var project in ProjectsCache(drive).Get().OrderBy(GetName))
        {
            if (Stopping) return;
            var name = GetName(project);
            WriteItemObject(name, sep + name, true);
        }
    }

    protected override bool HasChildItems(string path)
        => ResolveShadowDrive(path) is { } drive && path == drive.NameColonSeparator;

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
