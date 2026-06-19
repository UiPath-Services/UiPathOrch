using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTriggerDetail")]
[OutputType(typeof(ProcessSchedule))]
public class GetTriggerDetailCmdlet : OrchestratorPSCmdlet
{
    // -Name is Mandatory by design — the detail path makes one API call per
    // matched trigger (plus a side fetch for ExecutorRobots), so accidental
    // fan-out from a default "all triggers" would be expensive on large
    // folders. Wildcards (including "*") still work; the user just has to
    // type the selector explicitly.
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[] Name { get; set; } = default!;

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

    private static readonly string DefaultCsvName = "ExportedTriggers.csv";
    internal static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "ReleaseName",
        "Enabled",
        "SpecificPriorityValue",
        "StartStrategy",
        "StopStrategy",
        "StopProcessExpression",
        "KillProcessExpression",
        "AlertPendingExpression",
        "AlertRunningExpression",
        "ConsecutiveJobFailuresThreshold",
        "JobFailuresGracePeriodInHours",
        "RuntimeType",
        "InputArguments",
        "ResumeOnSameContext",
        "RunAsMe",
        "IsConnected",
        "CalendarName",
        "ActivateOnJobComplete",
        "ItemsActivationThreshold",
        "ItemsPerJobActivationTarget",
        "MaxJobsForActivation",
        "StartProcessCron",
        "StartProcessCronDetails",
        "QueueDefinitionName",
        "TimeZoneId",
        "StopProcessDate",
        "ExecutorRobots",
        "MachineRobots"
    ];

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        EmitDetailedTriggers(this, drivesFolders, wpName, writer);

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }

    /// <summary>
    /// Canonical implementation for "fetch each matched trigger's detail and
    /// emit either to caller.WriteObject or to the supplied CSV writer".
    /// Called by this cmdlet's ProcessRecord, by GetTriggerCmdlet's
    /// deprecated -ExpandDetails path, and by GetTriggerCmdlet's
    /// -ExportCsv path.
    /// </summary>
    internal static void EmitDetailedTriggers(
        OrchestratorPSCmdlet caller,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        List<WildcardPattern>? nameWildcards,
        StreamWriter? writer)
    {
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

                var (drive, folder) = result.Source;

                var targetEntities = entities
                        .FilterByWildcards(s => s?.Name, nameWildcards)
                        .OrderBy(s => s.Name);

                foreach (var entity in targetEntities)
                {
                    var detailedEntity = drive.TriggersDetailed.Get(folder, entity.Id!.Value);
                    if (detailedEntity is null) continue;

                    if (writer is not null) { WriteCsvContent(caller, writer, detailedEntity); }
                    else { caller.WriteObject(detailedEntity); }
                }
            }
            catch (OrchException ex)
            {
                caller.WriteError(new ErrorRecord(ex, "GetTriggerDetailError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }

    private static void WriteCsvContent(OrchestratorPSCmdlet caller, StreamWriter writer, ProcessSchedule t)
    {
        // Convert ExecutorRobots IDs to Names for output.
        string executorRobots = null;
        if (t.ExecutorRobots is not null && t.ExecutorRobots.Length != 0)
        {
            try
            {
                var (drive, folder) = caller.SessionState.ResolveToSingleFolder(t.Path);
                executorRobots = SerializeExecutorRobotArray(drive, t.ExecutorRobots);
            }
            catch (Exception ex)
            {
                caller.WriteWarning($"{t.GetPSPath()}: Failed to retrieve ExecutorRobots: {ex.Message}");
            }
        }

        // Convert MachineRobots IDs to Names for output.
        string machineRobots = null;
        if (t.MachineRobots is not null && t.MachineRobots.Length != 0)
        {
            try
            {
                var (drive, folder) = caller.SessionState.ResolveToSingleFolder(t.Path);
                machineRobots = SerializeMachineRobotSessions(drive, folder!, t.MachineRobots);
            }
            catch (Exception ex)
            {
                caller.WriteWarning($"{t.GetPSPath()}: Failed to retrieve MachineRobots: {ex.Message}");
            }
        }

        string[] line = [
            EscapeCsvValue(t.Path, true),
            EscapeCsvValue(t.Name, true),
            EscapeCsvValue(t.ReleaseName),
            EscapeCsvValue(t.Enabled),
            EscapeCsvValue(t.SpecificPriorityValue),
            EscapeCsvValue(t.StartStrategy),
            EscapeCsvValue(t.StopStrategy),
            EscapeCsvValue(t.StopProcessExpression),
            EscapeCsvValue(t.KillProcessExpression),
            EscapeCsvValue(t.AlertPendingExpression),
            EscapeCsvValue(t.AlertRunningExpression),
            EscapeCsvValue(t.ConsecutiveJobFailuresThreshold),
            EscapeCsvValue(t.JobFailuresGracePeriodInHours),
            EscapeCsvValue(t.RuntimeType),
            EscapeCsvValue(t.InputArguments),
            EscapeCsvValue(t.ResumeOnSameContext),
            EscapeCsvValue(t.RunAsMe),
            EscapeCsvValue(t.IsConnected),
            EscapeCsvValue(t.CalendarName),
            EscapeCsvValue(t.ActivateOnJobComplete),
            EscapeCsvValue(t.ItemsActivationThreshold),
            EscapeCsvValue(t.ItemsPerJobActivationTarget),
            EscapeCsvValue(t.MaxJobsForActivation),
            EscapeCsvValue(t.StartProcessCron),
            EscapeCsvValue(t.StartProcessCronDetails),
            EscapeCsvValue(t.QueueDefinitionName),
            EscapeCsvValue(t.TimeZoneId),
            EscapeCsvValue(FormatDateTimeWithKind(t.StopProcessDate)),
            EscapeCsvValue(executorRobots),
            EscapeCsvValue(machineRobots)
        ];
        writer.WriteCsvLine(line);
    }
}
