using System;
using System.Net;
using System.Net.Http;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins the batch-failure classification shared by Import-OrchTestDataQueueItem
// and Copy-OrchTestDataQueue: only a 400 Bad Request (a per-row data / schema
// problem) is worth retrying one item at a time. Every other failure rejects
// all rows the same way, so it must NOT trigger the per-item fallback (the
// caller surfaces one error and stops instead).
public class TestDataQueueUploadPolicyTests
{
    private static HttpResponseException Http(HttpStatusCode code) =>
        new("err", new HttpResponseMessage(code));

    [Fact]
    public void BadRequest_triggers_per_item_fallback()
    {
        Assert.True(TestDataQueueUploadPolicy.IsPerRowDataError(Http(HttpStatusCode.BadRequest)));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]      // 401 — token won't auto-recover
    [InlineData(HttpStatusCode.Forbidden)]         // 403 — permission won't auto-grant
    [InlineData(HttpStatusCode.NotFound)]          // 404 — the queue is gone
    [InlineData(HttpStatusCode.Conflict)]          // 409 — not per-row here
    [InlineData(HttpStatusCode.TooManyRequests)]   // 429 — throttling, transient
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]      // 504
    public void Other_http_statuses_do_not_trigger_fallback(HttpStatusCode code)
    {
        Assert.False(TestDataQueueUploadPolicy.IsPerRowDataError(Http(code)));
    }

    [Fact]
    public void Non_http_exceptions_do_not_trigger_fallback()
    {
        Assert.False(TestDataQueueUploadPolicy.IsPerRowDataError(new InvalidOperationException("boom")));
        Assert.False(TestDataQueueUploadPolicy.IsPerRowDataError(new HttpRequestException("network down")));
        Assert.False(TestDataQueueUploadPolicy.IsPerRowDataError(new OperationCanceledException()));
    }
}
