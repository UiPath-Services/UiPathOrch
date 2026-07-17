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
    [RobotUserArgumentTransformation]
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

            foreach (var machine in targetMachines.OrderBy(m => m.Name)
                .WithProgressBar(this, $"Updating machines in {drive.NameColonSeparator}", m => m.Name)
                .WithCancellation(cancelHandler.Token))
            {
                if (machine.Scope == "AutomationCloudRobot")
                {
                    WriteWarning($"{machine.Name}: Updating a machine with a Scope of 'AutomationCloudRobot' is not supported.");
                    continue;
                }

                // Build a PATCH payload with only the properties that need updating.
                // Since PatchMachine uses HTTP PATCH and null properties are excluded
                // from JSON serialization (WhenWritingNull), only specified parameters are sent.
                // Everything that needs an API/host round-trip (robot-user resolution, the
                // timezone display-name -> id lookup) is resolved here; the change decision itself
                // is the pure, API-free ComputeMachineUpdate core (unit-tested per field).
                var patch = new ExtendedMachine { Id = machine.Id };

                // Resolve -RobotUsers to concrete assignments (API).
                bool robotUsersSpecified = RobotUsers is not null;
                RobotUser[]? resolvedRobotUsers = null;
                if (robotUsersSpecified)
                {
                    // CSV export joins robot users into one comma-separated cell, so split it (honoring
                    // backtick-escaped commas) to match New-OrchMachine -- otherwise a multi-user cell
                    // "Alice,Bob" bound as a single wildcard and matched no robot on re-import.
                    var processedRobotUsers = RobotUsers!.SplitValuesByUnescapedCommasPreservingEscapes()?.ToArray();
                    if (processedRobotUsers is not null && processedRobotUsers.All(string.IsNullOrWhiteSpace))
                    {
                        processedRobotUsers = null;
                    }

                    if (processedRobotUsers is null)
                    {
                        // -RobotUsers supplied but empty (e.g. an empty CSV cell) clears the assignment;
                        // do NOT fall through to FilterByWildcards, whose empty-pattern set matches ALL.
                        resolvedRobotUsers = [];
                    }
                    else
                    {
                        var robots = drive.AllRobotsAcrossFolders.Get();
                        var wpRobotUsers = processedRobotUsers.ConvertToWildcardPatternList();
                        // Match on User.FullName (the CSV / manual form) OR Id (the object-pipe form a
                        // piped RobotUser is transformed to), so Get-OrchMachine | Update-OrchMachine works.
                        var targetRobots = robots.FilterByWildcardsAny([r => r?.User?.FullName, r => r?.Id?.ToString()], wpRobotUsers);
                        resolvedRobotUsers = targetRobots
                            .Select(r => new RobotUser()
                            {
                                UserName = r.Username,
                                RobotId = r.Id
                            })
                            .OrderBy(r => r.UserName)
                            .ToArray();
                    }
                }

                // The maintenance block runs when any of Cron/Duration/Enabled/TimeZone(name) is given
                // (matching the original guard; the hidden -MaintenanceTimeZoneId does not itself trigger it).
                bool maintenanceSpecified =
                    !string.IsNullOrEmpty(MaintenanceCron) ||
                    (MaintenanceDuration is not null && MaintenanceDuration != 0) ||
                    !string.IsNullOrEmpty(MaintenanceEnabled) ||
                    !string.IsNullOrEmpty(MaintenanceTimeZone);

                // Resolve the -MaintenanceTimeZone display name to a Windows timezone id (writes an
                // error on no/multiple match, exactly as before). Null when not supplied or unmatched.
                string? resolvedTimezoneIdFromName = null;
                if (!string.IsNullOrEmpty(MaintenanceTimeZone))
                {
                    var tzProbe = new MaintenanceWindow();
                    tzProbe.AssignIdFromName(
                        MaintenanceTimeZone,
                        TimeZoneInfo.GetSystemTimeZones,
                        e => e.DisplayName,
                        e => e.Id!,
                        (m, v) => m.TimezoneId = v,
                        this, machine.GetPSPath(), "TimeZone");
                    resolvedTimezoneIdFromName = tzProbe.TimezoneId;
                }

                bool dirty = ComputeMachineUpdate(patch, machine, new MachineUpdateInputs
                {
                    Description = Description,
                    UnattendedSlots = UnattendedSlots,
                    NonProductionSlots = NonProductionSlots,
                    TestAutomationSlots = TestAutomationSlots,
                    AutomationType = AutomationType,
                    TargetFramework = TargetFramework,
                    UpdatePolicyType = UpdatePolicyType,
                    UpdatePolicyVersion = UpdatePolicyVersion,
                    Tags = Tags,
                    RobotUsersSpecified = robotUsersSpecified,
                    ResolvedRobotUsers = resolvedRobotUsers,
                    MaintenanceSpecified = maintenanceSpecified,
                    MaintenanceCron = MaintenanceCron,
                    MaintenanceDuration = MaintenanceDuration,
                    MaintenanceEnabled = MaintenanceEnabled,
                    MaintenanceTimeZoneId = MaintenanceTimeZoneId,
                    ResolvedTimezoneIdFromName = resolvedTimezoneIdFromName,
                });

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

    /// <summary>
    /// Pure inputs for <see cref="ComputeMachineUpdate"/>. Everything that needs an API/host
    /// round-trip (robot-user resolution, the timezone display-name -> id lookup) is resolved by
    /// the cmdlet first and passed in here, so change detection is fully testable without a live
    /// Orchestrator.
    /// </summary>
    internal sealed class MachineUpdateInputs
    {
        public string? Description { get; init; }
        public int? UnattendedSlots { get; init; }
        public int? NonProductionSlots { get; init; }
        public int? TestAutomationSlots { get; init; }
        public string? AutomationType { get; init; }
        public string? TargetFramework { get; init; }

        public string? UpdatePolicyType { get; init; }
        public string? UpdatePolicyVersion { get; init; }

        public string[]? Tags { get; init; }

        /// <summary>True when -RobotUsers was bound at all.</summary>
        public bool RobotUsersSpecified { get; init; }
        /// <summary>Resolved robot-user assignments (empty array clears; null when -RobotUsers not bound).</summary>
        public RobotUser[]? ResolvedRobotUsers { get; init; }

        /// <summary>True when any of Cron/Duration/Enabled/TimeZone(name) was supplied (matches the original guard).</summary>
        public bool MaintenanceSpecified { get; init; }
        public string? MaintenanceCron { get; init; }
        public int? MaintenanceDuration { get; init; }
        public string? MaintenanceEnabled { get; init; }
        /// <summary>The hidden -MaintenanceTimeZoneId parameter (a direct Windows tz id).</summary>
        public string? MaintenanceTimeZoneId { get; init; }
        /// <summary>The -MaintenanceTimeZone display name resolved to a Windows tz id, or null (not supplied / unmatched).</summary>
        public string? ResolvedTimezoneIdFromName { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a fresh PATCH body) and returns
    /// whether anything actually changed versus <paramref name="source"/> (the current machine). Only a
    /// real difference flips the result true, so the caller can skip the PATCH — and its audit entry —
    /// when the request is a no-op. No API access, so this is unit-testable in isolation.
    /// </summary>
    internal static bool ComputeMachineUpdate(ExtendedMachine payload, ExtendedMachine source, MachineUpdateInputs input)
    {
        bool dirty = false;

        dirty |= payload.AssignStringIfNotNull(input.Description, source, m => m.Description, (m, v) => m.Description = v);
        dirty |= payload.AssignNumberIfNotNull(input.UnattendedSlots, source, m => m.UnattendedSlots, (m, v) => m.UnattendedSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.NonProductionSlots, source, m => m.NonProductionSlots, (m, v) => m.NonProductionSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.TestAutomationSlots, source, m => m.TestAutomationSlots, (m, v) => m.TestAutomationSlots = v);
        dirty |= payload.AssignStringIfNotNull(input.AutomationType, source, m => m.AutomationType, (m, v) => m.AutomationType = v);
        dirty |= payload.AssignStringIfNotNull(input.TargetFramework, source, m => m.TargetFramework, (m, v) => m.TargetFramework = v);

        // RobotUsers: write only when the assignment set actually differs from the current one.
        if (input.RobotUsersSpecified)
        {
            var newRobotUsers = input.ResolvedRobotUsers ?? [];
            if (!OrchStringExtensions.UnorderedEquals(source.RobotUsers, newRobotUsers, r => $"{r.RobotId}|{r.UserName}"))
            {
                payload.RobotUsers = newRobotUsers;
                dirty = true;
            }
        }

        if (!string.IsNullOrEmpty(input.UpdatePolicyType) || !string.IsNullOrEmpty(input.UpdatePolicyVersion))
        {
            payload.AssignUpdatePolicy(input.UpdatePolicyType, input.UpdatePolicyVersion);
            if (!OrchStringExtensions.UpdatePolicyEquals(source.UpdatePolicy, payload.UpdatePolicy))
                dirty = true;
        }

        if (input.Tags is not null)
        {
            dirty |= payload.AssignTags(input.Tags, source, m => m.Tags, (m, v) => m.Tags = v);
        }

        if (input.MaintenanceSpecified)
        {
            // Apply onto a copy of the current window (never the source's own object) so an unchanged
            // request stays a no-op and we don't mutate the cache in place.
            var mwSource = source.MaintenanceWindow;
            var mw = mwSource is not null ? OrchCollectionExtensions.DeepCopy(mwSource) : new MaintenanceWindow();
            mw.AssignStringIfNotNull(input.MaintenanceCron, (m, v) => m.CronExpression = v);
            mw.AssignNumberIfNotNullOrZero(input.MaintenanceDuration, (m, v) => m.Duration = v);
            mw.AssignBoolIfNotNull(input.MaintenanceEnabled, (m, v) => m.Enabled = v);

            // TimeZone: the hidden direct id first, then the resolved display-name id (either can be
            // absent), then default to the local zone — matching the original resolution order.
            mw.AssignStringIfNotNull(input.MaintenanceTimeZoneId, (m, v) => m.TimezoneId = v);
            if (input.ResolvedTimezoneIdFromName is not null) mw.TimezoneId = input.ResolvedTimezoneIdFromName;
            mw.TimezoneId ??= TimeZoneInfo.Local.Id;

            // Include the window in the PATCH only when it actually changed.
            if (!OrchStringExtensions.MaintenanceWindowEquals(mwSource, mw))
            {
                payload.MaintenanceWindow = mw;
                dirty = true;
            }
        }

        return dirty;
    }
}
