using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchMachine")]
[OutputType(typeof(Entities.ExtendedMachine))]
[OutputType(typeof(Entities.RobotUser))]
public class GetMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(MachineNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
        "Scope",
        "UnattendedSlots",
        "NonProductionSlots",
        "TestAutomationSlots",
        "AutomationType",
        "TargetFramework",
        "RobotUsers",
        "UpdatePolicyType",
        "UpdatePolicyVersion",
        "MaintenanceCron",
        "MaintenanceDuration",
        "MaintenanceEnabled",
        "MaintenanceTimezoneId",
        "Tags"
    ];

    private static void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, IEnumerable<ExtendedMachine> machines)
    {
        foreach (var machine in machines)
        {
            IEnumerable<string?> robotUsers = null;
            if (machine.RobotUsers is not null)
            {
                var allRobots = drive.AllRobotsAcrossFolders.Get();
                robotUsers = machine.RobotUsers?.Select(r => {
                    var robot = allRobots.FirstOrDefault(all => all.Id == r.RobotId);
                    return robot?.User?.FullName;
                })
                .Where(r => !string.IsNullOrEmpty(r));
            }

            string?[] line = [
                EscapeCsvValue(machine.Path, true),
                EscapeCsvValue(machine.Name, true),
                EscapeCsvValue(machine.Description),
                EscapeCsvValue(machine.Type),
                EscapeCsvValue(machine.Scope),
                EscapeCsvValue(machine.UnattendedSlots),
                EscapeCsvValue(machine.NonProductionSlots),
                EscapeCsvValue(machine.TestAutomationSlots),
                EscapeCsvValue(machine.AutomationType),
                EscapeCsvValue(machine.TargetFramework),
                EscapeCsvValue(robotUsers),
                machine.UpdatePolicy?.Type,
                machine.UpdatePolicy?.SpecificVersion,
                EscapeCsvValue(machine.MaintenanceWindow?.CronExpression),
                EscapeCsvValue(machine.MaintenanceWindow?.Duration),
                EscapeCsvValue(machine.MaintenanceWindow?.Enabled),
                EscapeCsvValue(machine.MaintenanceWindow?.TimezoneId),
                EscapeCsvValue(machine.Tags)
            ];
            WriteCsvLine(writer, line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.Machines.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var machines = result.GetResult(cancelHandler.Token);
                if (machines is null) continue;

                var drive = result.Source!;

                var filteredMachines = machines
                        .FilterByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name);

                if (writer is not null)
                {
                    WriteCsvContent(writer, drive, filteredMachines);
                }
                else if (!ExpandRobotUser.IsPresent)
                {
                    WriteObject(filteredMachines, true);
                }
                else
                {
                    foreach (var machine in filteredMachines)
                    {
                        if (machine.RobotUsers is null) continue;
                        WriteObject(machine.RobotUsers, true);
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetMachineError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
