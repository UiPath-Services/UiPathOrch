using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// =============================================================================
// TmCopy.cs -- copying Test Manager entities between projects (and tenants)
// =============================================================================
//
// Shared by `Copy-Item Tm1:\PRJ Tm2:\[NEWPREFIX]` (OrchTmProvider.CopyItem, every stage) and the
// Copy-TmTestCase / Copy-TmRequirement / Copy-TmTestSet cmdlets (one entity kind each), the way
// OrchProvider.CopyAssets & co. are shared by Copy-Item and Copy-OrchAsset. The stages take an
// IWritableHost, so the provider and the cmdlets drive the same code.
//
// One TmCopyContext is one run from one source project into one destination project. It carries
// the source-id -> destination-id maps the later stages need: Test Manager allows several test
// cases (requirements, test sets) with the same name, so an assignment is re-created from ids,
// never from names. Copy-Item runs four stages, in the order of Test Manager's menu
// (OrchTmProvider.CopyStages): requirements (+ labels) -> test cases (+ steps, labels, assignment
// to the requirements) -> test sets (+ labels, assignments) -> project settings.
//
// Re-running a copy must not double everything, which a blind create would do. Items are matched
// across the projects by a KEY (TmCopyKey): the name (exact, case included) and, for test cases and
// requirements, the foreignReference (exact; an external ticket id, which the copy carries over
// as is and never writes itself -- the server lets it be set only at creation). Both are trimmed,
// as the server trims them, and an empty reference is no reference. Test Manager enforces neither
// unique, so per key (TmCopy.Decide):
//   - no destination item with the key: created (also when the source has several);
//   - one in the source and one in the destination: PAIRED instead of created. The pair goes into
//     the id map (so assignments and labels still land) and the destination item is left alone --
//     except that a paired test case with no steps gets the source's steps (the "test case
//     created, steps failed partway" case);
//   - otherwise (several on either side, and at least one in the destination) it is not known
//     which is which: skipped with a warning, unless -Force, which creates them as new items.
// So a re-run never adds anything without -Force, and -Force still reuses the 1:1 pairs.
//
// Not copied (warned where the source has them): linked automation (package / entry point, an
// Orchestrator reference), a test set's Orchestrator execution settings (folder, robot, machine,
// environment variables), the project's default Orchestrator folder. Not copied, without a
// warning: custom fields (not yet), executions and their results, attachments, defects, and the
// project settings the API cannot set (maximum number of test steps, governance).
//
// Errors are reported and the run continues, as in OrchProvider.CopyItem: an item that fails to
// be created is absent from the id map, so its steps, assignments and labels are skipped with it.
// =============================================================================

internal sealed class TmCopyContext(IWritableHost host,
    OrchTmDriveInfo srcDrive, TmProject srcProject,
    OrchTmDriveInfo dstDrive, TmProject dstProject,
    bool confirmed, bool force, CancellationToken token)
{
    public IWritableHost Host { get; } = host;
    public OrchTmDriveInfo SrcDrive { get; } = srcDrive;
    public TmProject SrcProject { get; } = srcProject;
    public OrchTmDriveInfo DstDrive { get; } = dstDrive;
    public TmProject DstProject { get; } = dstProject;

    // True when an ancestor (Copy-Item's "Copy Project") already confirmed the whole run; the
    // stages then do not ask per item. False under the Copy-Tm* cmdlets, which ask per item.
    public bool Confirmed { get; } = confirmed;

    // -Force: an item whose key cannot be paired 1:1 is created instead of skipped (TmCopy.Decide).
    public bool Force { get; } = force;
    public CancellationToken Token { get; } = token;

    // Source id -> destination id, for every pair known in this run (created or paired by key).
    public Dictionary<string, string> TestCases { get; } = [];
    public Dictionary<string, string> Requirements { get; } = [];
    public Dictionary<string, string> TestSets { get; } = [];

    // Test cases whose steps the step stage copies: created by this run, or paired with a
    // destination test case that has no steps.
    public List<(TmTestCase src, string dstId)> StepTargets { get; } = [];

    // Source test cases this run skipped because their key could not be paired 1:1 (already
    // warned about), so the assignment stage does not report them a second time.
    public HashSet<string> SkippedTestCases { get; } = [];

    // The objects this run copied or paired WITH the operator's consent; the label stage reads
    // their labels. A test case only key-mapped for a test set's assignments is not here.
    public List<(string objectType, string srcId, string dstId, string srcPath)> LabelTargets { get; } = [];

    // Which destination lists this run changed, so the caller can drop exactly those caches.
    public bool DstTestCasesChanged, DstRequirementsChanged, DstTestSetsChanged, DstLabelsChanged;

    public string DstPath => DstProject.GetPSPath();

    public bool ShouldProcess(string srcItemPath, string action)
        => ShouldProcess(srcItemPath, action, out _);

    public bool ShouldProcess(string srcItemPath, string action, out ShouldProcessReason reason)
    {
        reason = ShouldProcessReason.None;
        return Confirmed || Host.ShouldProcess($"Item: '{srcItemPath}' Destination: '{DstPath}'", action, out reason);
    }

    // Under -WhatIf a test case is not created, so it has no destination id; this stands in for
    // one in ctx.TestCases so the assignment stage can still preview the additions. It is never
    // sent: under -WhatIf the addition's own ShouldProcess is false as well.
    public static string WhatIfId(string srcId) => "whatif:" + srcId;

    public void ClearDstCaches()
    {
        if (DstTestCasesChanged) DstDrive.TmTestCases.ClearCache(DstProject.id);
        if (DstRequirementsChanged) DstDrive.TmRequirements.ClearCache(DstProject.id);
        if (DstTestSetsChanged) DstDrive.TmTestSets.ClearCache(DstProject.id);
        if (DstLabelsChanged) DstDrive.TmLabels.ClearCache(DstProject.id);
        DstDrive.TmTestSteps.ClearCache(DstProject.id);
    }
}

