using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;
using Template = UiPath.PowerShell.Positional.Template;
using Any_Foreground_Background = UiPath.PowerShell.Positional.Any_Foreground_Background;
using Any_Windows_Portable  = UiPath.PowerShell.Positional.Any_Windows_Portable;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchMachine", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.CreatedMachine))]
    public class UpdateMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Description { get; set; }

        // 現在は Template しかサポートしていない
        // ["Template", "", "", ""]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Template>))]
        public string? Type { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? UnattendedSlots { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? NonProductionSlots { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? TestAutomationSlots { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Foreground_Background>))]
        public string? AutomationType { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Windows_Portable>))]
        public string? TargetFramework { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            if (string.IsNullOrEmpty(Type))            { Type = "Template"; }
            if (string.IsNullOrEmpty(AutomationType))  { AutomationType = null; }
            if (string.IsNullOrEmpty(TargetFramework)) { TargetFramework = null; }

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                foreach (var name in Name!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = System.IO.Path.Combine(drive.NameColonSeparator, name);
                    if (ShouldProcess(target, "Add Machine"))
                    {
                        ExtendedMachine machine = null;
                        try
                        {
                            machine = new()
                            {
                                Name = WildcardPattern.Unescape(name),
                                Description = Description,
                                Type = Type,
                                NonProductionSlots = NonProductionSlots,
                                UnattendedSlots = UnattendedSlots,
                                TestAutomationSlots = TestAutomationSlots,
                                AutomationType = AutomationType,
                                TargetFramework = TargetFramework,
                            };

                            var newMachine = drive.OrchAPISession.AddMachine(machine);
                            drive._dicExtendedMachines = null;
                            if (newMachine != null)
                            {
                                newMachine.Path = drive.NameColonSeparator;
                                WriteObject(newMachine);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddMachineError", ErrorCategory.InvalidOperation, machine));
                        }
                    }
                }
            }
        }
    }
}
