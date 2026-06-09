using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// TODO: Machines with ExtendedMachine.Scope == "AutomationCloudRobot"
// appear to require updates via a different endpoint.
// https://cloud.uipath.com/yotsuda/svc1/orchestrator_/odata/CloudTemplates({machineId})
[Cmdlet(VerbsData.Update, "OrchMachine", SupportsShouldProcess = true)]
public class UpdateMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // Type: Standard
    // Scope: Default
    // LicenseKey: Guid

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? UnattendedSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? NonProductionSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? TestAutomationSlots { get; set; }

    // HeadlessSlots
    // AutomationCloudSlots
    // AutomationCloudTestAutomationSlots

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Foreground_Background>))]
    public string? AutomationType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Windows_Portable>))]
    public string? TargetFramework { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineRobotUsersCompleter))]
    [SupportsWildcards]
    public string[]? RobotUsers { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<UserUpdatePolicyItems>))]
    public string? UpdatePolicyType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UpdatePolicyVersionCompleter))]
    public string? UpdatePolicyVersion { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? MaintenanceCron { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? MaintenanceDuration { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? MaintenanceEnabled { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeZoneCompleter))]
    [SupportsWildcards]
    public string? MaintenanceTimeZone { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? MaintenanceTimeZoneId { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TagsCompleter))]
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Dedicated to machine Tags
    private class TagsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Names already selected via parameter from the candidates
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drives, drive => drive.Machines.Get());

            foreach (var result in results)
            {
                foreach (var release in result
                    .FilterByWildcards(p => p?.Name, wpName)
                    .OrderBy(p => p.Name))
                {
                    if (release?.Tags is null) continue;

                    var values = release.Tags.ConvertToString();
                    if (string.IsNullOrEmpty(values)) continue;

                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult(PathTools.EscapePSText(values), values, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var existingMachines = drive.Machines.Get();
            var targetMachines = existingMachines.FilterByWildcards(m => m?.Name, wpName);

            foreach (var machine in targetMachines.OrderBy(m => m.Name).WithCancellation(cancelHandler.Token))
            {
                if (machine.Scope == "AutomationCloudRobot")
                {
                    WriteWarning($"{machine.Name}: Updating a machine with a Scope of 'AutomationCloudRobot' is not supported.");
                    continue;
                }

                // Build a PATCH payload with only the properties that need updating.
                // Since PatchMachine uses HTTP PATCH and null properties are excluded
                // from JSON serialization (WhenWritingNull), only specified parameters are sent.
                var patch = new ExtendedMachine { Id = machine.Id };
                bool dirty = false;

                dirty |= patch.AssignStringIfNotNull(Description, machine, m => m.Description, (m, v) => m.Description = v);
                dirty |= patch.AssignNumberIfNotNull(UnattendedSlots, machine, m => m.UnattendedSlots, (m, v) => m.UnattendedSlots = v);
                dirty |= patch.AssignNumberIfNotNull(NonProductionSlots, machine, m => m.NonProductionSlots, (m, v) => m.NonProductionSlots = v);
                dirty |= patch.AssignNumberIfNotNull(TestAutomationSlots, machine, m => m.TestAutomationSlots, (m, v) => m.TestAutomationSlots = v);
                dirty |= patch.AssignStringIfNotNull(AutomationType, machine, m => m.AutomationType, (m, v) => m.AutomationType = v);
                dirty |= patch.AssignStringIfNotNull(TargetFramework, machine, m => m.TargetFramework, (m, v) => m.TargetFramework = v);

                if (RobotUsers is not null)
                {
                    var robots = drive.AllRobotsAcrossFolders.Get();
                    var wpRobotUsers = RobotUsers.ConvertToWildcardPatternList();
                    var targetRobots = robots.FilterByWildcards(r => r?.User?.FullName, wpRobotUsers);
                    patch.RobotUsers = targetRobots
                        .Select(r => new RobotUser()
                        {
                            UserName = r.Username,
                            RobotId = r.Id
                        })
                        .OrderBy(r => r.UserName)
                        .ToList();
                    dirty = true;
                }

                if (!string.IsNullOrEmpty(UpdatePolicyType) || !string.IsNullOrEmpty(UpdatePolicyVersion))
                {
                    patch.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);
                    dirty = true;
                }

                if (Tags is not null)
                {
                    patch.AssignTags(Tags, (m, v) => m.Tags = v);
                    dirty = true;
                }

                if (!string.IsNullOrEmpty(MaintenanceCron) ||
                    (MaintenanceDuration is not null && MaintenanceDuration != 0) ||
                    !string.IsNullOrEmpty(MaintenanceEnabled) ||
                    !string.IsNullOrEmpty(MaintenanceTimeZone))
                {
                    patch.MaintenanceWindow = machine.MaintenanceWindow ?? new();
                    patch.MaintenanceWindow.AssignStringIfNotNull(MaintenanceCron, (m, v) => m.CronExpression = v);
                    patch.MaintenanceWindow.AssignNumberIfNotNullOrZero(MaintenanceDuration, (m, v) => m.Duration = v);
                    patch.MaintenanceWindow.AssignBoolIfNotNull(MaintenanceEnabled, (m, v) => m.Enabled = v);

                    #region Convert TimeZone to TimeZoneId
                    patch.MaintenanceWindow.AssignStringIfNotNull(MaintenanceTimeZoneId, (m, v) => m.TimezoneId = v);

                    patch.MaintenanceWindow.AssignIdFromName(
                        MaintenanceTimeZone,
                        TimeZoneInfo.GetSystemTimeZones,
                        e => e.DisplayName,
                        e => e.Id!,
                        (m, v) => m.TimezoneId = v,
                        this, machine.GetPSPath(), "TimeZone");

                    patch.MaintenanceWindow.TimezoneId ??= TimeZoneInfo.Local.Id;
                    #endregion
                    dirty = true;
                }

                if (!dirty) continue;

                string target = machine.GetPSPath();
                if (ShouldProcess(target, "Update Machine"))
                {
                    try
                    {
                        drive.OrchAPISession.PatchMachine(patch);
                        drive.Machines.ClearCache();
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
