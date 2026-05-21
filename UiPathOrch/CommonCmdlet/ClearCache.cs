using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

// Clear-OrchCache scope dispatch
// ==============================
// The cmdlet's -Path argument is scope-classified by raw form:
//
//   <no -Path>          on Orch drive  -> drive-level full clear of current drive
//                       off Orch drive -> all caches on all authenticated drives
//   -AllDrives          -> all caches on all authenticated drives (explicit form)
//   -Path orch1:        -> drive-level full clear (backward compat carve-out;
//                          PowerShell's usual "current folder of drive" semantics
//                          are intentionally overridden so existing scripts that
//                          spell the drive name continue to clear everything)
//   -Path orch1:\       -> tenant cache only (per-tenant + per-organization);
//                          mirrors the root-folder presentation surface, where
//                          Get-ChildItem orch1:\ shows tenant-scoped entities
//   -Path orch1:\Shared -> per-folder cache for that folder only; tenant and
//                          organization scopes are untouched. -Recurse / -Depth
//                          extend to subfolders.
//   -Path .             -> resolves to current location; reclassified as above
//
// Auth-state gate: every drive touched is first probed with IsAuthenticated.
// An unauthenticated drive cannot have a populated cache (nothing has been
// fetched into it), so a clear is a no-op -- silently skip rather than
// trigger a PKCE flow on the user's behalf to confirm an empty cache.
//
// DU / Tm shadow drives: drive-only and drive-root forms dispatch to the
// corresponding cache methods. Folder-form (project paths under DU/Tm) is not
// supported yet; it falls back to drive-level clear with a verbose note.
[Cmdlet(VerbsCommon.Clear, "OrchCache", SupportsShouldProcess = true)]
public class ClearCacheCmdlet : PSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(Position0AllDriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public SwitchParameter AllDrives { get; set; }

    // Drive completion shows every Orch / Du / Tm drive. Intentionally NOT
    // a folder completer -- folder paths can be typed manually when needed,
    // but the common Clear-OrchCache invocation is at the drive level and
    // folder enumeration here would be expensive and noisy.
    public class Position0AllDriveCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wpPath = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var drive in SessionState.EnumAllOrchDrives()
                .ExcludeByWildcards(d => d?.NameColon, wpPath)
                .Where(d => wp.IsMatch(d.NameColon)))
            {
                yield return DriveCompletion(drive.NameColon, drive.DisplayRoot, drive.Description);
            }
            foreach (var drive in SessionState.EnumAllDuDrives()
                .ExcludeByWildcards(d => d?.NameColon, wpPath)
                .Where(d => wp.IsMatch(d.NameColon)))
            {
                yield return DriveCompletion(drive.NameColon, drive.DisplayRoot, drive.Description);
            }
            foreach (var drive in SessionState.EnumAllTmDrives()
                .ExcludeByWildcards(d => d?.NameColon, wpPath)
                .Where(d => wp.IsMatch(d.NameColon)))
            {
                yield return DriveCompletion(drive.NameColon, drive.DisplayRoot, drive.Description);
            }
        }

        private static CompletionResult DriveCompletion(string driveName, string displayRoot, string? description)
        {
            string tiphelp = displayRoot;
            if (!string.IsNullOrEmpty(description)) tiphelp += $" ({description})";
            return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
        }
    }

    protected override void ProcessRecord()
    {
        if (AllDrives.IsPresent)
        {
            ClearAllAuthenticatedDrives();
            return;
        }

        // No -Path: dispatch by current location
        if (Path is null || Path.Length == 0)
        {
            if (SessionState.Path.CurrentLocation.Drive is OrchDriveInfoBase currentDrive)
            {
                if (currentDrive.IsAuthenticated) ClearDriveAll(currentDrive);
            }
            else
            {
                ClearAllAuthenticatedDrives();
            }
            return;
        }

        foreach (var pathSpec in Path)
        {
            ProcessPathSpec(pathSpec);
        }
    }

    private void ProcessPathSpec(string pathSpec)
    {
        // Auth-free drive lookup via raw string parse + SessionState.Drive.Get.
        // The Enum*Drives / EnumFolders helpers internally call
        // GetResolvedPSPathFromPSPath which round-trips to the provider's
        // ItemExists -- and that path validation can trigger PKCE on a drive
        // that hasn't been authenticated yet. Looking up by name first lets
        // us check IsAuthenticated before doing any provider-level work.
        var drive = TryGetDriveForPath(pathSpec);
        if (drive is null)
        {
            // Drive name doesn't match any registered drive. Fall through to
            // the legacy enumerator so the user gets a normal "drive not
            // found" error.
            foreach (var d in SessionState.EnumOrchDrives(new[] { pathSpec })) ClearDriveAll(d);
            foreach (var d in SessionState.EnumDuDrives(new[] { pathSpec })) ClearDriveAll(d);
            foreach (var d in SessionState.EnumTmDrives(new[] { pathSpec })) ClearDriveAll(d);
            return;
        }

        if (!drive.IsAuthenticated)
        {
            WriteVerbose($"-Path '{pathSpec}': drive '{drive.NameColon}' is not authenticated; cache is empty, nothing to clear.");
            return;
        }

        // Authenticated -- dispatch by scope.
        if (IsDriveOnlyForm(pathSpec))
        {
            ClearDriveAll(drive);
            return;
        }

        if (IsDriveRootForm(pathSpec))
        {
            ClearDriveTenant(drive);
            return;
        }

        if (drive is OrchDuDriveInfo || drive is OrchTmDriveInfo)
        {
            // Shadow drives don't yet support folder-path scope.
            WriteVerbose($"-Path '{pathSpec}' on a DU/Tm drive resolves to drive-level clear (project-path scope not yet implemented).");
            ClearDriveAll(drive);
            return;
        }

        // Folder path on an authenticated Orch main drive -- safe to resolve.
        var pairs = SessionState.EnumFolders(new[] { pathSpec }, Recurse.IsPresent, Depth, includeRoot: true);
        foreach (var (resolvedDrive, folder) in pairs)
        {
            if (IsRootFolder(folder))
            {
                // Defensive: drive-root form is handled above, but if
                // EnumFolders returns root via some wildcard match, treat
                // consistently as tenant clear.
                ClearDriveTenant(resolvedDrive);
            }
            else
            {
                if (ShouldProcess(folder.GetPSPath(), "Clear Folder Cache"))
                {
                    resolvedDrive.ClearFolderCache(folder);
                }
            }
        }
    }

    // Path classifier: drive-only form ("orch1:" with no path part after the
    // colon). Backward-compat carve-out -- normal PowerShell semantics would
    // resolve "orch1:" to the drive's current location, but existing scripts
    // expect drive-level clearing.
    private static bool IsDriveOnlyForm(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        var colonIdx = raw.IndexOf(':');
        return colonIdx > 0 && colonIdx == raw.Length - 1;
    }

    // Path classifier: drive-root form ("orch1:\" -- one separator after the
    // colon). Maps to tenant-scope clear: the root folder presentation
    // surface is tenant entities, so clearing what's visible at root means
    // clearing the tenant cache.
    private static bool IsDriveRootForm(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        var colonIdx = raw.IndexOf(':');
        if (colonIdx <= 0 || colonIdx >= raw.Length - 1) return false;
        var afterColon = raw.AsSpan(colonIdx + 1);
        return afterColon.Length == 1 && (afterColon[0] == '\\' || afterColon[0] == '/');
    }

    private static bool IsRootFolder(UiPath.PowerShell.Entities.Folder folder)
        => folder.Id is null || folder.Id.Value == 0;

    // Drive lookup that never touches the provider. Pure string parse for
    // the drive prefix + SessionState.Drive.Get for the lookup. Used as the
    // gate that lets us run IsAuthenticated before doing any work that
    // would otherwise round-trip through the provider stack.
    private OrchDriveInfoBase? TryGetDriveForPath(string pathSpec)
    {
        if (string.IsNullOrEmpty(pathSpec)) return null;
        var colonIdx = pathSpec.IndexOf(':');
        if (colonIdx <= 0)
        {
            // No colon: relative path -- belongs to the current drive.
            return SessionState.Path.CurrentLocation.Drive as OrchDriveInfoBase;
        }

        var driveName = pathSpec.Substring(0, colonIdx);
        try
        {
            return SessionState.Drive.Get(driveName) as OrchDriveInfoBase;
        }
        catch
        {
            return null;
        }
    }

    private void ClearAllAuthenticatedDrives()
    {
        foreach (var drive in SessionState.EnumAllOrchDrives())
        {
            if (drive.IsAuthenticated) ClearDriveAll(drive);
        }
        foreach (var drive in SessionState.EnumAllDuDrives())
        {
            if (drive.IsAuthenticated) ClearDriveAll(drive);
        }
        foreach (var drive in SessionState.EnumAllTmDrives())
        {
            if (drive.IsAuthenticated) ClearDriveAll(drive);
        }
    }

    private void ClearDriveAll(OrchDriveInfoBase drive)
    {
        // NameColon ("Orch1:") matches the drive-only -Path form the user
        // typed. Using NameColonSeparator ("Orch1:\") here would print the
        // drive-root form, which in the new scope dispatch means tenant-only
        // -- confusing for a drive-level clear.
        if (!ShouldProcess(drive.NameColon, "Clear Cache")) return;
        drive.ClearAllCache();

        // DU/Tm shadow drives share data with their parent Orch drive (the
        // shadow is just a view onto org/tenant state owned by the main drive);
        // clear the parent too so a follow-up Get-* on the parent doesn't see
        // stale data that the user already discarded on the shadow.
        switch (drive)
        {
            case OrchDuDriveInfo du:
                du.ParentDrive?.ClearAllCache();
                break;
            case OrchTmDriveInfo tm:
                tm.ParentDrive?.ClearAllCache();
                break;
        }
    }

    private void ClearDriveTenant(OrchDriveInfoBase drive)
    {
        if (ShouldProcess(drive.NameColonSeparator, "Clear Tenant Cache"))
        {
            drive.ClearTenantCache();
        }
    }
}
