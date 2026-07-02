using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    // =====================================================================
    // Destination resolution
    // ---------------------------------------------------------------------
    // Given a src entity, find its counterpart in the dst tenant/folder.
    // First the shared resolvers and ResolveDstByIdThenName, immediately
    // followed by the simple FindDst* that route their decision through it
    // (Bucket / Machine / Queue / Release / Calendar / CredentialStore /
    // TestSet). Below the divider are the remaining FindDst* that each carry
    // their own resolution core (Roles / User / Session), compound match
    // (TestCase / PmGroups), or link-FQN math (Folders).
    // =====================================================================

    // Generic name-match resolver used by the simple FindDst* methods
    // (FindDstRobot / FindDstMachine / FindDstQueue / FindDstRelease /
    // FindDstCalendar / FindDstCredentialStore / FindDstBucket /
    // FindDstTestSet).
    //
    // All current callers use the default StringComparison.OrdinalIgnoreCase
    // -- every entity kind the Orchestrator UI treats by name does so
    // case-insensitively, and same-name uniqueness on the server side is
    // case-insensitive too (creation with a differently-cased name fails
    // with "name already used"). The Ordinal overload is retained for any
    // future caller that has a legitimate case-sensitive need.
    internal static T? ResolveDstByName<T>(
        IEnumerable<T>? candidates,
        string? srcName,
        Func<T, string?> nameOf,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        where T : class
    {
        if (candidates is null || string.IsNullOrEmpty(srcName)) return null;
        return candidates.FirstOrDefault(c => c is not null && string.Equals(nameOf(c), srcName, comparison));
    }

    // Resolve the destination package entry point whose Path matches srcPath. Named
    // wrapper over ResolveDstByName so the entry-point match shares the family's
    // OrdinalIgnoreCase policy. CopyProcesses uses this to remap a Release's
    // EntryPointId across feeds; the original inline '==' was case-sensitive and
    // silently dropped the EntryPointId when a dst feed exposed the same entry point
    // under a different case (e.g. "Main.xaml" vs "main.xaml").
    internal static PackageEntryPoint? ResolveDstEntryPointByPath(
        IEnumerable<PackageEntryPoint>? dstEntryPoints, string? srcPath)
        => ResolveDstByName(dstEntryPoints, srcPath, e => e.Path);

    // Per-step outcome of ResolveDstByIdThenName.
    internal enum FindDstByNameResult
    {
        NullOrZeroId,   // srcId is null or 0 -> caller returns null silently
        SrcNotFound,    // no src entity carries srcId
        DstNotFound,    // src found, but no dst entity shares its name
        Resolved,       // matched; dst is non-null
    }

    // Pure decision core shared by the simple name-based FindDst* wrappers
    // (FindDstBucket / FindDstQueue / FindDstRelease / FindDstMachine /
    // FindDstCalendar / FindDstCredentialStore / FindDstTestSet): guard the id,
    // look the src entity up by id, then match a dst entity by name (case-
    // insensitive, via ResolveDstByName). The IO wrappers still own how each
    // result maps to WriteWarning / WriteError + the per-entity message, so this
    // extraction does not change their output. FindDstTestCase is intentionally
    // NOT covered — it matches on a compound (PackageIdentifier, Name) key.
    // Returns the matched src entity too (on DstNotFound / Resolved) so the IO
    // wrapper can name it in its "dst does not have ... '<name>'" message.
    internal static (TDst? dst, TSrc? src, FindDstByNameResult result) ResolveDstByIdThenName<TSrc, TDst>(
        IEnumerable<TSrc>? srcEntities, long? srcId, Func<TSrc, long?> srcIdOf,
        IEnumerable<TDst>? dstEntities, Func<TSrc, string?> srcNameOf, Func<TDst, string?> dstNameOf)
        where TSrc : class
        where TDst : class
    {
        if (srcId is null || srcId == 0) return (null, null, FindDstByNameResult.NullOrZeroId);

        var src = srcEntities?.FirstOrDefault(e => e is not null && srcIdOf(e) == srcId);
        if (src is null) return (null, null, FindDstByNameResult.SrcNotFound);

        var dst = ResolveDstByName(dstEntities, srcNameOf(src), dstNameOf);
        if (dst is null) return (null, src, FindDstByNameResult.DstNotFound);

        return (dst, src, FindDstByNameResult.Resolved);
    }

    // action should be like "Copy Process"
    internal static Bucket? FindDstBucket(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcBucketId,
        OrchDriveInfo dstDrive, Folder newFolder, string action, string msg)
    {
        if (srcBucketId is null || srcBucketId == 0) return null; // skip the fetches when there's no id

        var (dstBucket, srcBucket, result) = ResolveDstByIdThenName(
            srcDrive.Buckets.Get(srcFolder), srcBucketId, b => b.Id,
            dstDrive.Buckets.Get(newFolder), b => b.Name, b => b.Name);
        switch (result)
        {
            case FindDstByNameResult.SrcNotFound:
                _this.WriteWarning($"{msg}: {srcDrive.NameColonSeparator} does not have the bucket with Id = {srcBucketId}.");
                return null;
            case FindDstByNameResult.DstNotFound:
                _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have the bucket with Name = '{srcBucket!.Name}'.");
                return null;
        }
        return dstBucket;
    }

    internal static MachineFolder? FindDstMachine(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcMachineId, string msg,
        DropWarningBudget? budget = null)
    {
        if (srcMachineId is null || srcMachineId == 0) return null; // skip the fetches when there's no id

        try
        {
            var (dstMachineFolder, srcMachine, result) = ResolveDstByIdThenName(
                srcDrive.Machines.Get(), srcMachineId, m => m.Id,
                dstDrive.FolderMachinesAssigned.Get(dstFolder), m => m.Name, m => m.Name);
            switch (result)
            {
                case FindDstByNameResult.SrcNotFound:
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have machine with Id = {srcMachineId}.");
                    return null;
                case FindDstByNameResult.DstNotFound:
                    WarnDrop(_this, budget, $"machine '{srcMachine!.Name}'", $"{msg}: the machine '{srcMachine!.Name}' is dropped because it is not assigned in '{dstFolder.GetPSPath()}'. To keep it, copy the assignment from the source folder, e.g.: Copy-OrchFolderMachine -Path {PsLiteral(srcFolder.GetPSPath())} -Name {PsLiteral(srcMachine!.Name)} -Destination {PsLiteral(dstFolder.GetPSPath())}");
                    return null;
            }
            return dstMachineFolder;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateMachineIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static QueueDefinition? FindDstQueue(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcQueueId, string msg)
    {
        if (srcQueueId is null || srcQueueId.Value == 0) return null;

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out var _);

        IEnumerable<QueueDefinition>? srcQueues, dstQueues;
        try
        {
            srcQueues = srcDrive.Queues.Get(srcFolder);
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        try
        {
            dstQueues = dstDrive.Queues.Get(dstFolder);
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }

        var (dstQueue, srcQueue, result) = ResolveDstByIdThenName(
            srcQueues, srcQueueId, q => q.Id, dstQueues, q => q.Name, q => q.Name);
        switch (result)
        {
            // Normalized to a warning to match the rest of the FindDst* family —
            // only FindDstQueue used WriteError for src-not-found (accidental).
            case FindDstByNameResult.SrcNotFound:
                _this.WriteWarning($"{msg}: {srcFolder.GetPSPath()} does not have queue with Id = {srcQueueId}.");
                return null;
            case FindDstByNameResult.DstNotFound:
                _this.WriteWarning($"{msg}: {dstFolder.GetPSPath()} does not have queue with Name = '{srcQueue!.Name}'.");
                return null;
        }
        return dstQueue;
    }

    internal static Release? FindDstRelease(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcReleaseId, string msg)
    {
        if (srcReleaseId is null || srcReleaseId == 0) return null;

        IEnumerable<Release>? srcReleases, dstReleases;
        try
        {
            srcReleases = srcDrive.Releases.Get(srcFolder);
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        try
        {
            dstReleases = dstDrive.Releases.Get(dstFolder);
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(
                new OrchException(target, $"{msg}: Failed to get processes from {dstFolder.GetPSPath()}", ex),
                "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }

        var (dstRelease, srcRelease, result) = ResolveDstByIdThenName(
            srcReleases, srcReleaseId, r => r.Id, dstReleases, r => r.Name, r => r.Name);
        switch (result)
        {
            case FindDstByNameResult.SrcNotFound:
                _this.WriteWarning($"{msg}: The process id {srcReleaseId} not found in '{srcFolder.GetPSPath()}'.");
                return null;
            case FindDstByNameResult.DstNotFound:
                _this.WriteWarning($"{msg}: {dstFolder.GetPSPath()} does not have process with Name = '{srcRelease!.Name}'.");
                return null;
        }
        return dstRelease;
    }

    internal static ExtendedCalendar? FindDstCalendar(IWritableHost _this,
        OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Int64? srcCalendarId, string msg)
    {
        if (srcCalendarId is null || srcCalendarId == 0) return null;

        // Calendars are tenant-level. Only the dst fetch is wrapped (preserving the
        // original asymmetry — a src-side failure propagates to the caller).
        IEnumerable<ExtendedCalendar>? dstCalendars;
        try
        {
            dstCalendars = dstDrive.Calendars.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, msg, ex), "MigrateCalendarIdError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        var (dstCalendar, srcCalendar, result) = ResolveDstByIdThenName(
            srcDrive.Calendars.Get(), srcCalendarId, c => c.Id, dstCalendars, c => c.Name, c => c.Name);
        switch (result)
        {
            case FindDstByNameResult.SrcNotFound:
                _this.WriteWarning($"{msg}: {srcDrive.NameColonSeparator} doesn't have calendar with Id = {srcCalendarId}.");
                return null;
            case FindDstByNameResult.DstNotFound:
                _this.WriteWarning($"{msg}: Calendar with name '{srcCalendar!.Name}' does not exist in '{dstDrive.NameColonSeparator}'.");
                return null;
        }
        return dstCalendar;
    }

    internal static CredentialStore? FindDstCredentialStore(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcCredentialStoreId, string msg)
    {
        if (srcCredentialStoreId is null || srcCredentialStoreId.Value == 0) return null; // skip the fetches when there's no id

        try
        {
            var (dstCredentialStore, srcCredentialStore, result) = ResolveDstByIdThenName(
                srcDrive.CredentialStores.Get(), srcCredentialStoreId, cs => cs.Id,
                dstDrive.CredentialStores.Get(), cs => cs.Name, cs => cs.Name);
            switch (result)
            {
                case FindDstByNameResult.SrcNotFound:
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have credential store with Id = {srcCredentialStoreId}.");
                    return null;
                case FindDstByNameResult.DstNotFound:
                    _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have credential store with Name = '{srcCredentialStore!.Name}'.");
                    return null;
            }
            return dstCredentialStore;
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateCredentialStoreIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static TestSet? FindDstTestSet(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcTestSetId,
            OrchDriveInfo dstDrive, Folder newFolder, string msg)
    {
        if (srcTestSetId is null || srcTestSetId == 0) return null; // skip the fetches when there's no id

        var (dstTestSet, srcTestSet, result) = ResolveDstByIdThenName(
            srcDrive.TestSets.Get(srcFolder), srcTestSetId, ts => ts.Id,
            dstDrive.TestSets.Get(newFolder), ts => ts.Name, ts => ts.Name);
        switch (result)
        {
            case FindDstByNameResult.SrcNotFound:
                _this.WriteWarning($"{msg}: {srcFolder.GetPSPath()} does not have test set with Id = {srcTestSetId}.");
                return null;
            case FindDstByNameResult.DstNotFound:
                _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have test set with Name = '{srcTestSet!.Name}'.");
                return null;
        }
        return dstTestSet;
    }

    // ---------------------------------------------------------------------
    // Other FindDst* — not callers of ResolveDstByIdThenName: each has its
    // own resolution core / compound match / FQN math.
    // ---------------------------------------------------------------------

    // Per-src-role resolution outcome from ResolveDstRolesPure.
    internal enum FindDstRoleResult
    {
        Resolved,                       // matched a dst folder-scope role; Id appended to result list
        SkippedAsInherited,             // src role marked Origin = "Inherited" -> caller skips silently
        SkippedAsTenantRole,            // matched but dst role's Type == "Tenant" -> excluded from folder roles
        NotFoundInDstTenant,            // no dst role with matching Id (same-drive) or Name (cross-drive)
    }

    internal sealed record FindDstRoleEntry(SimpleRole SrcRole, Role? DstRole, FindDstRoleResult Result);

    // Pure version of FindDstRoles. Caller supplies the dst tenant role list
    // (fetched and possibly null-checked outside) so the matching policy is
    // exercisable without standing up a real OrchDriveInfo.
    //
    // Policy (preserved verbatim):
    //   - srcRoles with Origin == "Inherited" are silently skipped.
    //   - When isSameDrive, match by Id (safer for renamed roles).
    //   - When cross-drive, match by Name (case-insensitive).
    //   - A matched dst role with Type == "Tenant" is intentionally NOT
    //     added to the returned folder-role-id list -- tenant roles are
    //     surfaced in classic-folder user payloads but cannot legally be
    //     assigned as folder-scope roles.
    internal static List<FindDstRoleEntry> ResolveDstRolesPure(
        IEnumerable<SimpleRole> srcRoles,
        IEnumerable<Role> dstTenantRoles,
        bool isSameDrive)
    {
        var entries = new List<FindDstRoleEntry>();
        var dstList = dstTenantRoles.ToList();

        foreach (var sr in srcRoles)
        {
            if (sr.Origin == "Inherited")
            {
                entries.Add(new FindDstRoleEntry(sr, null, FindDstRoleResult.SkippedAsInherited));
                continue;
            }

            Role? matched = isSameDrive
                ? dstList.FirstOrDefault(r => r.Id == sr.Id)
                : dstList.FirstOrDefault(r => string.Equals(r.Name, sr.Name, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                entries.Add(new FindDstRoleEntry(sr, null, FindDstRoleResult.NotFoundInDstTenant));
                continue;
            }

            if (matched.Type == "Tenant")
            {
                entries.Add(new FindDstRoleEntry(sr, matched, FindDstRoleResult.SkippedAsTenantRole));
                continue;
            }

            entries.Add(new FindDstRoleEntry(sr, matched, FindDstRoleResult.Resolved));
        }
        return entries;
    }

    internal static List<Int64>? FindDstRoles(IWritableHost _this,
        OrchDriveInfo srcDrive, IEnumerable<SimpleRole> srcRoleIds,
        OrchDriveInfo dstDrive, string msg)
    {
        if (srcRoleIds is null || !srcRoleIds.Any()) return null;

        ICollection<Role> dstTenantRoles = null;
        try
        {
            dstTenantRoles = dstDrive.Roles.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, msg, ex), "MigrateRoleIdError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        var entries = ResolveDstRolesPure(srcRoleIds, dstTenantRoles, isSameDrive: srcDrive == dstDrive);

        var retRoles = new List<Int64>();
        foreach (var entry in entries)
        {
            switch (entry.Result)
            {
                case FindDstRoleResult.NotFoundInDstTenant:
                    _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have role with Name = '{entry.SrcRole.Name}'.");
                    break;
                case FindDstRoleResult.Resolved:
                    retRoles.Add(entry.DstRole!.Id ?? 0);
                    break;
                    // SkippedAsInherited / SkippedAsTenantRole: silent (preserved from original).
            }
        }
        return retRoles;
    }

    internal static TestCaseDefinition? FindDstTestCase(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcDefinitionId,
        OrchDriveInfo dstDrive, Folder newFolder, string msg)
    {
        var srcTestCases = srcDrive.TestCases.Get(srcFolder);
        var srcTestCase = srcTestCases.FirstOrDefault(ts => ts.Id == srcDefinitionId);
        if (srcTestCase is null)
        {
            _this.WriteWarning($"{msg}: {srcFolder.GetPSPath()} does not have test case with Id = {srcDefinitionId}.");
            return null;
        }

        var dstTestCases = dstDrive.TestCases.Get(newFolder);
        // Case-insensitive compound match. Test entity names follow the
        // same Orchestrator name-uniqueness rule as Buckets (rejected on
        // create when a same-name entity differs only in case), so a
        // case-sensitive '==' lookup here would miss a dst test case that
        // exists under a different case and cause a spurious "not found"
        // warning. PackageIdentifier is intrinsically case-stable but
        // matched the same way for symmetry.
        var dstTestCase = dstTestCases.FirstOrDefault(tc =>
            string.Equals(tc.PackageIdentifier, srcTestCase.PackageIdentifier, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(tc.Name, srcTestCase.Name, StringComparison.OrdinalIgnoreCase));
        if (dstTestCase is null)
        {
            _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have test case with PackageIdentifier = '{srcTestCase.PackageIdentifier}' and Name = '{srcTestCase.Name}'.");
        }
        return dstTestCase;
    }

    // If the group does not exist at the destination, create a group with the same name
    internal static List<PmGroup>? FindDstPmGroups(IWritableHost _this,
        OrchDriveInfo srcDrive, IEnumerable<string>? srcPmGroupIds,
        OrchDriveInfo dstDrive, string msg)
    {
        if (srcPmGroupIds is null) return null;

        string target = srcDrive.NameColonSeparator;
        IEnumerable<PmGroup>? srcPmGroups = null;
        try
        {
            srcPmGroups = srcDrive.PmGroups.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }
        if (srcPmGroups is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        target = dstDrive.NameColonSeparator;
        IEnumerable<PmGroup>? dstPmGroups = null;
        try
        {
            dstPmGroups = dstDrive.PmGroups.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }
        if (dstPmGroups is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        List<PmGroup> ret = [];
        foreach (var srcPmGroupId in srcPmGroupIds)
        {
            var srcPmGroup = srcPmGroups.FirstOrDefault(g => g?.id == srcPmGroupId);
            if (srcPmGroup is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have PmGroup with id = {srcPmGroupId}. Ignoring this id.");
                continue;
            }

            var dstPmGroup = dstPmGroups.FirstOrDefault(g => string.Compare(g!.displayName, srcPmGroup.displayName, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstPmGroup is null)
            {
                dstPmGroup = _this.CreatePmGroup(dstDrive, srcPmGroup.name);
                if (dstPmGroup is null) continue;
            }
            ret.Add(dstPmGroup);
        }
        return ret;
    }

    // Result of FindDstUser's pure-logic resolver (see ResolveDstUserPure).
    // Lets the IO wrapper emit the right warning per case without re-checking.
    internal enum FindDstUserResult
    {
        Found,                  // dstUser is non-null AND assigned to the target folder
        NotFound,               // no matching user in dstUsers (by name or email)
        NotAssignedToFolder,    // matched a user but they're not on newFolder's assigned-users list
    }

    // Pure name-resolution + folder-assignment check. Extracted from FindDstUser
    // so the matching policy is unit-testable without standing up a real
    // OrchDriveInfo / OrchAPISession / AuthManager. The IO wrapper below does
    // the cache fetch / cache-clear retry / warning emission.
    //
    // Matching order (preserved from the original implementation):
    //   1. UserMappingCsv lookup -- if userMapping[srcUser.UserName] exists
    //      and is non-empty, use the mapped name as searchName; otherwise
    //      searchName = srcUser.UserName.
    //   2. Case-insensitive UserName match against dstUsers.
    //   3. Case-insensitive UserName-equals-srcUser.EmailAddress match
    //      against dstUsers (only when allowEmailFallback is true).
    //   4. If a user is found, verify their Id is in assignedFolderUserIds;
    //      if not, return (user, NotAssignedToFolder) -- the caller should
    //      treat the user as unusable on this folder.
    internal static (Entities.User? user, FindDstUserResult result) ResolveDstUserPure(
        Entities.User srcUser,
        IEnumerable<Entities.User> dstUsers,
        Dictionary<string, string>? userMapping,
        HashSet<long> assignedFolderUserIds,
        bool allowEmailFallback = true)
    {
        string searchName = srcUser.UserName ?? "";
        if (userMapping is not null
            && userMapping.TryGetValue(searchName, out var mapped)
            && !string.IsNullOrEmpty(mapped))
        {
            searchName = mapped;
        }

        var dstUser = dstUsers.FirstOrDefault(u =>
            string.Equals(u.UserName, searchName, StringComparison.OrdinalIgnoreCase));

        if (dstUser is null && allowEmailFallback && !string.IsNullOrEmpty(srcUser.EmailAddress))
        {
            dstUser = dstUsers.FirstOrDefault(u =>
                string.Equals(u.UserName, srcUser.EmailAddress, StringComparison.OrdinalIgnoreCase));
        }

        if (dstUser is null) return (null, FindDstUserResult.NotFound);

        if (dstUser.Id is null || !assignedFolderUserIds.Contains(dstUser.Id.Value))
        {
            return (dstUser, FindDstUserResult.NotAssignedToFolder);
        }

        return (dstUser, FindDstUserResult.Found);
    }

    // TODO: Is this implementation incomplete? Need to search the directory for users.
    // The current implementation only searches local users.
    internal static Entities.User? FindDstUser(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcUserId, string msg,
        DropWarningBudget? budget = null,
        Dictionary<string, string>? userMapping = null)
    {
        if (srcUserId is null || srcUserId == 0) return null;
        try
        {
            var srcUser = srcDrive.Users.Get().FirstOrDefault(u => u.Id == srcUserId);
            if (srcUser is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have user with Id = {srcUserId}.");
                return null;
            }

            // Folder assignment fetch is auth-protected and the same on both
            // attempts, so fetch once outside the retry loop.
            var assignedFolderUserIds = dstDrive.FolderUsersWithInherited.Get(newFolder)
                .Where(ur => ur?.UserEntity?.Id is not null)
                .Select(ur => ur.UserEntity!.Id!.Value)
                .ToHashSet();

            // First attempt: try name match, then email fallback against the
            // currently-cached dstUsers list.
            var (dstUser, result) = ResolveDstUserPure(
                srcUser, dstDrive.Users.Get(), userMapping, assignedFolderUserIds,
                allowEmailFallback: true);

            // Retry once after clearing the tenant user cache. This handles the
            // case where AssignDirectoryUser was just called in CopyFolderUsers
            // and the cached user list is stale. Email fallback is enabled
            // on the retry too -- the first pass having tried email is no
            // reason to skip it after a fresh fetch (a B2B user whose
            // UserName != EmailAddress that wasn't in the stale cache is
            // exactly what the retry exists for).
            if (result == FindDstUserResult.NotFound)
            {
                dstDrive.Users.ClearCache();
                (dstUser, result) = ResolveDstUserPure(
                    srcUser, dstDrive.Users.Get(), userMapping, assignedFolderUserIds,
                    allowEmailFallback: true);
            }

            // Re-compute the search name (cheap) for the warning messages.
            string searchName = srcUser.UserName ?? "";
            if (userMapping is not null
                && userMapping.TryGetValue(searchName, out var mappedName)
                && !string.IsNullOrEmpty(mappedName))
            {
                searchName = mappedName;
            }

            switch (result)
            {
                case FindDstUserResult.NotFound:
                    // Routed through the budget too: during an asset copy the same missing
                    // user repeats for every asset that references them, and the summary
                    // should list not-found owners alongside not-assigned ones.
                    WarnDrop(_this, budget, $"user '{searchName}'", $"{msg}: {dstDrive.NameColon} does not have user with Name = '{searchName}'.");
                    return null;

                case FindDstUserResult.NotAssignedToFolder:
                    // Without this check, the cmdlet would PUT a per-Robot UserValue
                    // with a UserId that the destination folder doesn't own; the
                    // server returns 200 but silently drops the UserValue (and can
                    // wipe the asset's Global Value as a side effect).
                    WarnDrop(_this, budget, $"user '{searchName}'", $"{msg}: the per-user value for user '{searchName}' is dropped because the user is not assigned in '{newFolder.GetPSPath()}'. To keep it, copy the assignment from the source folder, e.g.: Copy-OrchFolderUser -Path {PsLiteral(srcFolder.GetPSPath())} -UserName {PsLiteral(searchName)} -Destination {PsLiteral(newFolder.GetPSPath())}");
                    return null;

                default:
                    return dstUser;
            }
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateUserIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static RobotsFromFolderModel? FindDstRobot(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcRobotId, string msg)
    {
        if (srcRobotId is null || srcRobotId == 0) return null;
        try
        {
            var srcRobot = srcDrive.Robots.Get()?.FirstOrDefault(r => r.Id == srcRobotId);
            if (srcRobot is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                return null;
            }
            //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            var dstRobot = ResolveDstByName(dstRobots, srcRobot.Name, r => r.Name);
            if (dstRobot is null)
            {
                _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have robot with Name = '{srcRobot.Name}' ({srcRobot.Username}) in '{dstFolder.GetPSPath()}'.");
                return null;
            }
            return dstRobot;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static RobotsFromFolderModel? FindDstRobotByUnattendedAccount(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcRobotId, string msg)
    {
        if (srcRobotId is null || srcRobotId == 0) return null;
        try
        {
            string? srcRobot_Type = null;
            string? srcRobot_Username = null;
            if (srcFolder.ProvisionType == "Manual")
            {
                // For classic folders, search for classic robots via GET /odata/Sessions
                var sessions = srcDrive.Sessions.Get(srcFolder);
                var srcRobot = sessions.FirstOrDefault(s => s.Robot?.Id == srcRobotId);
                if (srcRobot is null)
                {
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                    return null;
                }
                srcRobot_Type = srcRobot.Robot?.Type;
                srcRobot_Username = srcRobot.Robot?.Username;
            }
            else
            {
                var srcRobot = srcDrive.Robots.Get()?.FirstOrDefault(r => r.Id == srcRobotId);
                if (srcRobot is null)
                {
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                    return null;
                }
                srcRobot_Type = srcRobot.Type;
                srcRobot_Username = srcRobot.Username;
            }

            //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";

            // The current implementation searches for robots by the UR's Windows account name (an ID like domain\user)
            // Would it be better to search by the robot's own name? (Is that possible?)
            // For classic robots, no matching robot name can be found

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            // Both fields case-insensitive. Type is server-stable ("Unattended"
            // / "Development" / etc.) so case variation is unlikely in
            // practice, but the comparison costs nothing extra and any
            // future server-side casing change would silently not match
            // under the original case-sensitive '=='.
            var dstRobot = dstRobots?.FirstOrDefault(r =>
                string.Equals(r.Type, srcRobot_Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Username, srcRobot_Username, StringComparison.OrdinalIgnoreCase));
            if (dstRobot is null)
            {
                string target = dstFolder.GetPSPath();
                //_this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: A Robot with the user name '{srcRobot.Username}' is not configured in {dstFolder.GetPSPath()}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
                _this.WriteWarning($"{msg}: An unattended robot with the user name '{srcRobot_Username}' ({srcRobot_Username}) is not configured in {dstFolder.GetPSPath()}.");
                return null;
            }
            return dstRobot;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    // Pure 3-tier session matching extracted from FindDstSession so the
    // fallback chain is unit-testable. Original behaviour preserved verbatim,
    // including the questionable bits (see asymmetries below).
    //
    // Match tiers, applied in order:
    //   Tier 1 (primary): MachineName + HostMachineName + ServiceUserName
    //     all match case-insensitively. Null fields are coerced to "".
    //   Tier 2 (fallback A): MachineName + HostMachineName match
    //     case-insensitively AND the dst's ServiceUserName is null/empty.
    //     Useful when the dst session hasn't been assigned a service user yet.
    //   Tier 3 (fallback B): MachineName + HostMachineName match only.
    //     Loosest -- ignores ServiceUserName entirely.
    //
    // Preserved asymmetries (candidates for BugDiscovery):
    //   * Tier 1 coerces null fields to "" before comparing
    //     (string.Compare(s.X ?? "", srcX, ...)). Tiers 2 and 3 do NOT
    //     coerce on the dst side (string.Compare(s.X, srcX, ...)). When a
    //     dst session has a null field, Tier 1 sees it as ""-equals-srcX
    //     but Tiers 2/3 see null-vs-srcX which Compare treats with null
    //     ordering rules (null is less than any string). Different match
    //     outcomes are possible across tiers for the same row.
    //   * The wrapper (FindDstSession below) writes the "not found"
    //     warning BEFORE attempting Tier 2/3 fallbacks. A successful
    //     fallback resolution still leaves a misleading warning in the
    //     user's output stream.
    internal static MachineSessionRuntime? ResolveDstSessionPure(
        IEnumerable<MachineSessionRuntime> dstSessions,
        string srcMachineName,
        string srcHostMachineName,
        string srcServiceUserName)
    {
        // All tiers null-coerce dst fields to "" before compare so a dst
        // session row with null fields is visible to every tier. The
        // original implementation only coerced on Tier 1, which left
        // null-field rows unreachable for the looser tiers -- inconsistent
        // and fixed during this round of audits.

        // Tier 1: full triple match.
        var dstSession = dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ServiceUserName ?? "", srcServiceUserName, StringComparison.OrdinalIgnoreCase));
        if (dstSession is not null) return dstSession;

        // Tier 2: machine + host match, dst service user empty.
        dstSession = dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(s.ServiceUserName));
        if (dstSession is not null) return dstSession;

        // Tier 3: machine + host match only. Loosest.
        return dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase));
    }

    internal static MachineSessionRuntime? FindDstSession(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcSessionId, string msg)
    {
        if (srcSessionId is null || srcSessionId.Value == 0) return null;

        MachineSessionRuntime srcSession = null;
        try
        {
            // TODO: Changed this to use cache. Is it working correctly?
            var srcSessions = srcDrive.MachineSessionRuntimesByFolder.Fetch(srcFolder).ToList();
            srcSession = srcSessions.FirstOrDefault(s => s.SessionId == srcSessionId);
            if (srcSession is null)
            {
                _this.WriteWarning($"\"{srcFolder.GetPSPath()}\": {msg}: The session not found with SessionId {srcSessionId}.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "MigrateSessionIdError", ErrorCategory.InvalidOperation, srcFolder));
            return null;
        }

        var srcMachineName = srcSession.MachineName ?? "";
        var srcHostMachineName = srcSession.HostMachineName ?? "";
        var srcServiceUserName = srcSession.ServiceUserName ?? "";

        try
        {
            var dstSessions = dstDrive.MachineSessionRuntimesByFolder.Fetch(dstFolder);

            // Warn only when ALL three tiers miss. The original wrapper
            // warned after Tier 1 failed and BEFORE Tiers 2/3 were tried,
            // so a successful fallback resolution still left a misleading
            // "not found" message in the user's output stream. Fixed
            // during this round of audits.
            var resolved = ResolveDstSessionPure(dstSessions, srcMachineName, srcHostMachineName, srcServiceUserName);
            if (resolved is null)
            {
                _this.WriteWarning($"\"{dstFolder.GetPSPath()}\": {msg}: The session not found with MachineName ='{srcMachineName}', HostMachineName = '{srcHostMachineName}' and ServiceUserName = '{srcServiceUserName}'.");
            }
            return resolved;
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), msg, ex), "MigrateSessionIdError", ErrorCategory.InvalidOperation, dstFolder));
            return null;
        }
    }

    /// <summary>
    /// Finds, for each src link folder identified by <paramref name="folderIds"/>, the
    /// corresponding dst folder by rebasing src's relative position around
    /// <paramref name="srcAnchor"/> onto dst's tree around <paramref name="dstAnchor"/>.
    /// Replaces the older FQN-equality match, which was correct for cross-drive copies
    /// (where src and dst trees share FQN shape) but broken for same-drive copies
    /// (src and dst FQNs differ, so the equality only ever matched src against itself,
    /// leaving Link*/Bucket/Queue sharing the SOURCE entity into dst folders).
    /// </summary>
    internal static IEnumerable<Folder>? FindDstFolders(
        List<Int64>? folderIds,
        IEnumerable<Folder> srcFolders,
        IEnumerable<Folder> dstFolders,
        Folder srcAnchor,
        Folder dstAnchor)
    {
        if (folderIds is null) return null;

        var selectedSrcFolders = srcFolders.Where(src => folderIds.Contains(src.Id ?? 0)).ToList();
        if (selectedSrcFolders.Count == 0) return Enumerable.Empty<Folder>();

        var dstByFqn = new Dictionary<string, Folder>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dstFolders)
        {
            if (d.FullyQualifiedName is not null) dstByFqn[d.FullyQualifiedName] = d;
        }

        string srcAnchorFqn = srcAnchor.FullyQualifiedName ?? "";
        string dstAnchorFqn = dstAnchor.FullyQualifiedName ?? "";

        var result = new List<Folder>();
        foreach (var srcLink in selectedSrcFolders)
        {
            string? candidateDstFqn = ComputeDstFqn(srcLink.FullyQualifiedName ?? "", srcAnchorFqn, dstAnchorFqn);
            if (candidateDstFqn is null) continue;
            if (!dstByFqn.TryGetValue(candidateDstFqn, out var dstMatch)) continue;
            // Defensive: when src and dst share the folder pool (same drive), refuse to
            // return a dst folder that is itself one of the src link folders. Without
            // this, same-drive copies whose dst tree happens to alias an src link folder
            // would re-introduce the "share src into dst" foot-gun.
            if (folderIds.Contains(dstMatch.Id ?? 0)) continue;
            result.Add(dstMatch);
        }
        return result;
    }

    /// <summary>
    /// Returns the FQN of the dst folder corresponding to a src link folder, by rebasing
    /// the src→link relationship around the (srcAnchor, dstAnchor) pair. Handles three
    /// shapes: srcLink is a descendant of srcAnchor, srcLink is an ancestor of srcAnchor,
    /// or srcLink is reachable via a common ancestor higher up the src tree (sibling or
    /// cousin). Returns null when the relationship has no expressible dst equivalent
    /// (e.g., disjoint top-level subtrees).
    /// </summary>
    internal static string? ComputeDstFqn(string srcLinkFqn, string srcAnchorFqn, string dstAnchorFqn)
    {
        // Identical → dst equivalent is dstAnchor itself
        if (string.Equals(srcLinkFqn, srcAnchorFqn, StringComparison.OrdinalIgnoreCase))
        {
            return dstAnchorFqn;
        }

        // Descendant of srcAnchor: replace srcAnchor prefix with dstAnchor
        if (srcLinkFqn.StartsWith(srcAnchorFqn + "/", StringComparison.OrdinalIgnoreCase))
        {
            return dstAnchorFqn + srcLinkFqn.Substring(srcAnchorFqn.Length);
        }

        // Ancestor of srcAnchor: walk up dstAnchor by the same number of segments
        if (srcAnchorFqn.StartsWith(srcLinkFqn + "/", StringComparison.OrdinalIgnoreCase))
        {
            int upSteps = srcAnchorFqn.Substring(srcLinkFqn.Length).Count(c => c == '/');
            return WalkUp(dstAnchorFqn, upSteps);
        }

        // Sibling / cousin: find longest common prefix that ends at a "/" boundary,
        // walk up dstAnchor by the number of segments below that prefix in srcAnchor,
        // then descend into the dst tree by srcLink's tail under the common prefix.
        int lastBoundary = -1;
        int minLen = Math.Min(srcAnchorFqn.Length, srcLinkFqn.Length);
        for (int i = 0; i < minLen; i++)
        {
            if (char.ToLowerInvariant(srcAnchorFqn[i]) != char.ToLowerInvariant(srcLinkFqn[i])) break;
            if (srcAnchorFqn[i] == '/') lastBoundary = i;
        }
        if (lastBoundary < 0) return null; // no shared ancestor

        string srcCommon = srcAnchorFqn.Substring(0, lastBoundary);
        string srcSuffixToLink = srcLinkFqn.Substring(lastBoundary + 1);
        int upStepsFromAnchor = srcAnchorFqn.Substring(srcCommon.Length).Count(c => c == '/');

        string? dstCommon = WalkUp(dstAnchorFqn, upStepsFromAnchor);
        if (dstCommon is null) return null;

        return dstCommon.Length == 0 ? srcSuffixToLink : dstCommon + "/" + srcSuffixToLink;
    }

    /// <summary>Strips <paramref name="upSteps"/> trailing "/segment" pieces from
    /// <paramref name="fqn"/>. Returns null if the path can't go that high.</summary>
    internal static string? WalkUp(string fqn, int upSteps)
    {
        for (int i = 0; i < upSteps; i++)
        {
            int lastSlash = fqn.LastIndexOf('/');
            if (lastSlash < 0) return null;
            fqn = fqn.Substring(0, lastSlash);
        }
        return fqn;
    }
}