// The identity an item is matched by across projects (see the header): the trimmed name, compared
// and the trimmed foreignReference ("" when absent; test sets have none), both compared exactly --
// the record's own (ordinal) equality. The server trims both when it stores them. Case is not
// ignored: Test Manager does not keep names unique, so there is no server rule to follow, and a
// copy writes the name as is, so an earlier copy always matches exactly; ignoring case would only
// pair a source "Login" silently with an unrelated destination "login", and would make a source
// holding both "Login" and "login" a 2:2 key that can never be paired again.
internal readonly record struct TmCopyKey(string Name, string Reference)
{
    public static TmCopyKey Of(string? name, string? reference) => new((name ?? "").Trim(), (reference ?? "").Trim());
}

internal static class TmCopy
{
    internal const string TestCaseType = "testCase";
    internal const string RequirementType = "requirement";
    internal const string TestSetType = "testSet";

    // ----- pairing -------------------------------------------------------------

    internal static TmCopyKey KeyOf(TmTestCase e) => TmCopyKey.Of(e.name, e.foreignReference);
    internal static TmCopyKey KeyOf(TmRequirement e) => TmCopyKey.Of(e.name, e.foreignReference);
    internal static TmCopyKey KeyOf(TmTestSet e) => TmCopyKey.Of(e.name, null);
    internal static TmCopyKey KeyOf(TmAssignmentContainer e) => TmCopyKey.Of(e.Name, e.ForeignReference);

    private static ILookup<TmCopyKey, T> ByKey<T>(IEnumerable<T> items, Func<T, TmCopyKey> key)
        => items.ToLookup(key);

    // The items whose key is used once on each side, source item -> destination item.
    private static IEnumerable<(T src, T dst)> UniquePairs<T>(IEnumerable<T> src, IEnumerable<T> dst, Func<T, TmCopyKey> key)
    {
        var dstByKey = ByKey(dst, key);
        foreach (var g in ByKey(src, key))
        {
            if (g.Count() != 1) continue;
            var matches = dstByKey[g.Key].ToList();
            if (matches.Count == 1) yield return (g.First(), matches[0]);
        }
    }

    internal enum Pairing { Create, Paired, Skip }

    // What to do with the items of one key, from how many the source (>= 1) and the destination
    // have; see the header. -Force turns a Skip into a Create, never a Paired.
    internal static Pairing Decide(int srcCount, int dstCount, bool force)
        => dstCount == 0 ? Pairing.Create
        : srcCount == 1 && dstCount == 1 ? Pairing.Paired
        : force ? Pairing.Create
        : Pairing.Skip;

    // Decides one source item. The skip warning is per key, once per run (warned).
    private static Pairing Pair<T>(TmCopyContext ctx, string label, TmCopyKey key, string srcPath,
        ILookup<TmCopyKey, T> srcByKey, ILookup<TmCopyKey, T> dstByKey, HashSet<TmCopyKey> warned, out T? paired)
        where T : class
    {
        paired = null;
        int srcCount = Math.Max(1, srcByKey[key].Count());
        var dstMatches = dstByKey[key].ToList();
        var pairing = Decide(srcCount, dstMatches.Count, ctx.Force);
        if (pairing == Pairing.Paired) paired = dstMatches[0];
        if (pairing == Pairing.Skip && warned.Add(key))
        {
            string same = key.Reference.Length == 0 ? $"the name '{key.Name}'" : $"the name '{key.Name}' and the foreign reference '{key.Reference}'";
            ctx.Host.WriteWarning(srcCount == 1
                ? $"\"{srcPath}\": {dstMatches.Count} {label}s at {ctx.DstPath} have {same}, so it is not known which one this is; it is not copied. To copy it as a new {label}, use -Force."
                : $"\"{ctx.SrcProject.GetPSPath()}\": {srcCount} {label}s here and {dstMatches.Count} at {ctx.DstPath} have {same}, so it is not known which is which; they are not copied. To copy them as new {label}s, use -Force.");
        }
        return pairing;
    }

