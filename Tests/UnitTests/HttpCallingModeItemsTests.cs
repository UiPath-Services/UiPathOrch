using System.Linq;
using UiPath.PowerShell.Positional;
using Xunit;

namespace UnitTests;

// Pins the -CallingMode completer candidate list for New-/Update-OrchApiTrigger.
// All three values are live-verified against the server (Orch1 2026-05-22):
// each round-trips through New-OrchApiTrigger -> Get-OrchApiTrigger. The
// original list shipped without LongPolling; this test guards against that
// regression and against adding a value the server would reject (which
// surfaces as a cryptic "httpTrigger must not be null" binding failure).
public class HttpCallingModeItemsTests
{
    [Fact]
    public void Contains_ExactlyTheThreeServerAcceptedValues()
    {
        var items = HttpCallingModeItems.Items;
        Assert.Equal(
            new[] { "AsyncRequestReply", "FireAndForget", "LongPolling" }.OrderBy(s => s),
            items.OrderBy(s => s));
    }

    [Fact]
    public void IncludesLongPolling()
    {
        // Explicit regression guard: LongPolling was the value missing from
        // the original two-element list.
        Assert.Contains("LongPolling", HttpCallingModeItems.Items);
    }
}
