using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchMachine")]
    [OutputType(typeof(Entities.ExtendedMachine))]
    [OutputType(typeof(Entities.RobotUser))]
    public class GetMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(MachineNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter ExpandRobotUser { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedMachines.csv";
        private static readonly string[] CsvHeaders = [
            "Path", 
            "Name", 
            "Description", 
            "Type", 
            "UnattendedSlots",
            "NonProductionSlots",
            "TestAutomationSlots", 
            "AutomationType", 
            "TargetFramework", 
            "Tags"
        ];

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<ExtendedMachine> machines)
        {
            foreach (var machine in machines)
            {
                string[] line = [
                    EscapeCsvValue(machine.Path, true),
                    EscapeCsvValue(machine.Name, true),
                    EscapeCsvValue(machine.Description),
                    EscapeCsvValue(machine.UnattendedSlots),
                    EscapeCsvValue(machine.NonProductionSlots),
                    EscapeCsvValue(machine.TestAutomationSlots),
                    EscapeCsvValue(machine.Type),
                    EscapeCsvValue(machine.AutomationType),
                    EscapeCsvValue(machine.TargetFramework),
                    EscapeCsvValue(machine.Tags)
                ];
                WriteCsvLine(writer, line);
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetMachines());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var machines = result.GetResult(cancelHandler.Token);
                    if (machines == null) continue;

                    var filteredMachines = machines
                            .FilterByWildcards(m => m?.Name, wpName)
                            .OrderBy(m => m.Name);

                    if (writer != null)
                    {
                        WriteCsvContent(writer, filteredMachines);
                    }
                    else if (!ExpandRobotUser.IsPresent)
                    {
                        WriteObject(filteredMachines, true);
                    }
                    else
                    {
                        foreach (var machine in filteredMachines)
                        {
                            if (machine.RobotUsers == null) continue;
                            WriteObject(machine.RobotUsers, true);
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetMachineError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
