using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTrigger")]
[OutputType(typeof(Entities.ProcessSchedule))]
public class GetTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Deprecated. Routes to Get-OrchTriggerDetail via the shared helper. Kept
    // for backward compat; will be removed in a future major release.
    [Parameter]
    public SwitchParameter ExpandDetails { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedTriggers.csv";

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // Detail path: triggered by either the deprecated -ExpandDetails switch
        // or by -ExportCsv (the CSV row shape includes detail fields, so the
        // user-facing contract has always been "CSV implies detail enrichment").
        // -ExportCsv stays supported (output type matches CSV row shape:
        // ProcessSchedule); only -ExpandDetails emits a deprecation warning.
        bool useDetailPath = ExpandDetails.IsPresent || !string.IsNullOrEmpty(ExportCsv);

        if (useDetailPath)
        {
            if (ExpandDetails.IsPresent)
            {
                WriteWarning(
                    "'-ExpandDetails' on Get-OrchTrigger is deprecated and will be removed in a " +
                    "future major release. Use 'Get-OrchTriggerDetail' instead.");
            }

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, GetTriggerDetailCmdlet.CsvHeaders);

            var driveFolderList = drivesFolders.ToList();
            GetTriggerDetailCmdlet.EmitDetailedTriggers(this, driveFolderList, wpName, writer);

            if (!string.IsNullOrEmpty(ExportCsv))
            {
                WriteCSVExportedMessage(this, providerCsvPath);
            }
            return;
        }

        // List-only path: emit shallow ProcessSchedule entries from each folder.
        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.GetTriggers(df.folder)
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var targetEntities = entities
                        .FilterByWildcards(s => s?.Name, wpName)
                        .OrderBy(s => s.Name);

                WriteObject(targetEntities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTriggerError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
