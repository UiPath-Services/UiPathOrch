using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchProcess")]
[OutputType(typeof(Entities.Release))]
public class GetProcessCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    // Deprecated. Routes to Get-OrchProcessDetail via the shared helper. Kept
    // for backward compat; will be removed in a future major release.
    [Parameter]
    public SwitchParameter ExpandDetails { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedProcesses.csv";

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // Detail path: triggered by either the deprecated -ExpandDetails switch
        // or by -ExportCsv (the CSV row shape includes detail fields, so the
        // user-facing contract has always been "CSV implies detail enrichment").
        // -ExportCsv stays supported (output type matches CSV row shape: Release);
        // only -ExpandDetails emits a deprecation warning.
        bool useDetailPath = ExpandDetails.IsPresent || !string.IsNullOrEmpty(ExportCsv);

        if (useDetailPath)
        {
            if (ExpandDetails.IsPresent)
            {
                WriteWarning(
                    "'-ExpandDetails' on Get-OrchProcess is deprecated and will be removed in a " +
                    "future major release. Use 'Get-OrchProcessDetail' instead.");
            }

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, GetProcessDetailCmdlet.CsvHeaders);

            // EnumFolders returns the same shape used by Get-OrchProcessDetail's helper.
            var driveFolderList = drivesFolders.ToList();
            GetProcessDetailCmdlet.EmitDetailedReleases(this, driveFolderList, wpName, writer);

            if (!string.IsNullOrEmpty(ExportCsv))
            {
                WriteCSVExportedMessage(this, providerCsvPath);
            }
            return;
        }

        // List-only path: emit shallow Release entries from each folder.
        var folderList = drivesFolders.ToList();
        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, folderList.Count, "Getting processes");
        int index = 0;
        foreach (var (drive, folder) in folderList)
        {
            // Ctrl+C between folders: let the cancellation propagate (outside the try) so the
            // pipeline stops AS CANCELLED, rather than returning partial results that look like a
            // complete listing -- and without emitting one "operation was canceled" error per
            // remaining folder.
            cancelHandler.Token.ThrowIfCancellationRequested();
            reporter.WriteProgress(++index, folder.GetPSPath());
            try
            {
                var releases = drive.Releases.Get(folder);
                var targetReleases = releases
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name);
                WriteObject(targetReleases, true);
            }
            catch (OperationCanceledException)
            {
                // Ctrl+C while drive.Releases.Get(folder) is in flight (the data call observes the
                // cancel and throws): propagate a single stop instead of a per-folder error here PLUS
                // the loop's next ThrowIfCancellationRequested propagation -- which surfaced as two
                // "operation was canceled" messages.
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                continue;
            }
        }
    }
}
