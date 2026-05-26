using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// The Orchestrator web "Upload Items" dialog rejects >15,000-row CSVs
// client-side for BOTH regular queues and test data queues (observed
// 2026-05-25 — same message for both). Import-OrchQueueItem and
// Import-OrchTestDataQueueItem mirror that cap via CsvUploadLimit so a CSV the
// web rejects is rejected identically by the cmdlets. The exact message is
// pinned here because it is reproduced verbatim from the web.
public class CsvUploadLimitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15000)]
    public void AtOrBelowCap_IsAllowed(int rowCount)
    {
        Assert.Null(CsvUploadLimit.RowLimitError(rowCount));
    }

    [Theory]
    [InlineData(15001)]
    [InlineData(20000)]
    [InlineData(300000)]
    public void AboveCap_ReturnsWebVerbatimMessage(int rowCount)
    {
        Assert.Equal(
            $"The maximum number of rows allowed is 15000. You are trying to upload {rowCount} records.",
            CsvUploadLimit.RowLimitError(rowCount));
    }
}
