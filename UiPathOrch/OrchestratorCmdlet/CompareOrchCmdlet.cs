using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shared -ExportCsv surface for the Compare-Orch* family.
//
// Every cmdlet in the family emits the same OrchComparison shape, so unlike the Get-Orch*
// cmdlets — where each noun declares its own column list, chosen to bind back into its New-/
// Update- partner — the columns here are fixed once for all of them. A comparison has no import
// partner: the file is a migration verification report, so the columns are the ones a reader
// needs and the values are written as they read on screen, not escaped for re-binding.
//
// The six columns, in this order, are exactly what `Compare-Orch* | Export-Csv` already emitted,
// so a script built on that pipeline keeps finding the same headers. ReferenceObject and
// DifferenceObject are deliberately not among them: they are whole entities kept for downstream
// piping, and a CSV can hold nothing of them but their type name.
public abstract class CompareOrchCmdlet : OrchestratorPSCmdlet, IDisposable
{
    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    internal static readonly string[] CsvHeaders = [
        "SideIndicator",
        "Name",
        "DifferenceName",
        "Path",
        "DifferencePath",
        "Differences",
    ];

    // File name used when -ExportCsv names a directory rather than a file.
    protected abstract string DefaultCsvName { get; }

    private StreamWriter? _csvWriter;
    private string? _csvProviderPath;
    private bool _csvOpened;

    /// <summary>
    /// The sink the comparison engine emits through: the pipeline when -ExportCsv was not given,
    /// a CSV row writer when it was. Safe to call from every ProcessRecord — the file is created
    /// once per invocation, so driving the reference side from the pipeline appends the records
    /// of every item instead of truncating the file down to the last one.
    /// </summary>
    protected Action<object> CsvOrPipeline()
    {
        if (!_csvOpened)
        {
            _csvOpened = true;
            (var physicalPath, _csvProviderPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            _csvWriter = WriteCsvHeader(physicalPath, CsvEncoding, CsvHeaders);
        }

        var writer = _csvWriter;
        if (writer is null) return WriteObject;

        return o =>
        {
            // Nothing but comparison rows reaches the sink today; anything else still goes to the
            // pipeline rather than being flattened into a line that does not match the header.
            if (o is not OrchComparison c) { WriteObject(o); return; }

            writer.WriteCsvLine(CsvRow(c));
        };
    }

    // One row per comparison, in CsvHeaders order. Values are not wildcard-escaped: unlike a
    // Get-Orch* export there is no cmdlet to import this back into, and a name containing '[' is
    // meant to read as itself to whoever opens the report.
    internal static string[] CsvRow(OrchComparison c) => [
        EscapeCsvValue(c.SideIndicator),
        EscapeCsvValue(c.Name),
        EscapeCsvValue(c.DifferenceName),
        EscapeCsvValue(c.Path),
        EscapeCsvValue(c.DifferencePath),
        EscapeCsvValue(c.Differences?.ToString()),
    ];

    protected override void EndProcessing()
    {
        CloseCsv(announce: true);
        base.EndProcessing();
    }

    // PowerShell disposes the cmdlet after the pipeline ends, including one stopped with Ctrl-C
    // before EndProcessing runs; without this the rows written so far would be lost in the
    // StreamWriter's buffer, which for a long -Recurse comparison is most of them. The "exported
    // as" line is NOT written here: a stopped pipeline rejects the write, and the file is a
    // partial result anyway.
    public void Dispose()
    {
        CloseCsv(announce: false);
        GC.SuppressFinalize(this);
    }

    private void CloseCsv(bool announce)
    {
        if (_csvWriter is null) return;
        _csvWriter.Dispose();
        _csvWriter = null;
        if (announce) WriteCSVExportedMessage(this, _csvProviderPath);
    }
}
