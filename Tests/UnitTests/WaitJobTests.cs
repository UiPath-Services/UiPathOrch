using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins the pieces of Wait-OrchJob / Start-OrchJob -Wait that do not need a server: which job
// states end the wait, and the Id filters one polling round sends per folder.
public class WaitJobTests
{
    [Theory]
    [InlineData("Successful", true)]
    [InlineData("Faulted", true)]
    [InlineData("Stopped", true)]
    [InlineData("Suspended", true)]   // as Wait-Job: a job waiting on a person must not hold the caller
    [InlineData("Pending", false)]
    [InlineData("Running", false)]
    [InlineData("Stopping", false)]
    [InlineData("Terminating", false)]
    [InlineData("Resumed", false)]
    [InlineData(null, false)]
    public void Final_states(string? state, bool final)
    {
        Assert.Equal(final, JobWaiter.IsFinal(state));
    }

    [Fact]
    public void In_form_takes_up_to_100_ids_per_request_without_duplicates()
    {
        var ids = Enumerable.Range(1, 250).Select(i => (long)i).Concat([1L, 2L]);
        var filters = OrchAPISession.BuildJobIdFilters(ids, useOr: false).ToList();

        Assert.Equal(3, filters.Count);
        Assert.StartsWith("&$filter=Id in (1,2,3,", filters[0]);
        Assert.EndsWith(",250)", filters[2]);
        Assert.Equal(250, filters.Sum(f => f.Split(',').Length));
    }

    [Fact]
    public void Or_form_stays_under_the_node_limit()
    {
        // OData refuses more than 100 nodes; an "or" of 20 "Id eq" passed and 40 failed (Cloud, 2026-10-08).
        var filters = OrchAPISession.BuildJobIdFilters(Enumerable.Range(1, 31).Select(i => (long)i), useOr: true).ToList();

        Assert.Equal(3, filters.Count);
        Assert.Equal("&$filter=(Id eq 1 or Id eq 2 or Id eq 3 or Id eq 4 or Id eq 5 or Id eq 6 or Id eq 7 or Id eq 8 or Id eq 9 or Id eq 10 or Id eq 11 or Id eq 12 or Id eq 13 or Id eq 14 or Id eq 15)", filters[0]);
        Assert.Equal("&$filter=(Id eq 31)", filters[2]);
    }
}