    // Key-maps the test cases (key unique on both sides) into ctx.TestCases, for Copy-TmRequirement
    // / Copy-TmTestSet, which re-create assignments without copying test cases. Existing entries win.
    internal static void MapTestCasesByKey(TmCopyContext ctx)
    {
        foreach (var (src, dst) in UniquePairs(ctx.SrcDrive.TmTestCases.Get(ctx.SrcProject), ctx.DstDrive.TmTestCases.Get(ctx.DstProject), KeyOf))
        {
            ctx.TestCases.TryAdd(src.id!, dst.id!);
        }
    }

    // ----- test cases ----------------------------------------------------------

    internal static void CopyTestCases(TmCopyContext ctx, List<WildcardPattern>? wpName, ProgressReporter reporter)
    {
        var host = ctx.Host;
        string msg = "Copying test cases";
        List<TmTestCase> srcAll, srcTestCases, dstAll;
        try
        {
            srcAll = ctx.SrcDrive.TmTestCases.Get(ctx.SrcProject);
            srcTestCases = srcAll.FilterByWildcards(e => e?.name, wpName).OrderBy(e => e.objKey!, ObjKeyComparer.Instance).ToList();
            dstAll = ctx.DstDrive.TmTestCases.Get(ctx.DstProject);
        }
        catch (Exception ex)
        {
            host.WriteError(new ErrorRecord(new OrchException(ctx.SrcProject.GetPSPath(), msg, ex), "GetTmTestCaseError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            return;
        }

        var srcByKey = ByKey(srcAll, KeyOf);
        var dstByKey = ByKey(dstAll, KeyOf);
        var warned = new HashSet<TmCopyKey>();
        int linkedAutomation = 0;

        reporter.TotalNum = srcTestCases.Count;
        int index = 0;
        foreach (var src in srcTestCases.WithCancellation(ctx.Token))
        {
            string srcPath = src.GetPSPath();
            var pairing = Pair(ctx, "test case", KeyOf(src), srcPath, srcByKey, dstByKey, warned, out var paired);
            if (pairing == Pairing.Skip)
            {
                ctx.SkippedTestCases.Add(src.id!);
                continue;
            }
            if (!ctx.ShouldProcess(srcPath, "Copy TmTestCase", out var reason))
            {
                if (reason == ShouldProcessReason.WhatIf)
                {
                    ctx.TestCases[src.id!] = paired?.id ?? TmCopyContext.WhatIfId(src.id!);
                }
                continue;
            }

            reporter.WriteProgress(++index, src.name);
            if (src.hasLinkedAutomation == true) ++linkedAutomation;

            if (paired is not null)
            {
                ctx.TestCases[src.id!] = paired.id!;
                ctx.LabelTargets.Add((TestCaseType, src.id!, paired.id!, srcPath));
                try
                {
                    // Steps only into a paired test case that has none: one that has steps is
                    // the operator's (or an earlier complete run's), and is left alone.
                    if (ctx.DstDrive.OrchAPISession.GetTmTestSteps(ctx.DstProject.id!, paired.id!) is not { Length: > 0 })
                    {
                        ctx.StepTargets.Add((src, paired.id!));
                    }
                }
                catch (Exception ex)
                {
                    host.WriteError(new ErrorRecord(new OrchException(srcPath, "The steps of the destination test case could not be read; its steps are not copied.", ex), "GetTmTestStepError", ErrorCategory.InvalidOperation, src));
                }
                continue;
            }

            TmTestCase? created;
            try
            {
                created = ctx.DstDrive.OrchAPISession.PostTmTestCase(ctx.DstProject.id!, new TmCreateTestCaseRequest
                {
                    name = src.name,
                    description = src.description,
                    version = src.version,
                    // The user's external reference, carried over as is (see the header).
                    // connectorTestCaseId is the source's connector link, not the destination's.
                    foreignReference = src.foreignReference,
                    projectId = ctx.DstProject.id,
                });
                ctx.DstTestCasesChanged = true;
            }
            catch (Exception ex)
            {
                host.WriteError(new ErrorRecord(new OrchException(srcPath, ex), "CopyTmTestCaseError", ErrorCategory.InvalidOperation, src));
                continue;
            }
            if (created?.id is null) continue;

            ctx.TestCases[src.id!] = created.id;
            ctx.LabelTargets.Add((TestCaseType, src.id!, created.id, srcPath));
            ctx.StepTargets.Add((src, created.id));

            // The create request has no pre-/post-condition; they are set by the same PATCH
            // Update-TmTestCase uses.
            var operations = new List<TmPatchOperation>();
            TmPatch.ReplaceIfChanged(operations, "/preCondition", src.preCondition, null);
            TmPatch.ReplaceIfChanged(operations, "/postCondition", src.postCondition, null);
            if (operations.Count > 0)
            {
                try
                {
                    ctx.DstDrive.OrchAPISession.PatchTmTestCase(ctx.DstProject.id!, created.id, operations);
                }
                catch (Exception ex)
                {
                    host.WriteError(new ErrorRecord(new OrchException(srcPath, "The test case was copied, but its pre-/post-condition could not be set.", ex), "CopyTmTestCaseConditionError", ErrorCategory.InvalidOperation, src));
                }
            }
        }

        if (linkedAutomation > 0)
        {
            host.WriteWarning($"\"{ctx.SrcProject.GetPSPath()}\": {linkedAutomation} test case(s) have linked automation, which is not copied. Link the automation at the destination again.");
        }
    }

    // ----- test steps ----------------------------------------------------------

    // Copies the steps of every test case in ctx.StepTargets, appending in the source order. Test
    // cases run in parallel; the steps of one test case are sequential (their order is the point).
    internal static void CopyTestSteps(TmCopyContext ctx, ProgressReporter reporter)
    {
        if (ctx.StepTargets.Count == 0) return;
        var srcDrive = ctx.SrcDrive;
        var api = ctx.DstDrive.OrchAPISession;
        string dstProjectId = ctx.DstProject.id!;

        using var results = OrchThreadPool.RunForEach(ctx.StepTargets,
            t => t.src.GetPSPath(),
            t => t.src.GetPSPath(),
            t =>
            {
                var steps = srcDrive.TmTestSteps.Get(ctx.SrcProject, t.src);
                // No cancellation check between the steps: a Ctrl+C stops the run between test
                // cases (GetResultWithProgress), never in the middle of one test case's steps.
                foreach (var step in steps)
                {
                    api.PostTmTestStep(dstProjectId, new TmCreateTestStepRequest
                    {
                        testCaseId = t.dstId,
                        orderNo = -1, // append
                        actionType = step.actionType,
                        description = step.description ?? "",
                        expectedResult = step.expectedResult,
                        clipboardData = step.clipboardData,
                    });
                }
                return steps.Count;
            });

        reporter.TotalNum = results.Count;
        foreach (var result in results)
        {
            try
            {
                results.GetResultWithProgress(result, reporter, ctx.Token);
            }
            catch (OrchException ex)
            {
                string path = ex.Target as string ?? "";
                ctx.Host.WriteError(new ErrorRecord(new OrchException(path, "The steps of the test case were not all copied", ex.InnerException ?? ex), "CopyTmTestStepError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    // ----- requirements --------------------------------------------------------

    internal static void CopyRequirements(TmCopyContext ctx, List<WildcardPattern>? wpName, ProgressReporter reporter)
    {
        var host = ctx.Host;
        List<TmRequirement> srcAll, srcItems, dstAll;
        try
        {
            srcAll = ctx.SrcDrive.TmRequirements.Get(ctx.SrcProject);
            srcItems = srcAll.FilterByWildcards(e => e?.name, wpName).OrderBy(e => e.objKey!, ObjKeyComparer.Instance).ToList();
            dstAll = ctx.DstDrive.TmRequirements.Get(ctx.DstProject);
        }
        catch (Exception ex)
        {
            host.WriteError(new ErrorRecord(new OrchException(ctx.SrcProject.GetPSPath(), "Copying requirements", ex), "GetTmRequirementError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            return;
        }

        var srcByKey = ByKey(srcAll, KeyOf);
        var dstByKey = ByKey(dstAll, KeyOf);
        var warned = new HashSet<TmCopyKey>();

        reporter.TotalNum = srcItems.Count;
        int index = 0;
        foreach (var src in srcItems.WithCancellation(ctx.Token))
        {
            string srcPath = src.GetPSPath();
            var pairing = Pair(ctx, "requirement", KeyOf(src), srcPath, srcByKey, dstByKey, warned, out var paired);
            if (pairing == Pairing.Skip) continue;
            if (!ctx.ShouldProcess(srcPath, "Copy TmRequirement")) continue;

            reporter.WriteProgress(++index, src.name);
            string? dstId = paired?.id;
            if (dstId is null)
            {
                try
                {
                    // foreignReference is carried over as is (see the header);
                    // connectorRequirementId is the source's connector link, not the destination's.
                    dstId = ctx.DstDrive.OrchAPISession.PostTmRequirement(ctx.DstProject.id!, new TmCreateRequirementRequest
                    {
                        name = src.name,
                        description = src.description,
                        foreignReference = src.foreignReference,
                        projectId = ctx.DstProject.id,
                    })?.id;
                    ctx.DstRequirementsChanged = true;
                }
                catch (Exception ex)
                {
                    host.WriteError(new ErrorRecord(new OrchException(srcPath, ex), "CopyTmRequirementError", ErrorCategory.InvalidOperation, src));
                    continue;
                }
                if (dstId is null) continue;
            }

            ctx.Requirements[src.id!] = dstId;
            ctx.LabelTargets.Add((RequirementType, src.id!, dstId, srcPath));
        }
    }

    // ----- test sets -----------------------------------------------------------

    internal static void CopyTestSets(TmCopyContext ctx, List<WildcardPattern>? wpName, ProgressReporter reporter)
    {
        var host = ctx.Host;
        List<TmTestSet> srcAll, srcItems, dstAll;
        try
        {
            srcAll = ctx.SrcDrive.TmTestSets.Get(ctx.SrcProject);
            srcItems = srcAll.FilterByWildcards(e => e?.name, wpName).OrderBy(e => e.objKey!, ObjKeyComparer.Instance).ToList();
            dstAll = ctx.DstDrive.TmTestSets.Get(ctx.DstProject);
        }
        catch (Exception ex)
        {
            host.WriteError(new ErrorRecord(new OrchException(ctx.SrcProject.GetPSPath(), "Copying test sets", ex), "GetTmTestSetError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            return;
        }

        var srcByKey = ByKey(srcAll, KeyOf);
        var dstByKey = ByKey(dstAll, KeyOf);
        var warned = new HashSet<TmCopyKey>();

        reporter.TotalNum = srcItems.Count;
        int index = 0;
        foreach (var src in srcItems.WithCancellation(ctx.Token))
        {
            string srcPath = src.GetPSPath();
            var pairing = Pair(ctx, "test set", KeyOf(src), srcPath, srcByKey, dstByKey, warned, out var paired);
            if (pairing == Pairing.Skip) continue;
            if (!ctx.ShouldProcess(srcPath, "Copy TmTestSet")) continue;

            reporter.WriteProgress(++index, src.name);
            string? dstId = paired?.id;
            if (dstId is null)
            {
                try
                {
                    dstId = ctx.DstDrive.OrchAPISession.PostTmTestSet(ctx.DstProject.id!, new TmCreateTestSetRequest
                    {
                        name = src.name,
                        description = src.description,
                        enableCoverage = src.enableCoverage,
                        projectId = ctx.DstProject.id,
                    })?.id;
                    ctx.DstTestSetsChanged = true;
                }
                catch (Exception ex)
                {
                    host.WriteError(new ErrorRecord(new OrchException(srcPath, ex), "CopyTmTestSetError", ErrorCategory.InvalidOperation, src));
                    continue;
                }
                if (dstId is null) continue;

                var dropped = UncopiedExecutionSettings(src);
                if (dropped.Count > 0)
                {
                    host.WriteWarning($"\"{srcPath}\": the execution settings {string.Join(", ", dropped)} refer to the source's Orchestrator and are not copied. Set them at the destination again.");
                }
            }

            ctx.TestSets[src.id!] = dstId;
            ctx.LabelTargets.Add((TestSetType, src.id!, dstId, srcPath));
        }
    }

    // The test set settings that point into the source's Orchestrator tenant.
    private static List<string> UncopiedExecutionSettings(TmTestSet src)
    {
        var dropped = new List<string>();
        if (!string.IsNullOrEmpty(src.folderKey)) dropped.Add("folder");
        if (!string.IsNullOrEmpty(src.robotName) || !string.IsNullOrEmpty(src.robotUserKey)) dropped.Add("robot");
        if (!string.IsNullOrEmpty(src.machineKey) || !string.IsNullOrEmpty(src.hostMachineName)) dropped.Add("machine");
        if (src.enableEnvironmentVariables == true) dropped.Add("environment variables");
        if (!string.IsNullOrEmpty(src.executionNameFormatOverride)) dropped.Add("execution name format");
        return dropped;
    }

    // ----- assignments ---------------------------------------------------------

    // The containers (test sets or requirements) whose key is used once in the source and once in
    // the destination, source id -> destination id. Copy-TmTestCase uses it to add the test cases
    // it copied to the destination's counterparts of the containers they belong to.
    internal static Dictionary<string, string> MapContainersByKey(TmCopyContext ctx, TmTestCaseAssignmentKind kind)
        => UniquePairs(kind.GetContainers(ctx.SrcDrive, ctx.SrcProject), kind.GetContainers(ctx.DstDrive, ctx.DstProject), KeyOf)
            .ToDictionary(p => p.src.Id, p => p.dst.Id);

    // Re-creates the test case assignments of the given containers (test set or requirement),
    // through the id maps, with one assign call per destination container.
    //
    // Two modes. Copy-Item / Copy-TmTestSet / Copy-TmRequirement copied the CONTAINERS: every
    // assigned test case should follow, and the ones with no counterpart at the destination are
    // counted and reported once per container. Copy-TmTestCase (copiedTestCasesOnly) copied TEST
    // CASES into existing containers: only those test cases are added, the container's other test
    // cases are not this run's business (no warning), and since the container is not something
    // the operator confirmed, each container's addition is confirmed on its own.
    internal static void CopyAssignments(TmCopyContext ctx, TmTestCaseAssignmentKind kind, Dictionary<string, string> containers, ProgressReporter reporter, bool copiedTestCasesOnly = false)
    {
        if (containers.Count == 0) return;
        var srcApi = ctx.SrcDrive.OrchAPISession;
        var dstApi = ctx.DstDrive.OrchAPISession;
        string srcProjectId = ctx.SrcProject.id!, dstProjectId = ctx.DstProject.id!;
        var srcNames = kind.GetContainers(ctx.SrcDrive, ctx.SrcProject).ToDictionary(c => c.Id, c => c.PSPath);
        var dstNames = copiedTestCasesOnly ? kind.GetContainers(ctx.DstDrive, ctx.DstProject).ToDictionary(c => c.Id, c => c.PSPath) : [];
        var srcTestCaseNames = copiedTestCasesOnly ? ctx.SrcDrive.TmTestCases.Get(ctx.SrcProject).Where(e => e.id is not null).ToDictionary(e => e.id!, e => e.name) : [];

        // Both sides read in parallel; the destination side is empty for a container just created,
        // but reading it is what keeps a re-run from sending what is already there.
        using var results = OrchThreadPool.RunForEach(containers.ToList(),
            p => srcNames.GetValueOrDefault(p.Key) ?? p.Key,
            p => srcNames.GetValueOrDefault(p.Key) ?? p.Key,
            p => (src: kind.GetAssigned(srcApi, srcProjectId, p.Key) ?? [], dst: kind.GetAssigned(dstApi, dstProjectId, p.Value) ?? []));

        reporter.TotalNum = results.Count;
        foreach (var result in results)
        {
            (string[] srcAssigned, string[] dstAssigned) assigned;
            try
            {
                assigned = results.GetResultWithProgress(result, reporter, ctx.Token);
            }
            catch (OrchException ex)
            {
                string failed = ex.Target as string ?? "";
                ctx.Host.WriteError(new ErrorRecord(new OrchException(failed, $"The test cases assigned to the {kind.ContainerLabel} could not be read", ex.InnerException ?? ex), "CopyTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, failed));
                continue;
            }

            var (srcContainerId, dstContainerId) = result.Source;
            string path = srcNames.GetValueOrDefault(srcContainerId) ?? srcContainerId;
            try
            {
                var (srcAssigned, dstAssigned) = assigned;

                var already = dstAssigned.ToHashSet();
                var toAssign = new List<(string srcId, string dstId)>();
                int missing = 0;
                foreach (var srcTestCaseId in srcAssigned)
                {
                    if (!ctx.TestCases.TryGetValue(srcTestCaseId, out var dstTestCaseId))
                    {
                        if (!ctx.SkippedTestCases.Contains(srcTestCaseId)) ++missing;
                        continue;
                    }
                    if (!already.Contains(dstTestCaseId) && !toAssign.Any(t => t.dstId == dstTestCaseId)) toAssign.Add((srcTestCaseId, dstTestCaseId));
                }
                if (missing > 0 && !copiedTestCasesOnly)
                {
                    ctx.Host.WriteWarning($"\"{path}\": {missing} assigned test case(s) have no counterpart at {ctx.DstPath} (not copied, or not unique there by name and foreign reference); they are not assigned.");
                }
                if (toAssign.Count == 0) continue;

                if (copiedTestCasesOnly)
                {
                    string dstPath = dstNames.GetValueOrDefault(dstContainerId) ?? dstContainerId;
                    string names = string.Join(", ", toAssign.Select(t => srcTestCaseNames.GetValueOrDefault(t.srcId) ?? t.srcId));
                    if (!ctx.Host.ShouldProcess(dstPath, $"Add test cases: {names}")) continue;
                }
                // Belt and braces: a -WhatIf stand-in id must never reach the server.
                string[] ids = [.. toAssign.Select(t => t.dstId).Where(id => !id.StartsWith("whatif:", StringComparison.Ordinal))];
                if (ids.Length > 0) kind.Assign(dstApi, dstProjectId, dstContainerId, ids);
            }
            catch (Exception ex)
            {
                ctx.Host.WriteError(new ErrorRecord(new OrchException(path, $"The test cases assigned to the {kind.ContainerLabel} could not be copied", ex), "CopyTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    // ----- project settings ----------------------------------------------------

    // The time zone and the execution name format, the only settings the API lets be set. Each is
    // copied only while the destination still has its default (UTC / none), so a setting changed
    // at the destination by hand is never overwritten, and a re-run still completes a copy that
    // stopped before this stage. The default Orchestrator folder points into the source's
    // tenant: not copied, with a warning while the destination has none.
    internal static void CopyProjectSettings(TmCopyContext ctx, ProgressReporter reporter)
    {
        var host = ctx.Host;
        string srcPath = ctx.SrcProject.GetPSPath();
        var dstApi = ctx.DstDrive.OrchAPISession;
        string dstProjectId = ctx.DstProject.id!;
        TmProjectSettings? src, dst;
        try
        {
            src = ctx.SrcDrive.OrchAPISession.GetTmProjectSettings(ctx.SrcProject.id!);
            dst = dstApi.GetTmProjectSettings(dstProjectId);
        }
        catch (Exception ex)
        {
            host.WriteError(new ErrorRecord(new OrchException(srcPath, "The project settings could not be read, so they are not copied.", ex), "GetTmProjectSettingsError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            return;
        }
        if (src is null || dst is null) return;

        bool timeZone = !string.IsNullOrEmpty(src.projectTimeZone) && !IsDefaultTimeZone(src.projectTimeZone)
            && IsDefaultTimeZone(dst.projectTimeZone);
        bool nameFormat = !string.IsNullOrEmpty(src.executionNameFormat) && string.IsNullOrEmpty(dst.executionNameFormat);
        reporter.TotalNum = (timeZone ? 1 : 0) + (nameFormat ? 1 : 0);
        int index = 0;

        if (timeZone && ctx.ShouldProcess(srcPath, $"Copy project time zone: {src.projectTimeZone}"))
        {
            reporter.WriteProgress(++index, "time zone");
            try
            {
                dstApi.PutTmProjectSettings(dstProjectId, new TmUpdateProjectSettingsRequest { projectTimeZone = src.projectTimeZone });
            }
            catch (Exception ex)
            {
                host.WriteError(new ErrorRecord(new OrchException(srcPath, $"The project time zone '{src.projectTimeZone}' could not be set at {ctx.DstPath}.", ex), "CopyTmProjectSettingsError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            }
        }
        if (nameFormat && ctx.ShouldProcess(srcPath, $"Copy project execution name format: {src.executionNameFormat}"))
        {
            reporter.WriteProgress(++index, "execution name format");
            try
            {
                dstApi.PatchTmProjectSettings(dstProjectId, [new TmPatchOperation { op = "replace", path = "/executionNameFormat", value = src.executionNameFormat }]);
            }
            catch (Exception ex)
            {
                host.WriteError(new ErrorRecord(new OrchException(srcPath, $"The project execution name format '{src.executionNameFormat}' could not be set at {ctx.DstPath}.", ex), "CopyTmProjectSettingsError", ErrorCategory.InvalidOperation, ctx.SrcProject));
            }
        }
        if (!string.IsNullOrEmpty(src.folderKey) && string.IsNullOrEmpty(dst.folderKey))
        {
            host.WriteWarning($"\"{srcPath}\": the project's default Orchestrator folder refers to the source's Orchestrator and is not copied. Set it at {ctx.DstPath} again.");
        }
        ctx.DstDrive.TmProjectSetting.ClearCache(dstProjectId);
    }

    // A new project's time zone is UTC.
    private static bool IsDefaultTimeZone(string? timeZone)
        => string.IsNullOrEmpty(timeZone) || string.Equals(timeZone, "UTC", StringComparison.OrdinalIgnoreCase);

    // ----- labels --------------------------------------------------------------

    // Reads the labels of the objects in ctx.LabelTargets (of one object type, when given; one
    // call each: the bulk read returns only the union of the names), then adds them at the
    // destination with one addlabels call per object type and label set. Adding is idempotent,
    // so a paired object gets the source's labels on top of its own.
    internal static void CopyLabels(TmCopyContext ctx, ProgressReporter reporter, string? onlyType = null)
    {
        var targets = onlyType is null ? ctx.LabelTargets : ctx.LabelTargets.Where(t => t.objectType == onlyType).ToList();
        if (targets.Count == 0) return;
        var srcApi = ctx.SrcDrive.OrchAPISession;
        string srcProjectId = ctx.SrcProject.id!;

        using var results = OrchThreadPool.RunForEach(targets,
            t => t.srcPath,
            t => t.srcPath,
            t => srcApi.GetTmObjectLabels(srcProjectId, t.objectType, t.srcId) ?? []);

        var groups = new Dictionary<(string objectType, string key), (string[] labels, List<string> dstIds)>();
        reporter.TotalNum = results.Count;
        foreach (var result in results)
        {
            try
            {
                var labels = results.GetResultWithProgress(result, reporter, ctx.Token)!
                    .Select(l => l.name).Where(n => !string.IsNullOrEmpty(n)).Select(n => n!)
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
                if (labels.Length == 0) continue;
                var (objectType, _, dstId, _) = result.Source;
                var key = (objectType, string.Join('\n', labels));
                if (!groups.TryGetValue(key, out var group))
                {
                    groups[key] = group = (labels, []);
                }
                group.dstIds.Add(dstId);
            }
            catch (OrchException ex)
            {
                string path = ex.Target as string ?? "";
                ctx.Host.WriteError(new ErrorRecord(new OrchException(path, "The labels could not be read, so they are not copied", ex.InnerException ?? ex), "GetTmObjectLabelError", ErrorCategory.InvalidOperation, path));
            }
        }

        var dstApi = ctx.DstDrive.OrchAPISession;
        foreach (var ((objectType, _), (labels, dstIds)) in groups.WithCancellation(ctx.Token))
        {
            foreach (var chunk in dstIds.Chunk(100).WithCancellation(ctx.Token))
            {
                try
                {
                    dstApi.AddTmObjectLabels(ctx.DstProject.id!, objectType, chunk, labels);
                    ctx.DstLabelsChanged = true;
                }
                catch (Exception ex)
                {
                    ctx.Host.WriteError(new ErrorRecord(new OrchException(ctx.DstPath, $"The labels {TmLabelCsv.Format(labels)} could not be added to {chunk.Length} {objectType} object(s).", ex), "CopyTmObjectLabelError", ErrorCategory.InvalidOperation, ctx.DstPath));
                }
            }
        }
    }
}

// Copy-TmTestCase / Copy-TmRequirement / Copy-TmTestSet: the selected entities of each source
// project into ONE destination project. Each item is confirmed on its own (ShouldProcess per
// item), unlike Copy-Item, which confirms the project once.
public abstract class CopyTmEntityCmdletBase : OrchestratorPSCmdlet
{
    protected abstract string[]? NamePatterns { get; }
    protected abstract string? DestinationPath { get; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Create the items whose name (and foreign reference) cannot be paired 1:1 with the
    // destination's, instead of skipping them (TmCopy.Decide).
    [Parameter]
    public SwitchParameter Force { get; set; }

    internal abstract void Copy(TmCopyContext ctx, List<WildcardPattern>? wpName);

    protected override void ProcessRecord()
    {
        var dsts = SessionState.EnumTmFolders([DestinationPath]);
        if (dsts.Count != 1)
        {
            ThrowTerminatingError(new ErrorRecord(new ArgumentException($"-Destination must name one project, but '{DestinationPath}' names {dsts.Count}."), "TmCopyDestinationNotSingle", ErrorCategory.InvalidArgument, DestinationPath));
            return;
        }
        var (dstDrive, dstProject) = dsts[0];
        var srcs = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath));
        var wpName = NamePatterns.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (srcDrive, srcProject) in srcs.WithCancellation(cancelHandler.Token))
        {
            if (srcProject.id == dstProject.id)
            {
                WriteError(new ErrorRecord(new OrchException(srcProject.GetPSPath(), "Cannot copy into the same project."), "TmCopyToItselfError", ErrorCategory.InvalidOperation, srcProject));
                continue;
            }

            var ctx = new TmCopyContext(this, srcDrive, srcProject, dstDrive, dstProject, false, Force.IsPresent, cancelHandler.Token);
            try
            {
                Copy(ctx, wpName);
            }
            finally
            {
                ctx.ClearDstCaches();
            }
        }
    }
}
