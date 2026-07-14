using System.Management.Automation;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

/// <summary>
/// The drive context an Invoke-OrchApi call runs in, resolved from -Path or the current location.
///
/// A UiPathOrch drive contributes a folder (the OU header). A DU or TM drive contributes a PROJECT
/// instead: it has no Orchestrator folders, and every DU/TM endpoint is scoped by a project id in
/// its path (/du_/api/framework/projects/{projectId}/..., /testmanager_/api/v2/{projectId}/...).
/// Either way the auth, base URLs and partition come from the Orchestrator drive — for a DU/TM
/// drive that is its ParentDrive, since a shadow drive is just a view onto the same tenant.
/// </summary>
/// <param name="Drive">The Orchestrator drive owning auth + base URLs (a shadow drive's parent).</param>
/// <param name="Folder">Orchestrator folder for the X-UIPATH-OrganizationUnitId header. Null on a
/// DU/TM drive, which has no folders — the call goes out without a folder context.</param>
/// <param name="PartitionGlobalId">Fills {partitionGlobalId}. Null when unknown.</param>
/// <param name="ProjectId">Fills {projectId}: the DU/TM project the location points at. Null at a
/// shadow drive's root (no single project) and on a UiPathOrch drive.</param>
/// <param name="ProjectKind">Which service <paramref name="ProjectId"/> belongs to. A project id is
/// only meaningful within its own service, so it is never substituted into another's path.</param>
/// <param name="ContextPath">PSPath stamped on emitted records — where the caller actually was.</param>
internal sealed record ApiContext(
    OrchDriveInfo Drive,
    Folder? Folder,
    string? PartitionGlobalId,
    string? ProjectId,
    ApiService ProjectKind,
    string ContextPath);

internal static class ApiContextResolver
{
    /// <summary>
    /// Resolves the context for <paramref name="path"/> (or, when it is empty, the current
    /// location). Returns null when that location is not on any UiPathOrch / DU / TM drive.
    ///
    /// <paramref name="allowFetch"/> is the difference between the cmdlet and the completer.
    /// The cmdlet may hit the API to learn the partition id or the project list. The completer
    /// runs on a &lt;Tab&gt; keypress and may NOT: it passes false, which restricts the lookups to
    /// values already in memory (a cached partition id or the token's prt_id claim; an already
    /// populated project cache). Unresolved ids then stay as their {placeholder}, which the user
    /// can fill by hand — far better than a completion that blocks on a sign-in.
    /// </summary>
    internal static ApiContext? Resolve(SessionState? sessionState, string? path, bool allowFetch)
    {
        if (sessionState is null) return null;

        var psDrive = DriveOf(sessionState, path);

        return psDrive switch
        {
            OrchDriveInfo orch => FromOrchDrive(sessionState, orch, path, allowFetch),
            OrchDuDriveInfo du => FromShadowDrive(sessionState, du.ParentDrive, du, du.DuProjects,
                                                  p => p.name, p => p.id,
                                                  ApiService.DocumentUnderstanding, path, allowFetch),
            OrchTmDriveInfo tm => FromShadowDrive(sessionState, tm.ParentDrive, tm, tm.TmProjects,
                                                  p => p.projectPrefix, p => p.id,
                                                  ApiService.TestManager, path, allowFetch),
            _ => null,
        };
    }

    // The drive a path names, or the current location's drive when no path was given.
    private static PSDriveInfo? DriveOf(SessionState sessionState, string? path)
    {
        if (string.IsNullOrEmpty(path)) return sessionState.Path.CurrentLocation?.Drive;

        string name = OrchDriveInfo.ExtractDriveName(path);
        if (string.IsNullOrEmpty(name)) return sessionState.Path.CurrentLocation?.Drive;

        try { return sessionState.Drive.Get(name); }
        catch { return null; }   // no such drive — the caller reports it
    }

    private static ApiContext? FromOrchDrive(SessionState sessionState, OrchDriveInfo orch, string? path, bool allowFetch)
    {
        string resolvePath = string.IsNullOrEmpty(path) ? sessionState.Path.CurrentLocation.Path : path;

        OrchDriveInfo drive;
        Folder folder;
        try { (drive, folder) = sessionState.ResolveToSingleFolder(resolvePath); }
        catch when (!allowFetch) { return null; }   // completer: a bad path yields no completions
        // allowFetch (the cmdlet): let the resolution error propagate to the caller, which reports it.

        return new ApiContext(
            drive,
            folder,
            ResolvePartitionGlobalId(drive, allowFetch),
            ProjectId: null,
            ApiService.None,
            folder.GetPSPath());
    }

    private static ApiContext? FromShadowDrive<TProject>(
        SessionState sessionState,
        OrchDriveInfo parent,
        OrchDriveInfoBase shadow,
        ListCachePerTenant<TProject> projects,
        Func<TProject, string?> getName,
        Func<TProject, string?> getId,
        ApiService projectKind,
        string? path,
        bool allowFetch)
        where TProject : class
    {
        string resolvePath = string.IsNullOrEmpty(path) ? sessionState.Path.CurrentLocation.Path : path;

        // A flat drive: the drive-relative path IS the project name (empty = the drive root, where
        // no single project is in scope, so {projectId} stays unfilled).
        string projectName = OrchDriveInfo.PSPathToOrchPath(resolvePath);

        string? projectId = null;
        if (projectName.Length > 0)
        {
            var list = allowFetch ? projects.Get() : projects.CachedValue;
            var match = list?.FirstOrDefault(
                p => string.Equals(getName(p), projectName, StringComparison.OrdinalIgnoreCase));
            if (match is not null) projectId = getId(match);
        }

        // The Orchestrator folder context does not apply to a DU/TM call: leave it null so no
        // X-UIPATH-OrganizationUnitId header goes out.
        return new ApiContext(
            parent,
            Folder: null,
            ResolvePartitionGlobalId(parent, allowFetch),
            projectId,
            projectKind,
            shadow.NameColonSeparator + projectName);
    }

    /// <summary>
    /// The name of <paramref name="parent"/>'s mounted DU / TM drive, or null when the tenant has
    /// none. Used to tell the user which -Path would supply a {projectId} — naming a drive that
    /// exists, not one we assume from the parent's name.
    /// </summary>
    internal static string? ShadowDriveNameFor(SessionState? sessionState, OrchDriveInfo parent, ApiService service)
    {
        if (sessionState is null) return null;

        return service switch
        {
            ApiService.DocumentUnderstanding =>
                sessionState.EnumAllDuDrives().FirstOrDefault(d => ReferenceEquals(d.ParentDrive, parent))?.Name,
            ApiService.TestManager =>
                sessionState.EnumAllTmDrives().FirstOrDefault(d => ReferenceEquals(d.ParentDrive, parent))?.Name,
            _ => null,
        };
    }

    /// A human label for the service, for the messages that name it.
    internal static string ServiceLabel(ApiService service) => service switch
    {
        ApiService.DocumentUnderstanding => "Document Understanding",
        ApiService.TestManager => "Test Manager",
        _ => "",
    };

    /// The partition global id, at the cost the caller allows. Without fetching there are two free
    /// sources: the value the drive already resolved, and the prt_id claim of the token in memory.
    /// GetPartitionGlobalId() is the fetching path — its fallback enumerates /odata/Users and reads
    /// each user in turn, which is why the completer must never take it.
    private static string? ResolvePartitionGlobalId(OrchDriveInfo drive, bool allowFetch)
        => allowFetch
            ? drive.GetPartitionGlobalId()
            : drive.PartitionGlobalId ?? drive.OrchAPISession.AuthManager.GetPartitionGlobalIdFromJwt();
}
