using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;

using System;

namespace UiPath.PowerShell.Commands
{
    // TODO: ExtendedMachine.Scope == "AutomationCloudRobot" となっているマシンは
    // 別のエンドポイントで更新が必要のようだ。
    // https://cloud.uipath.com/yotsuda/svc1/orchestrator_/odata/CloudTemplates({machineId})
    [Cmdlet(VerbsData.Update, "OrchMachine", SupportsShouldProcess = true)]
    public class UpdateMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter<Positional.Name>))]
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
        // AutomationType

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
        public string[]? Tags { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        // マシンの Tags 専用
        private class TagsCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Id は、候補から除外する
                var wpName = CreateWPListFromOtherParameters(commandAst, "Name", Positional.Name.Parameters);

                var results = ParallelResults.ForEach(drives, drive => drive.Machines.Get());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var release in entities!
                        .FilterByWildcards(p => p?.Name, wpName)
                        .OrderBy(p => p.Name))
                    {
                        if (release?.Tags == null) continue;

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
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpName = Name.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                var existingMachines = drive.Machines.Get();
                var targetMachines = existingMachines.FilterByWildcards(m => m?.Name, wpName);

                foreach (var machine in targetMachines.OrderBy(m => m.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (machine.Scope == "AutomationCloudRobot")
                    {
                        WriteWarning($"{machine.Name}: Updating a machine with a Scope of 'AutomationCloudRobot' is not supported.");
                        continue;
                    }

                    var postingMachine = OrchCollectionExtensions.DeepCopy(machine);
                    postingMachine.AssignStringIfNotNull(Description,         (m, v) => m.Description = v);
                    postingMachine.AssignNumberIfNotNull(UnattendedSlots,     (m, v) => m.UnattendedSlots = v);
                    postingMachine.AssignNumberIfNotNull(NonProductionSlots,  (m, v) => m.NonProductionSlots = v);
                    postingMachine.AssignNumberIfNotNull(TestAutomationSlots, (m, v) => m.TestAutomationSlots = v);

                    postingMachine.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);

                    postingMachine.AssignTags(Tags, (m, v) => m.Tags = v);

                    if (postingMachine.Equals(machine)) continue;

                    string target = machine.GetPSPath();
                    if (ShouldProcess(target, "Update Machine"))
                    {
                        try
                        {
                            postingMachine.AutomationType = null;
                            postingMachine.EndpointDetectionStatus = null;
                            postingMachine.Key = null;
                            postingMachine.RobotVersions = null;
                            postingMachine.TargetFramework = null;
                            postingMachine.UpdateInfo = null;

                            foreach (var ru in postingMachine.RobotUsers ?? [])
                            {
                                ru.HasTriggers = null;
                            }

                            if (!string.IsNullOrEmpty(MaintenanceCron) ||
                                (MaintenanceDuration != null && MaintenanceDuration != 0) ||
                                !string.IsNullOrEmpty(MaintenanceEnabled) ||
                                !string.IsNullOrEmpty(MaintenanceTimeZone))
                            {
                                postingMachine.MaintenanceWindow ??= new();
                                postingMachine.MaintenanceWindow.AssignStringIfNotNull(MaintenanceCron, (m, v) => m.CronExpression = v);
                                postingMachine.MaintenanceWindow.AssignNumberIfNotNullOrZero(MaintenanceDuration, (m, v) => m.Duration = v);
                                postingMachine.MaintenanceWindow.AssignBoolIfNotNull(MaintenanceEnabled, (m, v) => m.Enabled = v);

                                // TODO: AddTrigger.cs にも同じ処理がある。共通化したい
                                #region TimeZone を TimeZoneId に変換
                                postingMachine.MaintenanceWindow.AssignStringIfNotNullOrEmpty(MaintenanceTimeZoneId, (m, v) => m.TimezoneId = v);

                                postingMachine.MaintenanceWindow.AssignIdFromName(
                                    MaintenanceTimeZone,
                                    TimeZoneInfo.GetSystemTimeZones,
                                    e => e.DisplayName,
                                    e => e.Id!,
                                    (m, v) => m.TimezoneId = v,
                                    this, target, "TimeZone");

                                postingMachine.MaintenanceWindow.TimezoneId ??= TimeZoneInfo.Local.Id;
                                #endregion
                            }

                            drive.OrchAPISession.PatchMachine(postingMachine);
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
}
