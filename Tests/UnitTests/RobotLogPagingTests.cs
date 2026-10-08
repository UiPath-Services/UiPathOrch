using System.Globalization;
using System.Text.RegularExpressions;
using UiPath.OrchAPI;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchAPISession.PageRobotLogsByTimeStamp, the windowed paging Get-OrchLog uses so that an
// Elasticsearch log store's max_result_window ($skip + $top must stay below it) does not stop the
// listing. The fake store below refuses a page past the window, as Automation Cloud does, and
// applies the anchor clause the pager adds. Small page / window sizes keep the tests fast.
public class RobotLogPagingTests
{
    private const ulong PageSize = 10;
    private const ulong Window = 100;

    private static readonly DateTime Origin = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    // count logs; msOf(i) gives each one's millisecond offset from Origin, so rows can share one.
    private static List<Log> MakeLogs(int count, Func<int, int> msOf) =>
        Enumerable.Range(0, count)
            .Select(i => new Log { Message = $"m{i}", RawMessage = $"{{\"fingerprint\":\"{i}\"}}", TimeStamp = Origin.AddMilliseconds(msOf(i)).AddTicks(i % 7) })
            .ToList();

    private sealed class FakeStore
    {
        private readonly List<Log> _logs;
        public int Requests;
        public FakeStore(List<Log> logs) => _logs = logs;

        public Log[] Fetch(string? anchor, ulong top, ulong skip, bool ascending)
        {
            Requests++;
            if (skip + top >= Window)
                throw new InvalidOperationException("Depth of pagination is limited in Elasticsearch by the max_result_window index setting.");

            IEnumerable<Log> rows = _logs;
            if (anchor is not null)
            {
                var m = Regex.Match(anchor, @"^\(TimeStamp (lt|ge) (\S+)\)$");
                Assert.True(m.Success, anchor);
                var t = DateTime.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                rows = m.Groups[1].Value == "lt"
                    ? rows.Where(l => l.TimeStamp!.Value < t)
                    : rows.Where(l => l.TimeStamp!.Value >= t);
            }
            // Ties within a millisecond come back in an order that differs from the list order,
            // as a store may give them in any order.
            rows = ascending
                ? rows.OrderBy(l => l.TimeStamp).ThenByDescending(l => l.Message)
                : rows.OrderByDescending(l => l.TimeStamp).ThenBy(l => l.Message);
            return rows.Skip((int)skip).Take((int)top).ToArray();
        }
    }

    private static List<Log> Page(FakeStore store, bool ascending, ulong skip = 0, ulong first = ulong.MaxValue) =>
        OrchAPISession.PageRobotLogsByTimeStamp((a, top, s) => store.Fetch(a, top, s, ascending), ascending, skip, first, PageSize, Window).ToList();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reads_past_the_window_once_each_in_order(bool ascending)
    {
        var logs = MakeLogs(350, i => i);
        var result = Page(new FakeStore(logs), ascending);

        Assert.Equal(350, result.Count);
        Assert.Equal(350, result.Distinct().Count());
        var expected = ascending ? logs.OrderBy(l => l.TimeStamp) : logs.OrderByDescending(l => l.TimeStamp);
        Assert.Equal(expected.Select(l => l.TimeStamp), result.Select(l => l.TimeStamp));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rows_sharing_a_millisecond_across_a_window_edge_come_once(bool ascending)
    {
        // Seven rows per millisecond, so every window edge falls inside a millisecond.
        var logs = MakeLogs(350, i => i / 7);
        var result = Page(new FakeStore(logs), ascending);

        Assert.Equal(350, result.Count);
        Assert.Equal(logs.ToHashSet(), result.ToHashSet());
    }

    [Fact]
    public void A_skip_past_the_window_is_counted_off()
    {
        var logs = MakeLogs(350, i => i);
        var result = Page(new FakeStore(logs), ascending: false, skip: 230, first: 15);

        var expected = logs.OrderByDescending(l => l.TimeStamp).Skip(230).Take(15);
        Assert.Equal(expected.Select(l => l.Message), result.Select(l => l.Message));
    }

    [Fact]
    public void A_skip_inside_the_first_window_goes_to_the_server()
    {
        var logs = MakeLogs(350, i => i);
        var store = new FakeStore(logs);
        var result = Page(store, ascending: false, skip: 40, first: 5);

        Assert.Equal(logs.OrderByDescending(l => l.TimeStamp).Skip(40).Take(5).Select(l => l.Message), result.Select(l => l.Message));
        Assert.Equal(1, store.Requests);
    }

    [Fact]
    public void Small_first_asks_for_that_many()
    {
        var store = new FakeStore(MakeLogs(350, i => i));
        var result = Page(store, ascending: false, first: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, store.Requests);
    }

    [Fact]
    public void Fewer_rows_than_a_page_take_one_request()
    {
        var store = new FakeStore(MakeLogs(4, i => i));
        Assert.Equal(4, Page(store, ascending: false).Count);
        Assert.Equal(1, store.Requests);
    }

    [Fact]
    public void More_rows_in_one_millisecond_than_a_window_throws()
    {
        var store = new FakeStore(MakeLogs(250, _ => 0));
        Assert.Throws<InvalidOperationException>(() => Page(store, ascending: false));
    }

    [Theory]
    [InlineData("&$filter=((Level ge '2'))", "(TimeStamp lt X)", "&$filter=(TimeStamp lt X) and ((Level ge '2'))")]
    [InlineData(null, "(TimeStamp lt X)", "&$filter=(TimeStamp lt X)")]
    [InlineData("&$filter=(A)", null, "&$filter=(A)")]
    public void AddFilterClause_prepends_to_the_filter(string? query, string? clause, string expected)
    {
        Assert.Equal(expected, OrchAPISession.AddFilterClause(query, clause));
    }
}
