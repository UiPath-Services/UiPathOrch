using System.Management.Automation;
using System.Text;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Test cases assigned to a test set or a requirement: Get-, Add- and Remove-TmTestSetTestCase and
// -TmRequirementTestCase. The two relations differ only in the API calls and the names, which the
// kind below supplies; the cmdlets are thin subclasses of the bases at the end of this file.
//
// Names, not ids, identify both ends, so a CSV exported from one tenant imports into another. Test
// Manager allows several test cases (test sets, requirements) with the same name, so a name with no
// wildcard must match exactly one: a name that matches two is an error, never a guess. A wildcard
// pattern selects every match, as on the other Tm cmdlets.
internal abstract class TmTestCaseAssignmentKind
{
    internal abstract string ContainerLabel { get; }   // "test set" / "requirement", for messages
    internal abstract string ContainerColumn { get; }  // "TestSet" / "Requirement": the CSV column and the parameter
    internal abstract IEnumerable<TmAssignmentContainer> GetContainers(OrchTmDriveInfo drive, TmProject project);
    internal abstract string[]? GetAssigned(OrchAPISession api, string projectId, string containerId);
    internal abstract void Assign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds);
    internal abstract void Unassign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds);
    internal abstract object CreateOutput(TmProject project, TmAssignmentContainer container, TmTestCase? testCase, string testCaseId);
}

internal sealed record TmAssignmentContainer(string Id, string? Name, string? ObjKey, string Path)
{
    public string PSPath => System.IO.Path.Combine(Path, Name ?? Id);
}

internal sealed class TmTestSetAssignmentKind : TmTestCaseAssignmentKind
{
    internal static readonly TmTestSetAssignmentKind Instance = new();
    internal override string ContainerLabel => "test set";
    internal override string ContainerColumn => "TestSet";
    internal override IEnumerable<TmAssignmentContainer> GetContainers(OrchTmDriveInfo drive, TmProject project)
        => drive.TmTestSets.Get(project).Select(e => new TmAssignmentContainer(e.id!, e.name, e.objKey, project.GetPSPath()));
    internal override string[]? GetAssigned(OrchAPISession api, string projectId, string containerId)
        => api.GetTmTestSetAssignedTestCaseIds(projectId, containerId);
    internal override void Assign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds)
        => api.AssignTmTestSetTestCases(projectId, containerId, testCaseIds);
    internal override void Unassign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds)
        => api.UnassignTmTestSetTestCases(projectId, containerId, testCaseIds);
    internal override object CreateOutput(TmProject project, TmAssignmentContainer container, TmTestCase? testCase, string testCaseId)
    {
        var output = new TmTestSetTestCase { Path = container.Path, TestSet = container.Name, testSetId = container.Id, testCaseId = testCaseId };
        output.CopyFrom(testCase);
        return output;
    }
}

internal sealed class TmRequirementAssignmentKind : TmTestCaseAssignmentKind
{
    internal static readonly TmRequirementAssignmentKind Instance = new();
    internal override string ContainerLabel => "requirement";
    internal override string ContainerColumn => "Requirement";
    internal override IEnumerable<TmAssignmentContainer> GetContainers(OrchTmDriveInfo drive, TmProject project)
        => drive.TmRequirements.Get(project).Select(e => new TmAssignmentContainer(e.id!, e.name, e.objKey, project.GetPSPath()));
    internal override string[]? GetAssigned(OrchAPISession api, string projectId, string containerId)
        => api.GetTmRequirementAssignedTestCaseIds(projectId, containerId);
    internal override void Assign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds)
        => api.AssignTmRequirementTestCases(projectId, containerId, testCaseIds);
    internal override void Unassign(OrchAPISession api, string projectId, string containerId, string[] testCaseIds)
        => api.UnassignTmRequirementTestCases(projectId, containerId, testCaseIds);
    internal override object CreateOutput(TmProject project, TmAssignmentContainer container, TmTestCase? testCase, string testCaseId)
    {
        var output = new TmRequirementTestCase { Path = container.Path, Requirement = container.Name, requirementId = container.Id, testCaseId = testCaseId };
        output.CopyFrom(testCase);
        return output;
    }
}

