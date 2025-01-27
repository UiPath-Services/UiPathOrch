using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchTrigger")]
    [OutputType(typeof(Entities.ProcessSchedule))]
    public class GetTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        [Parameter]
        public SwitchParameter ExpandDetails { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedTriggers.csv";
        private static readonly string[] CsvHeaders = [
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

        private void WriteCsvContent(StreamWriter writer, ProcessSchedule t)
        {
            #region ExecutorRobots の Id を Name に変換して出力
            string executorRobots = null;
            if (t.ExecutorRobots != null && t.ExecutorRobots.Length != 0)
            {
                try
                {
                    var (drive, folder) = OrchDriveInfo.ResolveToSingleFolder(t.Path);
                    executorRobots = SerializeExecutorRobotArray(drive, t.ExecutorRobots);
                }
                catch (Exception ex)
                {
                    WriteWarning($"{t.GetPSPath()}: Failed to retrieve ExecutorRobots: {ex.Message}");
                }
            }
            #endregion

            #region MachineRobots の Id を Name に変換して出力
            string machineRobots = null;
            if (t.MachineRobots != null && t.MachineRobots.Length != 0)
            {
                try
                {
                    var (drive, folder) = OrchDriveInfo.ResolveToSingleFolder(t.Path);
                    machineRobots = SerializeMachineRobotSessions(this, drive, folder!, t.GetPSPath(), t.MachineRobots);
                }
                catch (Exception ex)
                {
                    WriteWarning($"{t.GetPSPath()}: Failed to retrieve MachineRobots: {ex.Message}");
                }
            }
            #endregion

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
            WriteCsvLine(writer, line);
        }

        private void Output(StreamWriter? writer, ProcessSchedule? trigger)
        {
            if (trigger == null) return;

            if (writer != null)
            {
                WriteCsvContent(writer, trigger);
            }
            else
            {
                WriteObject(trigger);
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetTriggers(df.folder)
            );

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    var (drive, folder) = result.Source;

                    var targetEntities = entities
                            .FilterByWildcards(s => s?.Name, wpName)
                            .OrderBy(s => s.Name);

                    if (ExpandDetails.IsPresent || writer != null)
                    {
                        foreach (var entity in targetEntities)
                        {
                            var detailedEntity = drive.GetTrigger(folder, entity);
                            Output(writer, detailedEntity);
                        }
                    }
                    else
                    {
                        WriteObject(targetEntities, true);
                        //Output(writer, targetEntities);
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetTriggerError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
