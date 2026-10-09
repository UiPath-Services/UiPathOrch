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
        var filters = OrchAPISession.BuildJobIdFilters(ids).ToList();

        Assert.Equal(3, filters.Count);
        Assert.StartsWith("&$filter=Id in (1,2,3,", filters[0]);
        Assert.EndsWith(",250)", filters[2]);
        Assert.Equal(250, filters.Sum(f => f.Split(',').Length));
    }
}
