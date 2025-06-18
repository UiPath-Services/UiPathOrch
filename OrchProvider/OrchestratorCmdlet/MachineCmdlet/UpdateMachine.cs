using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

// TODO: ExtendedMachine.Scope == "AutomationCloudRobot" となっているマシンは
// 別のエンドポイントで更新が必要のようだ。
// https://cloud.uipath.com/yotsuda/svc1/orchestrator_/odata/CloudTemplates({machineId})
[Cmdlet(VerbsData.Update, "OrchMachine", SupportsShouldProcess = true)]
public class UpdateMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(MachineRobotUsersCompleter<TPositional>))]
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
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var results = ParallelResults3.GroupBy(drives, drive => drive.Machines.Get());

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
                postingMachine.AssignStringIfNotNull(Description,            (m, v) => m.Description = v);
                postingMachine.AssignNumberIfNotNull(UnattendedSlots,        (m, v) => m.UnattendedSlots = v);
                postingMachine.AssignNumberIfNotNull(NonProductionSlots,     (m, v) => m.NonProductionSlots = v);
                postingMachine.AssignNumberIfNotNull(TestAutomationSlots,    (m, v) => m.TestAutomationSlots = v);
                postingMachine.AssignStringIfNotNullOrEmpty(AutomationType,  (m, v) => m.AutomationType = v);
                postingMachine.AssignStringIfNotNullOrEmpty(TargetFramework, (m, v) => m.TargetFramework = v);

                if (RobotUsers is not null)
                {
                    // 変更されたかを正しく確認できるようにソートしておく
                    //machine.RobotUsers = machine?.RobotUsers?.OrderBy(r => r.UserName).ToList();
                    // と思ったけど、RobotUsers メンバの同一性の確認は無理な気がする。。ので諦める
                    // -RobotUsers が指定された場合には、必ず Machine を Update することになる
                    // ExtendedRobot class の Equals() では RobotUsers の同値性を確認せず
                    // ここで RobotUsers の UserName と RobotId だけを使って同値性を確認すれば
                    // まあできなくもないが、、今はちと面倒なのでそこまでしない

                    var robots = drive.AllRobotsAcrossFolders.Get();
                    var wpRobotUsers = RobotUsers.ConvertToWildcardPatternList();
                    var targetRobots = robots.FilterByWildcards(r => r?.User?.FullName, wpRobotUsers);
                    postingMachine.RobotUsers = targetRobots
                        .Select(r => new RobotUser()
                        {
                            UserName = r.Username,
                            RobotId = r.Id
                        })
                        .OrderBy(r => r.UserName)
                        .ToList();
                }

                postingMachine.AssignUpdatePolicy(UpdatePolicyType, UpdatePolicyVersion);

                postingMachine.AssignTags(Tags, (m, v) => m.Tags = v);

                if (postingMachine.Equals(machine)) continue;

                string target = machine?.GetPSPath();
                if (ShouldProcess(target, "Update Machine"))
                {
                    try
                    {
                        postingMachine.EndpointDetectionStatus = null;
                        postingMachine.Key = null;
                        postingMachine.RobotVersions = null;
                        postingMachine.UpdateInfo = null;

                        foreach (var ru in postingMachine.RobotUsers ?? [])
                        {
                            ru.HasTriggers = null;
                        }

                        if (!string.IsNullOrEmpty(MaintenanceCron) ||
                            (MaintenanceDuration is not null && MaintenanceDuration != 0) ||
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