internal static class TmTestCaseAssignment
{
    // The items a pattern names. A pattern with no wildcard is a name and must match exactly one
    // item (ignoring case); one with a wildcard selects every match. The error is null when the
    // pattern resolved, else what went wrong ("no ... named", "... named X are N").
    // `scope` qualifies the items in the message, e.g. " assigned to Tm:\P\Smoke".
    internal static List<T> Resolve<T>(IEnumerable<T> items, Func<T, string?> getName, string pattern, string label, out string? error, string scope = "")
    {
        error = null;
        if (WildcardPattern.ContainsWildcardCharacters(pattern))
        {
            var wp = new WildcardPattern(pattern, WildcardOptions.IgnoreCase);
            return items.Where(e => wp.IsMatch(getName(e))).ToList();
        }

        string name = WildcardPattern.Unescape(pattern);
        var matches = items.Where(e => string.Equals(getName(e), name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            error = $"There is no {label}{scope} named '{name}'.";
        }
        else if (matches.Count > 1)
        {
            error = $"There are {matches.Count} {label}s{scope} named '{name}'; rename them, or select them with a wildcard.";
            matches.Clear();
        }
        return matches;
    }

    // Names used by more than one item (ignoring case): an export that writes one of them cannot be
    // imported back, because the name does not say which item it was.
    internal static HashSet<string> DuplicateNames<T>(IEnumerable<T> items, Func<T, string?> getName)
        => items.Select(getName).Where(n => n is not null)
            .GroupBy(n => n!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)
            .Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public abstract class GetTmTestCaseAssignmentCmdletBase : OrchestratorPSCmdlet
{
    internal abstract TmTestCaseAssignmentKind Kind { get; }
    protected abstract string[]? ContainerPatterns { get; }
    protected abstract string[]? TestCasePatterns { get; }
    protected abstract string? ExportCsvPath { get; }
    protected abstract Encoding? ExportCsvEncoding { get; }
    protected abstract string DefaultCsvName { get; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpContainer = ContainerPatterns.ConvertToWildcardPatternList();
        var wpTestCase = TestCasePatterns.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsvPath, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, ExportCsvEncoding, ["Path", Kind.ContainerColumn, "TestCase"]);

        var containers = new List<(OrchTmDriveInfo drive, TmProject project, TmAssignmentContainer container)>();
        foreach (var (drive, project) in drivesProjects)
        {
            try
            {
                containers.AddRange(Kind.GetContainers(drive, project)
                    .FilterByWildcards(e => e?.Name, wpContainer)
                    .OrderBy(e => e.ObjKey!, ObjKeyComparer.Instance)
                    .Select(c => (drive, project, c)));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, project));
            }
        }

        using var results = OrchThreadPool.RunForEach(containers,
            t => t.container.PSPath,
            t => t.container,
            t => Kind.GetAssigned(t.drive.OrchAPISession, t.project.id!, t.container.Id) ?? []);

