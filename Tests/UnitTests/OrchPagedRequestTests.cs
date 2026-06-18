using System.Collections.Generic;
using System.Linq;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins the request-composition side of the HTTP layer that GetEnumerable / GetEnumerableIdentity
// / GetEnumerablePortal use: how a page's URL is built from (baseUrl, endpoint, top, skip, query)
// and how the shared Paginate loop advances skip across pages. The actual HttpClient send in
// those helpers is welded to a live PSDrive session and can't be unit-mocked, so these tests
// stand in a fake "transport" (a fetchPage delegate that records the URL it would request via
// the real BuildPagedUrl + Paginate) to assert the per-page request sequence — closing the
// previously-untested "parameters -> $top/$skip/$filter wiring" gap.
public class OrchPagedRequestTests
{
    [Fact]
    public void BuildPagedUrl_odata_style_uses_dollar_top_and_skip()
        => Assert.Equal(
            "https://host/odata/Assets?$top=1000&$skip=0&$filter=Name eq 'x'",
            OrchAPISession.BuildPagedUrl("https://host", "/odata/Assets", 1000, 0, "&$filter=Name eq 'x'", odataStyle: true));

    [Fact]
    public void BuildPagedUrl_identity_style_uses_bare_top_and_skip()
        => Assert.Equal(
            "https://host/identity_/api/Group?top=50&skip=100",
            OrchAPISession.BuildPagedUrl("https://host/identity_", "/api/Group", 50, 100, null, odataStyle: false));

    [Fact]
    public void BuildPagedUrl_appends_query_tail_verbatim()
        => Assert.Equal(
            "https://host/odata/QueueItems?$top=1000&$skip=0&$filter=(Type eq 'X')&$orderby=Id desc&$expand=Robot",
            OrchAPISession.BuildPagedUrl("https://host", "/odata/QueueItems", 1000, 0,
                "&$filter=(Type eq 'X')&$orderby=Id desc&$expand=Robot", odataStyle: true));

    // Drive the real Paginate loop with a fake transport that records the URL each page would
    // request. Two full 1000-item pages then a short page: skip must advance 0 -> 1000 -> 2000,
    // the query tail rides along on every page, and stopOnPartialPage ends without a 4th request.
    [Fact]
    public void Paginate_emits_correct_per_page_urls_and_stops_on_partial_page()
    {
        var pageLengths = new Queue<int>(new[] { 1000, 1000, 3 });
        var requestedUrls = new List<string>();

        var items = OrchAPISession.Paginate<int>((top, skip) =>
        {
            requestedUrls.Add(OrchAPISession.BuildPagedUrl(
                "https://host", "/odata/Jobs", top, skip, "&$filter=State eq 'Running'", odataStyle: true));
            int len = pageLengths.Count > 0 ? pageLengths.Dequeue() : 0;
            return Enumerable.Repeat(1, len).ToArray();
        }, startSkip: 0, first: ulong.MaxValue, stopOnPartialPage: true).ToList();

        Assert.Equal(2003, items.Count);
        Assert.Equal(new[]
        {
            "https://host/odata/Jobs?$top=1000&$skip=0&$filter=State eq 'Running'",
            "https://host/odata/Jobs?$top=1000&$skip=1000&$filter=State eq 'Running'",
            "https://host/odata/Jobs?$top=1000&$skip=2000&$filter=State eq 'Running'",
        }, requestedUrls);
    }

    // `first` (the -First cap) shrinks the last page's $top so the layer never over-fetches.
    [Fact]
    public void Paginate_caps_top_to_remaining_first()
    {
        var requestedTops = new List<ulong>();

        var items = OrchAPISession.Paginate<int>((top, skip) =>
        {
            requestedTops.Add(top);
            return Enumerable.Repeat(1, (int)top).ToArray();
        }, startSkip: 0, first: 1500, stopOnPartialPage: true).ToList();

        // Page 1 asks for 1000 (full PageSize); page 2 asks for the remaining 500, not 1000.
        Assert.Equal(new ulong[] { 1000, 500 }, requestedTops);
        Assert.Equal(1500, items.Count);
    }
}
