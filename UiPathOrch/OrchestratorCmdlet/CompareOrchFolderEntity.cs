using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Shared folder-scoped comparison engine for the Compare-Orch* family (Compare-OrchProcess,
// Compare-OrchQueue, ...). Resolves the reference (-Path) and difference (-DifferencePath)
// folders, matches entities by name, and emits OrchComparison rows. The caller supplies the
// entity accessor, name/path selectors, and the curated comparator set; everything else
// (mode dispatch, "<=" / "=>" / "<>" / "==", folder mirroring, the DifferenceName-not-found
// error) is identical across nouns and lives here.
//
// Compare-OrchAsset is intentionally NOT built on this helper: it needs an extra entity filter
// (-ValueType), an asymmetric per-user-value comparison (-UserMappingCsv), so it stays
// standalone. The diff engine (EntityComparison) and output type (OrchComparison) are shared
// by both.
// Shared -Property handling for the Compare-Orch* family: builds the case-insensitive filter
// set and warns on names that aren't comparators (otherwise a typo'd property would silently
// compare nothing and report every entity as equal).
internal static class CompareParameterHelper
{
    internal static HashSet<string>? ResolvePropertyFilter(Cmdlet host, string[]? property, IReadOnlyCollection<string> validNames)
    {
        if (property is not { Length: > 0 }) return null;

        var only = new HashSet<string>(property, StringComparer.OrdinalIgnoreCase);
        var unknown = only.Where(p => !validNames.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
        if (unknown.Count > 0)
        {
            host.WriteWarning($"-Property: ignoring unrecognized name(s): {string.Join(", ", unknown)}. " +
                $"Comparable properties are: {string.Join(", ", validNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))}.");
        }
        return only;
    }

    // One-time warning for Compare-Orch* on secret-bearing entities: secret fields are
    // write-only (the API never returns them), so the comparison cannot detect their drift.
    internal static void WarnSecretNotCompared(Cmdlet host, string secretNoun)
        => host.WriteWarning(
            $"Secret values are write-only (never returned by the API) and are NOT compared: " +
            $"{secretNoun}. This comparison cannot detect secret drift.");

    internal enum BroadcastMatch { Single, NotFound, Ambiguous }

    // Resolve a (possibly wildcard) -DifferenceName to exactly ONE entity from the candidate set.
    // Pure (no drive) so it is unit-testable; the caller fetches the candidates and writes the
    // entity-specific NotFound / Ambiguous error. Names carries the matched name(s) for the
    // ambiguous-error message. Matching is case-insensitive; a literal name (no wildcard
    // metacharacters) matches exactly.
    internal static (BroadcastMatch Status, T? Target, List<string> Names) ResolveBroadcastTarget<T>(
        IReadOnlyList<T> candidates, string pattern, Func<T, string?> getName) where T : class
    {
        var wp = new WildcardPattern(pattern, WildcardOptions.IgnoreCase);
        var matches = candidates.Where(e => getName(e) is { } n && wp.IsMatch(n)).ToList();
        var names = matches.Select(getName).Where(n => n is not null).Select(n => n!).ToList();
        return matches.Count switch
        {
            0 => (BroadcastMatch.NotFound, null, names),
            1 => (BroadcastMatch.Single, matches[0], names),
            _ => (BroadcastMatch.Ambiguous, null, names),
        };
    }
}

internal static class FolderCompare
{
    internal static void Run<T>(
        SessionState? sessionState,
        string? referencePath,
        string? differencePath,
        string? differenceName,
        List<WildcardPattern>? wpName,
        bool recurse, uint depth, bool includeEqual,
        IReadOnlyCollection<string>? only,
        Func<OrchDriveInfo, Folder, IEnumerable<T>> getEntities,
        Func<T?, string?> getName,
        Func<T, string> getPSPath,
        IReadOnlyList<(string Name, Func<T, object?> Get)> comparators,
        string errorId,
        Action<object> writeObject,
        Action<ErrorRecord> writeError) where T : class
    {
        var (srcDrive, srcRootFolder) = sessionState.ResolveToSingleFolder(referencePath);
        var srcDrivesFolders = sessionState.EnumFolders(referencePath, recurse, depth);
        var (dstDrive, dstRootFolder) = sessionState.ResolveToSingleFolder(differencePath);

        using var cancel = new ConsoleCancelHandler();

        // Broadcast mode: every reference entity vs the single named target.
        if (!string.IsNullOrEmpty(differenceName))
        {
            // -DifferenceName may be a wildcard, but must resolve to exactly one entity.
            List<T> candidates;
            try { candidates = getEntities(dstDrive, dstRootFolder).ToList(); }
            catch (Exception ex)
            {
                writeError(new ErrorRecord(new OrchException(dstRootFolder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, dstRootFolder));
                return;
            }
            var (status, matchedTarget, names) = CompareParameterHelper.ResolveBroadcastTarget(candidates, differenceName, getName);
            if (status == CompareParameterHelper.BroadcastMatch.NotFound)
            {
                writeError(new ErrorRecord(
                    new OrchException(dstRootFolder.GetPSPath(), $"DifferenceName '{differenceName}' was not found in '{dstRootFolder.GetPSPath()}'."),
                    "DifferenceNameNotFound", ErrorCategory.ObjectNotFound, differenceName));
                return;
            }
            if (status == CompareParameterHelper.BroadcastMatch.Ambiguous)
            {
                writeError(new ErrorRecord(
                    new OrchException(dstRootFolder.GetPSPath(), $"DifferenceName '{differenceName}' matched {names.Count} entities in '{dstRootFolder.GetPSPath()}' ({string.Join(", ", names)}); it must resolve to a single name."),
                    "DifferenceNameAmbiguous", ErrorCategory.InvalidArgument, differenceName));
                return;
            }
            T target = matchedTarget!;

            foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancel.Token))
            {
                IEnumerable<T> refs;
                try { refs = getEntities(srcDrive, srcFolder).FilterByWildcards(getName, wpName); }
                catch (Exception ex) { writeError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, srcFolder)); continue; }

                foreach (var r in refs)
                {
                    if (r is null) continue;
                    EmitComparison(r, target, only, includeEqual, getName, getPSPath, comparators, writeObject);
                }
            }
            return;
        }

        // Name-match mode: reference folder vs the mirrored difference folder.
        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancel.Token))
        {
            List<T> refs;
            try { refs = getEntities(srcDrive, srcFolder).FilterByWildcards(getName, wpName).ToList(); }
            catch (Exception ex) { writeError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, srcFolder)); continue; }

            Folder? dstFolder = ResolveDifferenceFolderOrNull(srcRootFolder, srcFolder, dstDrive, dstRootFolder);

            var diffByName = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            if (dstFolder is not null)
            {
                try
                {
                    foreach (var e in getEntities(dstDrive, dstFolder).FilterByWildcards(getName, wpName))
                        if (getName(e) is { } n) diffByName[n] = e;
                }
                catch (Exception ex) { writeError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, dstFolder)); continue; }
            }

            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in refs)
            {
                if (r is null) continue;
                if (getName(r) is { } name && diffByName.TryGetValue(name, out var d))
                {
                    matched.Add(name);
                    EmitComparison(r, d, only, includeEqual, getName, getPSPath, comparators, writeObject);
                }
                else
                {
                    EmitOnly(r, EntityComparison.ReferenceOnly, getName, getPSPath, writeObject);
                }
            }

            foreach (var kv in diffByName)
            {
                if (matched.Contains(kv.Key)) continue;
                EmitOnly(kv.Value, EntityComparison.DifferenceOnly, getName, getPSPath, writeObject);
            }
        }
    }

    private static void EmitComparison<T>(
        T reference, T difference, IReadOnlyCollection<string>? only, bool includeEqual,
        Func<T?, string?> getName, Func<T, string> getPSPath,
        IReadOnlyList<(string Name, Func<T, object?> Get)> comparators, Action<object> writeObject)
    {
        var diffs = EntityComparison.DiffProperties(reference, difference, comparators, only);
        if (diffs.Count == 0)
        {
            if (includeEqual)
            {
                writeObject(new OrchComparison
                {
                    SideIndicator = EntityComparison.Equal,
                    Name = getName(reference),
                    DifferenceName = getName(difference),
                    Path = getPSPath(reference),
                    DifferencePath = getPSPath(difference),
                    ReferenceObject = reference,
                    DifferenceObject = difference,
                });
            }
            return;
        }

        writeObject(new OrchComparison
        {
            SideIndicator = EntityComparison.Different,
            Name = getName(reference),
            DifferenceName = getName(difference),
            Path = getPSPath(reference),
            DifferencePath = getPSPath(difference),
            Differences = diffs,
            ReferenceObject = reference,
            DifferenceObject = difference,
        });
    }

    private static void EmitOnly<T>(T entity, string side, Func<T?, string?> getName, Func<T, string> getPSPath, Action<object> writeObject)
    {
        bool isReference = side == EntityComparison.ReferenceOnly;
        writeObject(new OrchComparison
        {
            SideIndicator = side,
            Name = getName(entity),
            DifferenceName = isReference ? null : getName(entity),
            Path = isReference ? getPSPath(entity) : null,
            DifferencePath = isReference ? null : getPSPath(entity),
            ReferenceObject = isReference ? entity : null,
            DifferenceObject = isReference ? null : entity,
        });
    }

    private static Folder? ResolveDifferenceFolderOrNull(Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder)
    {
        string relativePath = srcFolder.GetRelativePath(srcRootFolder);
        string dstRoot = dstRootFolder.FullyQualifiedName ?? "";
        string strDstFolder = dstRoot == "" ? relativePath : (dstRoot + "/" + relativePath).Trim('/');
        if (string.IsNullOrEmpty(strDstFolder)) return dstDrive.RootFolder;
        return dstDrive.GetFolders()
            .FirstOrDefault(f => string.Compare(f.FullyQualifiedName, strDstFolder, StringComparison.OrdinalIgnoreCase) == 0);
    }
}

