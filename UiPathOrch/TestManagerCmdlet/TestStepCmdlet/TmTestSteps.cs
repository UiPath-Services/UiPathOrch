using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Test steps have no name: a step is addressed by its test case and its 0-based position
// (orderNo). Positions are not stable -- the server renumbers the steps on every insert, move
// and delete -- so the write cmdlets read the steps from the server (refresh) before turning
// positions into step ids, and clear the test case's cached steps after changing any of them.
internal static class TmTestSteps
{
    // The steps of one test case, sorted by orderNo and stamped with the project path and the
    // test case's name. Cached per test case (drive.TmTestSteps); refresh reads them from the
    // server even when cached.
    internal static List<TmTestStep> Get(OrchTmDriveInfo drive, TmProject project, TmTestCase testCase, bool refresh = false)
    {
        var steps = refresh ? drive.TmTestSteps.Refresh(project, testCase) : drive.TmTestSteps.Get(project, testCase);
        // Stamped on every read: the test case may have been renamed since the steps were cached.
        foreach (var step in steps)
        {
            step.Path = project.GetPSPath();
            step.TestCase = testCase.name;
        }
        return steps;
    }

    // The test cases named by the patterns, in each project. Get-* passes all = true so that no
    // pattern means every test case; the write cmdlets pass false so that it means none.
    internal static IEnumerable<(OrchTmDriveInfo drive, TmProject project, TmTestCase testCase)> SelectTestCases(
        PSCmdlet cmdlet, IEnumerable<(OrchTmDriveInfo drive, TmProject project)> drivesProjects, List<WildcardPattern>? patterns, bool all)
    {
        foreach (var (drive, project) in drivesProjects)
        {
            IEnumerable<TmTestCase>? testCases = null;
            try
            {
                testCases = drive.TmTestCases.Get(project);
            }
            catch (Exception ex)
            {
                cmdlet.WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseError", ErrorCategory.InvalidOperation, project));
            }
            if (testCases is null) continue;

            var selected = all
                ? testCases.FilterByWildcards(e => e?.name, patterns)
                : testCases.SelectByWildcards(e => e?.name, patterns);
            foreach (var testCase in selected.OrderBy(e => e.objKey!, ObjKeyComparer.Instance))
            {
                yield return (drive, project, testCase);
            }
        }
    }

    // The steps at the given positions (see ResolvePosition), in position order, without
    // duplicates. Positions out of range go to `outOfRange`.
    internal static List<TmTestStep> SelectByPosition(List<TmTestStep> steps, IEnumerable<int> positions, List<int> outOfRange)
    {
        var indexes = new SortedSet<int>();
        foreach (var position in positions)
        {
            if (ResolvePosition(position, steps.Count) is int index)
                indexes.Add(index);
            else
                outOfRange.Add(position);
        }
        return indexes.Select(i => steps[i]).ToList();
    }

    // An existing step's index: 0..count-1, or negative from the end (-1 = the last step).
    // Null when out of range.
    internal static int? ResolvePosition(int position, int count)
    {
        int index = position < 0 ? count + position : position;
        return index >= 0 && index < count ? index : null;
    }

    // Where a new step goes: 0..count (count = after the last step), or negative from the end
    // (-1 = after the last step, -2 = before it). Null when out of range.
    internal static int? ResolveInsertPosition(int position, int count)
    {
        int index = position < 0 ? count + 1 + position : position;
        return index >= 0 && index <= count ? index : null;
    }

    internal static ErrorRecord OutOfRangeError(TmTestCase testCase, int position, int count)
        => new(new ArgumentException($"{testCase.GetPSPath()} has {count} step(s); position {position} is out of range."),
            "TmTestStepOutOfRange", ErrorCategory.ObjectNotFound, position);
}
