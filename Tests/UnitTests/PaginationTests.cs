using System;
using System.Collections.Generic;
using System.Linq;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins the shared offset-paging loop (OrchAPISession.Paginate) that the OData / identity /
// portal enumerators delegate to. The interesting axes are the terminal condition
// (stop-on-partial-page vs stop-on-empty-page), the `first` cap, the start offset, and
// behavior when a server returns fewer items per page than requested.
public class PaginationTests
{
    // A fake paged endpoint over `data` that returns at most `serverCap` items per request
    // (regardless of the requested top), recording the requested top of each call.
    private static Func<ulong, ulong, int[]?> Server(int[] data, int serverCap, List<ulong>? calls = null)
        => (top, skip) =>
        {
            calls?.Add(top);
            int start = (int)skip;
            if (start >= data.Length) return Array.Empty<int>();
            int take = (int)Math.Min(Math.Min(top, (ulong)serverCap), (ulong)(data.Length - start));
            return data.Skip(start).Take(take).ToArray();
        };

    [Fact]
    public void StopOnPartialPage_returns_all_with_no_extra_request_when_server_honors_top()
    {
        // 1050 items, server returns up to the requested top (1000) -> two requests
        // (1000 + 50), and the 50-item page ends it with no extra empty request.
        var data = Enumerable.Range(0, 1050).ToArray();
        var calls = new List<ulong>();
        var result = OrchAPISession.Paginate<int>(Server(data, serverCap: 1000, calls), 0, ulong.MaxValue, stopOnPartialPage: true).ToList();

        Assert.Equal(data, result);
        Assert.Equal(2, calls.Count);
    }

    [Fact]
    public void StopOnPartialPage_assumes_the_server_returns_up_to_top()
    {
        // Contract of stopOnPartialPage:true: a page shorter than the requested top is treated
        // as the end. A server that caps a page below the requested top stops this mode early —
        // that case must use the empty-page mode (next test).
        var data = Enumerable.Range(0, 250).ToArray();
        var result = OrchAPISession.Paginate<int>(Server(data, serverCap: 100), 0, ulong.MaxValue, stopOnPartialPage: true).ToList();

        Assert.Equal(100, result.Count); // documents the contract explicitly
    }

    [Fact]
    public void EmptyPageMode_returns_all_even_when_server_caps_below_requested_top()
    {
        // The empty-page mode (stopOnPartialPage:false) keeps paging until an empty page, so a
        // server that caps each page below the requested top never drops the tail.
        var data = Enumerable.Range(0, 250).ToArray();
        var calls = new List<ulong>();
        var result = OrchAPISession.Paginate<int>(Server(data, serverCap: 100, calls), 0, ulong.MaxValue, stopOnPartialPage: false).ToList();

        Assert.Equal(data, result);          // 100 + 100 + 50, none dropped
        Assert.Equal(4, calls.Count);        // ...followed by one empty page to confirm the end
    }

    [Fact]
    public void Respects_the_first_limit_mid_page()
    {
        var data = Enumerable.Range(0, 5000).ToArray();
        var calls = new List<ulong>();
        var result = OrchAPISession.Paginate<int>(Server(data, serverCap: 1000, calls), 0, first: 1500, stopOnPartialPage: true).ToList();

        Assert.Equal(1500, result.Count);
        Assert.Equal(Enumerable.Range(0, 1500), result);
        // top requests shrink toward the remaining budget: 1000 then 500.
        Assert.Equal(new ulong[] { 1000, 500 }, calls);
    }

    [Fact]
    public void Honors_the_start_offset()
    {
        var data = Enumerable.Range(0, 100).ToArray();
        var result = OrchAPISession.Paginate<int>(Server(data, serverCap: 1000), startSkip: 30, ulong.MaxValue, stopOnPartialPage: true).ToList();

        Assert.Equal(Enumerable.Range(30, 70), result);
    }

    [Fact]
    public void Empty_source_yields_nothing_in_one_request()
    {
        var calls = new List<ulong>();
        var result = OrchAPISession.Paginate<int>(Server(Array.Empty<int>(), serverCap: 1000, calls), 0, ulong.MaxValue, stopOnPartialPage: true).ToList();

        Assert.Empty(result);
        Assert.Single(calls);
    }
}