// Tenant-scoped counterpart of FolderCompare for entities that live at the drive level rather
// than in folders (Role, Machine, CredentialStore, Calendar, Webhook, ...). The reference
// (-Path) and difference (-DifferencePath) resolve to whole drives; there is no folder
// mirroring and no -Recurse. The output Path/DifferencePath are built from the drive root and
// the entity name, since tenant-level Get accessors don't always populate the entity's Path.
internal static class TenantCompare
{
    internal static void Run<T>(
        SessionState? sessionState,
        string? referencePath,
        string? differencePath,
        string? differenceName,
        List<WildcardPattern>? wpName,
        bool includeEqual,
        IReadOnlyCollection<string>? only,
        Func<OrchDriveInfo, IEnumerable<T>> getEntities,
        Func<T?, string?> getName,
        IReadOnlyList<(string Name, Func<T, object?> Get)> comparators,
        string errorId,
        Action<object> writeObject,
        Action<ErrorRecord> writeError,
        // Optional: maps a reference entity's name to its difference-side equivalent before
        // matching (Compare-OrchUser's -UserMappingCsv, where the match key itself is renamed
        // across tenants). The emitted Name keeps the original reference value.
        Func<string?, string?>? referenceNameMap = null) where T : class
    {
        var srcDrive = sessionState.GetOrchDrive(referencePath);
        var dstDrive = sessionState.GetOrchDrive(differencePath);

        List<T> refs;
        try { refs = getEntities(srcDrive).FilterByWildcards(getName, wpName).ToList(); }
        catch (Exception ex) { writeError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), errorId, ErrorCategory.InvalidOperation, srcDrive)); return; }

        // Broadcast mode: every reference entity vs the single named target.
        if (!string.IsNullOrEmpty(differenceName))
        {
            // -DifferenceName may be a wildcard, but must resolve to exactly one entity.
            List<T> candidates;
            try { candidates = getEntities(dstDrive).ToList(); }
            catch (Exception ex) { writeError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), errorId, ErrorCategory.InvalidOperation, dstDrive)); return; }
            var (status, matchedTarget, names) = CompareParameterHelper.ResolveBroadcastTarget(candidates, differenceName, getName);
            if (status == CompareParameterHelper.BroadcastMatch.NotFound)
            {
                writeError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, $"DifferenceName '{differenceName}' was not found in '{dstDrive.NameColonSeparator}'."),
                    "DifferenceNameNotFound", ErrorCategory.ObjectNotFound, differenceName));
                return;
            }
            if (status == CompareParameterHelper.BroadcastMatch.Ambiguous)
            {
                writeError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, $"DifferenceName '{differenceName}' matched {names.Count} entities in '{dstDrive.NameColonSeparator}' ({string.Join(", ", names)}); it must resolve to a single name."),
                    "DifferenceNameAmbiguous", ErrorCategory.InvalidArgument, differenceName));
                return;
            }
            T target = matchedTarget!;
            foreach (var r in refs)
            {
                if (r is null) continue;
                Emit(r, target, srcDrive, dstDrive, getName, only, includeEqual, comparators, writeObject);
            }
            return;
        }

        // Name-match mode.
        List<T> diffs;
        try { diffs = getEntities(dstDrive).FilterByWildcards(getName, wpName).ToList(); }
        catch (Exception ex) { writeError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), errorId, ErrorCategory.InvalidOperation, dstDrive)); return; }

        var diffByName = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in diffs)
            if (getName(e) is { } n) diffByName[n] = e;

        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in refs)
        {
            if (r is null) continue;
            var refName = getName(r);
            var matchKey = referenceNameMap is null ? refName : (referenceNameMap(refName) ?? refName);
            if (matchKey is { } key && diffByName.TryGetValue(key, out var d))
            {
                matched.Add(key);
                Emit(r, d, srcDrive, dstDrive, getName, only, includeEqual, comparators, writeObject);
            }
            else
            {
                EmitOnly(r, srcDrive, getName, EntityComparison.ReferenceOnly, writeObject);
            }
        }
        foreach (var kv in diffByName)
        {
            if (matched.Contains(kv.Key)) continue;
            EmitOnly(kv.Value, dstDrive, getName, EntityComparison.DifferenceOnly, writeObject);
        }
    }

    private static string TenantPath<T>(OrchDriveInfo drive, T entity, Func<T?, string?> getName)
        => System.IO.Path.Combine(drive.NameColonSeparator, getName(entity) ?? "");

    private static void Emit<T>(
        T reference, T difference, OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Func<T?, string?> getName,
        IReadOnlyCollection<string>? only, bool includeEqual,
        IReadOnlyList<(string Name, Func<T, object?> Get)> comparators, Action<object> writeObject)
    {
        var diffs = EntityComparison.DiffProperties(reference, difference, comparators, only);
        if (diffs.Count == 0)
        {
            if (includeEqual)
            {
                writeObject(new OrchComparison
                {
                    SideIndicator = EntityComparison.Equal,
                    Name = getName(reference),
                    DifferenceName = getName(difference),
                    Path = TenantPath(srcDrive, reference, getName),
                    DifferencePath = TenantPath(dstDrive, difference, getName),
                    ReferenceObject = reference,
                    DifferenceObject = difference,
                });
            }
            return;
        }

        writeObject(new OrchComparison
        {
            SideIndicator = EntityComparison.Different,
            Name = getName(reference),
            DifferenceName = getName(difference),
            Path = TenantPath(srcDrive, reference, getName),
            DifferencePath = TenantPath(dstDrive, difference, getName),
            Differences = diffs,
            ReferenceObject = reference,
            DifferenceObject = difference,
        });
    }

    private static void EmitOnly<T>(T entity, OrchDriveInfo drive, Func<T?, string?> getName, string side, Action<object> writeObject)
    {
        bool isReference = side == EntityComparison.ReferenceOnly;
        var p = TenantPath(drive, entity, getName);
        writeObject(new OrchComparison
        {
            SideIndicator = side,
            Name = getName(entity),
            DifferenceName = isReference ? null : getName(entity),
            Path = isReference ? p : null,
            DifferencePath = isReference ? null : p,
            ReferenceObject = isReference ? entity : null,
            DifferenceObject = isReference ? null : entity,
        });
    }
}
