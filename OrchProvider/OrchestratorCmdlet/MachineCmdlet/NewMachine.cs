using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.New, "OrchMachine", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.CreatedMachine))]
    public class AddMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Description { get; set; }

        // 現在は Template と Standard と Serverless をサポート
        // AutomationCloudRobot もサポートしなければ。
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Template_Standard_Serverless>))]
        public string? Type { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Default_Serverless_AutomationCloudRobot>))]
        public string? Scope { get; set; }

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
        [ArgumentCompleter(typeof(MachineRobotUsersCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? RobotUsers { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[]? Tags { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            RobotUsers = RobotUsers?.Split1stValueByUnescapedCommas()?.ToArray();

            if (string.IsNullOrEmpty(Type))            { Type = "Template"; }
            if (string.IsNullOrEmpty(AutomationType))  { AutomationType = null; }
            if (string.IsNullOrEmpty(TargetFramework)) { TargetFramework = null; }

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                foreach (var name in Name!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (Scope == "PersonalWorkspace")
                    {
                        WriteWarning($"{drive.NameColonSeparator}{name}: Machines with the \"Scope\" set to \"PersonalWorkspace\" cannot be added with this cmdlet. Please enable the personal workspace using the Enable-OrchPersonalWorkspace cmdlet.");
                        continue;
                    }

                    string target = System.IO.Path.Combine(drive.NameColonSeparator, name);
                    if (ShouldProcess(target, "Add Machine"))
                    {
                       List<RobotUser>? lstRobotUsers = null;
                       if (RobotUsers != null)
                       {
                            var robots = drive.AllRobotsAcrossFolders.Get();
                            var wpRobotUsers = RobotUsers.ConvertToWildcardPatternList();
                            var targetRobots = robots.FilterByWildcards(r => r?.User?.FullName, wpRobotUsers);
                            lstRobotUsers = targetRobots
                                .Select(r => new RobotUser()
                                {
                                    UserName = r.Username,
                                    RobotId = r.Id
                                })
                                .OrderBy(r => r.UserName)
                                .ToList();
                        }

                        ExtendedMachine machine = null;
                        try
                        {
                            if (Scope == "Serverless")
                            {
                                TargetFramework ??= "Portable";
                            }

                            machine = new()
                            {
                                Name = WildcardPattern.Unescape(name),
                                Description = Description,
                                Type = Type,
                                Scope = Scope,
                                NonProductionSlots = NonProductionSlots,
                                UnattendedSlots = UnattendedSlots,
                                TestAutomationSlots = TestAutomationSlots,
                                AutomationType = AutomationType,
                                TargetFramework = TargetFramework,
                                RobotUsers = lstRobotUsers
                            };

                            machine.AssignTags(Tags, (m, v) => m.Tags = v);

                            var newMachine = drive.OrchAPISession.AddMachine(machine);
                            drive.Machines.ClearCache();
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
