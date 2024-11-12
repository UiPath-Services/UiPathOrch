using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchTrigger")]
    [OutputType(typeof(Entities.ProcessSchedule))]
    public class GetTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<Positional.Name>))]
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
            "MachineRobots"
        ];

        private void WriteCsvContent(StreamWriter writer, ProcessSchedule t)
        {
            #region MachineRobots の Id を Name に変換して出力
            string machineRobots = null;
            if (t.MachineRobots != null && t.MachineRobots.Length != 0)
            {
                try
                {
                    var (drive, folder) = OrchDriveInfo.EnumFolders(t.Path).FirstOrDefault();
                    machineRobots = SerializeMachineRobotSessionArray(drive, folder!, t.MachineRobots);
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

        //private void Output(StreamWriter? writer, IEnumerable<ProcessSchedule> triggers)
        //{
        //    if (writer != null)
        //    {
        //        foreach (var t in triggers)
        //        {
        //            WriteCsvContent(writer, t);
        //        }
        //    }
        //    else
        //    {
        //        WriteObject(triggers, true);
        //    }
        //}

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

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

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
