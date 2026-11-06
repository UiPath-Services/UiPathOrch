using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Action Center lists tasks with $orderby=Priority desc,CreationTime asc (seen in the browser's
// requests): the most urgent first, and within a priority the one waiting longest. Priority is a
// name here, so it is ranked by meaning, not alphabetically.
public class TaskOrderTests
{
    private static OrchTask T(long id, string? priority, int day)
        => new() { Id = id, Priority = priority, CreationTime = new DateTime(2026, 1, day) };

    [Fact]
    public void Most_urgent_first_then_longest_waiting()
    {
        var tasks = new[]
        {
            T(1, "Low", 1), T(2, "Medium", 5), T(3, "Critical", 9),
            T(4, "Medium", 2), T(5, "High", 3), T(6, "Critical", 4),
        };
        Assert.Equal(new long?[] { 6, 3, 5, 4, 2, 1 }, OrchTask.InActionCenterOrder(tasks).Select(t => t.Id));
    }

    [Fact]
    public void Unknown_priority_and_missing_time_sort_last()
    {
        var tasks = new[]
        {
            T(1, null, 1), T(2, "Low", 2),
            new OrchTask { Id = 3, Priority = "Low", CreationTime = null },
        };
        Assert.Equal(new long?[] { 2, 3, 1 }, OrchTask.InActionCenterOrder(tasks).Select(t => t.Id));
    }
}
