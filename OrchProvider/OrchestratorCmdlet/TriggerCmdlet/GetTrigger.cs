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

        private static void WriteCsvContent(StreamWriter writer, ProcessSchedule t)
        {
            var line = new StringBuilder();

            line.Append($"{EscapeCsvValue(t.Path, true)},");
            line.Append($"{EscapeCsvValue(t.Name, true)},");
            line.Append($"{EscapeCsvValue(t.ReleaseName)},");
            line.Append($"{t.Enabled},");
            line.Append($"{t.SpecificPriorityValue},");
            line.Append($"{t.StartStrategy},");
            line.Append($"{t.StopStrategy},");
            line.Append($"{EscapeCsvValue(t.StopProcessExpression)},");
            line.Append($"{EscapeCsvValue(t.KillProcessExpression)},");
            line.Append($"{EscapeCsvValue(t.AlertPendingExpression)},");
            line.Append($"{EscapeCsvValue(t.AlertRunningExpression)},");
            line.Append($"{t.ConsecutiveJobFailuresThreshold},");
            line.Append($"{t.JobFailuresGracePeriodInHours},");
            line.Append($"{EscapeCsvValue(t.RuntimeType)},");
            line.Append($"{EscapeCsvValue(t.InputArguments)},");
            line.Append($"{t.ResumeOnSameContext},");
            line.Append($"{t.RunAsMe},");
            line.Append($"{t.IsConnected},");
            line.Append($"{EscapeCsvValue(t.CalendarName)},");
            line.Append($"{t.ActivateOnJobComplete},");
            line.Append($"{t.ItemsActivationThreshold},");
            line.Append($"{t.ItemsPerJobActivationTarget},");
            line.Append($"{t.MaxJobsForActivation},");
            line.Append($"{EscapeCsvValue(t.StartProcessCron)},");
            line.Append($"{EscapeCsvValue(t.StartProcessCronDetails)},");
            line.Append($"{EscapeCsvValue(t.QueueDefinitionName)},");
            line.Append($"{EscapeCsvValue(t.TimeZoneId)},");
            line.Append($"{FormatDateTimeWithKind(t.StopProcessDate)},");

            #region MachineRobots の Id を Name に変換して出力
            if (t.MachineRobots != null && t.MachineRobots.Length != 0)
            {
                var (drive, folder) = OrchDriveInfo.EnumFolders(t.Path).FirstOrDefault();
                string machineRobots = SerializeMachineRobotSessionArray(drive, folder!, t.MachineRobots);
                line.Append($"{EscapeCsvValue(machineRobots)}");
            }
            #endregion

            writer.WriteLine(line.ToString());
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
                df => df.drive.GetProcessSchedules(df.folder)
            );

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
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
                            var detailedEntity = drive.GetProcessSchedule(folder, entity);
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