        // Warn once per name that the export cannot carry back unambiguously.
        var warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, $"Getting the test cases of each {Kind.ContainerLabel}");
        foreach (var result in results)
        {
            try
            {
                var assignedIds = results.GetResultWithProgress(result, reporter, cancelHandler.Token)!;
                var (drive, project, container) = result.Source;
                var testCases = drive.TmTestCases.Get(project);
                var byId = testCases.Where(e => e.id is not null).ToDictionary(e => e.id!);

                var rows = assignedIds
                    .Select(id => (id, testCase: byId.GetValueOrDefault(id)))
                    .Where(r => wpTestCase is null || wpTestCase.Count == 0 || wpTestCase.Any(p => p.IsMatch(r.testCase?.name)))
                    .ToList();

                if (writer is null)
                {
                    WriteObject(rows.Select(r => Kind.CreateOutput(project, container, r.testCase, r.id)), true);
                    continue;
                }

                var duplicateTestCases = TmTestCaseAssignment.DuplicateNames(testCases, e => e.name);
                var duplicateContainers = TmTestCaseAssignment.DuplicateNames(Kind.GetContainers(drive, project), e => e.Name);
                foreach (var (id, testCase) in rows)
                {
                    if (testCase?.name is null)
                    {
                        WriteWarning($"\"{container.PSPath}\": the assigned test case {id} was not found in the project; it is not exported.");
                        continue;
                    }
                    if (duplicateContainers.Contains(container.Name ?? "") && warned.Add($"{project.id}|c|{container.Name}"))
                    {
                        WriteWarning($"\"{project.GetPSPath()}\": several {Kind.ContainerLabel}s are named '{container.Name}'; importing their rows will fail until the names are made unique.");
                    }
                    if (duplicateTestCases.Contains(testCase.name) && warned.Add($"{project.id}|t|{testCase.name}"))
                    {
                        WriteWarning($"\"{project.GetPSPath()}\": several test cases are named '{testCase.name}'; importing their rows will fail until the names are made unique.");
                    }
                    writer.WriteCsvLine([
                        EscapeCsvValue(container.Path, true),
                        EscapeCsvValue(container.Name, true),
                        EscapeCsvValue(testCase.name, true)
                    ]);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}

// Add- and Remove-: ProcessRecord resolves every record to (container, test cases) and collects
// them; EndProcessing sends one call per container, so a CSV of many rows for one test set becomes
// one request. The test cases already assigned are read once per container, so Add skips the ones
// already there and Remove only the ones that are not, and -WhatIf names exactly what changes.
public abstract class ChangeTmTestCaseAssignmentCmdletBase : OrchestratorPSCmdlet
{
    internal abstract TmTestCaseAssignmentKind Kind { get; }
    protected abstract string[]? ContainerPatterns { get; }
    protected abstract string[]? TestCasePatterns { get; }
    protected abstract bool Adding { get; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private sealed class Pending(OrchTmDriveInfo drive, TmProject project, TmAssignmentContainer container, HashSet<string> assigned)
    {
        public OrchTmDriveInfo Drive { get; } = drive;
        public TmProject Project { get; } = project;
        public TmAssignmentContainer Container { get; } = container;
        public HashSet<string> Assigned { get; } = assigned;
        public List<TmTestCase> Changes { get; } = [];
    }

    // By container id, in the order first seen.
    private readonly Dictionary<string, Pending> _pending = [];
    private readonly List<Pending> _order = [];

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath));
        foreach (var (drive, project) in drivesProjects)
        {
            List<TmAssignmentContainer> allContainers;
            List<TmTestCase> testCases;
            try
            {
                allContainers = Kind.GetContainers(drive, project).ToList();
                testCases = drive.TmTestCases.Get(project);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, project));
                continue;
            }

            var containers = new List<TmAssignmentContainer>();
            foreach (var pattern in ContainerPatterns ?? [])
            {
                containers.AddRange(TmTestCaseAssignment.Resolve(allContainers, e => e.Name, pattern, Kind.ContainerLabel, out var error));
                if (error is not null)
                {
                    WriteError(new ErrorRecord(new ItemNotFoundException($"\"{project.GetPSPath()}\": {error}"), "TmAssignmentContainerNotResolved", ErrorCategory.ObjectNotFound, pattern));
                }
            }

            foreach (var container in containers.DistinctBy(c => c.Id).OrderBy(c => c.ObjKey!, ObjKeyComparer.Instance))
            {
                if (GetPending(drive, project, container) is not { } pending) continue;

                // Remove looks only among the test cases assigned to this container, so a name
                // shared with an unassigned test case is still unambiguous here.
                var candidates = Adding ? testCases : testCases.Where(e => pending.Assigned.Contains(e.id!)).ToList();
                foreach (var pattern in TestCasePatterns ?? [])
                {
                    var matched = TmTestCaseAssignment.Resolve(candidates, e => e.name, pattern, "test case", out var error, Adding ? "" : " assigned to it");
                    if (error is not null)
                    {
                        WriteError(new ErrorRecord(new ItemNotFoundException($"\"{container.PSPath}\": {error}"), "TmAssignmentTestCaseNotResolved", ErrorCategory.ObjectNotFound, pattern));
                    }
                    foreach (var testCase in matched)
                    {
                        bool isAssigned = pending.Assigned.Contains(testCase.id!);
                        if (isAssigned != Adding && !pending.Changes.Any(c => c.id == testCase.id))
                        {
                            pending.Changes.Add(testCase);
                        }
                    }
                }
            }
        }
    }

    private Pending? GetPending(OrchTmDriveInfo drive, TmProject project, TmAssignmentContainer container)
    {
        if (_pending.TryGetValue(container.Id, out var pending)) return pending;
        try
        {
            var assigned = Kind.GetAssigned(drive.OrchAPISession, project.id!, container.Id) ?? [];
            pending = new Pending(drive, project, container, assigned.ToHashSet());
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(container.PSPath, ex), "GetTmTestCaseAssignmentError", ErrorCategory.InvalidOperation, container.PSPath));
            return null;
        }
        _pending[container.Id] = pending;
        _order.Add(pending);
        return pending;
    }

    protected override void EndProcessing()
    {
        string verb = Adding ? "Add" : "Remove";
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var pending in _order
            .Where(p => p.Changes.Count != 0)
            .ToList()
            .WithProgressBar(this, $"{(Adding ? "Assigning" : "Unassigning")} test cases", p => p.Container.PSPath)
            .WithCancellation(cancelHandler.Token))
        {
            var target = pending.Container.PSPath;
            var names = string.Join(", ", pending.Changes.Select(c => c.name));
            if (!ShouldProcess(target, $"{verb} test cases: {names}")) continue;

            var ids = pending.Changes.Select(c => c.id!).ToArray();
            try
            {
                if (Adding)
                    Kind.Assign(pending.Drive.OrchAPISession, pending.Project.id!, pending.Container.Id, ids);
                else
                    Kind.Unassign(pending.Drive.OrchAPISession, pending.Project.id!, pending.Container.Id, ids);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), $"{verb}TmTestCaseAssignmentError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
