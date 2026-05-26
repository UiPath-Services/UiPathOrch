namespace UiPath.PowerShell.Commands;

// The Orchestrator web "Upload Items" dialog rejects CSV uploads of more than
// 15,000 rows client-side — for BOTH regular queues and test data queues —
// with the message reproduced below (observed 2026-05-25). 15,000 is the
// default of the Upload.Queues.MaxNumberOfItems app setting.
//
// Import-OrchQueueItem and Import-OrchTestDataQueueItem enforce the same cap so
// a CSV the web rejects is rejected identically by the cmdlets. This matters
// especially for test data queues: the /BulkAddItems API itself accepts far
// more (200,000 verified in one call), so without this client-side check the
// cmdlet would succeed where the web blocks — diverging from the web.
internal static class CsvUploadLimit
{
    internal const int MaxRows = 15000;

    // Returns the web's exact rejection message when rowCount exceeds the cap,
    // or null when the upload is within the limit.
    internal static string? RowLimitError(int rowCount) =>
        rowCount > MaxRows
            ? $"The maximum number of rows allowed is {MaxRows}. You are trying to upload {rowCount} records."
            : null;
}
