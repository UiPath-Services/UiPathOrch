using System.Net;

namespace UiPath.PowerShell.Commands;

// Decides whether a rejected bulk upload (/api/TestDataQueueActions/BulkAddItems)
// is worth retrying one item at a time.
//
// A per-item fallback only helps when the rows differ in whether the server
// accepts them — i.e. a data / schema-validation problem, which Orchestrator
// reports as 400 Bad Request. For any other failure the rows are irrelevant:
// 401/403 (auth / permission), 404 (the queue is gone) and 429 / 5xx
// (throttling / transient / server) would reject every row identically, so
// retrying per item just floods the caller with duplicate errors and hammers a
// struggling server. In those cases the caller should surface one error and
// stop instead of falling back.
//
// Shared by Import-OrchTestDataQueueItem and Copy-OrchTestDataQueue's item copy
// so both classify a failed batch the same way. EnsureSuccessStatusCode throws
// HttpResponseException carrying the status, so this is a reliable status check
// rather than message-string matching.
internal static class TestDataQueueUploadPolicy
{
    public static bool IsPerRowDataError(Exception ex) =>
        ex is HttpResponseException { StatusCode: HttpStatusCode.BadRequest };
}
