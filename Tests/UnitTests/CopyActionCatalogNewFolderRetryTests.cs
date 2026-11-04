using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Copy-Item retries creating an action catalog in a destination folder the Actions service does
// not know yet (403, errorCode 1100), for about 30 seconds; the slowest folder measured took 11.
public class CopyActionCatalogNewFolderRetryTests
{
    [Fact]
    public void Schedule_CoversAboutThirtySeconds()
    {
        var total = OrchProvider.NewFolderRetryDelays.Aggregate(TimeSpan.Zero, (a, d) => a + d);
        Assert.InRange(total.TotalSeconds, 25, 35);
    }

    [Fact]
    public void Delay_FollowsTheSchedule_ThenStops()
    {
        Assert.Equal(TimeSpan.FromSeconds(0.5), OrchProvider.NewFolderRetryDelay(0));
        int last = OrchProvider.NewFolderRetryDelays.Length - 1;
        Assert.Equal(OrchProvider.NewFolderRetryDelays[last], OrchProvider.NewFolderRetryDelay(last));
        Assert.Null(OrchProvider.NewFolderRetryDelay(last + 1));
        Assert.Null(OrchProvider.NewFolderRetryDelay(-1));
    }

    [Fact]
    public void ErrorCode_IsReadFromTheResponseBody()
    {
        // The body the server sent for a folder created a moment earlier (measured 2026-11-04).
        var body = """{"message":"Folder does not exist or the user does not have access to the folder.","errorCode":1100,"traceId":"00-x-y-00"}""";
        var ex = new OrchException("Orch4:\\New", "Copying action catalog", new Exception(body));
        Assert.Equal(OrchProvider.FolderNotFoundOrNoAccessErrorCode, OrchException.ExtractErrorCode(ex));
    }
}
