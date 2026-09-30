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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? HeadlessSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? HostingSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AppTestSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? PerformanceTestSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AgentSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? FunctionSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AutomationCloudSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AutomationCloudTestAutomationSlots { get; set; }

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
    [ArgumentCompleter(typeof(TimeZoneIdCompleter))]
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

            // Fields the server does not know are dropped rather than sent: this is a PATCH,
            // and an API 18 server answers a body whose only field is AppTestSlots with
            // "machineDto must not be null". The check runs after the fetch above, because
            // ApiVersion is only published once the session has seen an API response.
            double? api = drive.OrchAPISession.ApiVersion;
            string driveTarget = drive.NameColonSeparator;
            var appTestSlots = MachineFieldVersions.SupportedOrNull(AppTestSlots, MachineFieldVersions.AppTestSlots, api, nameof(AppTestSlots), driveTarget, WriteWarning);
            var hostingSlots = MachineFieldVersions.SupportedOrNull(HostingSlots, MachineFieldVersions.HostingSlots, api, nameof(HostingSlots), driveTarget, WriteWarning);
            var performanceTestSlots = MachineFieldVersions.SupportedOrNull(PerformanceTestSlots, MachineFieldVersions.PerformanceTestSlots, api, nameof(PerformanceTestSlots), driveTarget, WriteWarning);
            var functionSlots = MachineFieldVersions.SupportedOrNull(FunctionSlots, MachineFieldVersions.FunctionSlots, api, nameof(FunctionSlots), driveTarget, WriteWarning);
            // The older fields (Tags, AutomationType, TargetFramework, RobotUsers,
            // UpdatePolicy, Maintenance*) are sent as before: their inherited "introduced in"
            // notes proved wrong on a real API 13.0 server, and a wrong constant blocks a
            // field that works. When such a field is the reason the server cannot bind the
            // body, DescribeUnboundPayload turns its unhelpful answer into a usable one.
            var automationCloudSlots = AutomationCloudSlots;
            var automationCloudTestAutomationSlots = AutomationCloudTestAutomationSlots;
            var tags = Tags;
            var automationType = AutomationType;
            var targetFramework = TargetFramework;
            var robotUsers = RobotUsers;
            var updatePolicyType = UpdatePolicyType;
            var updatePolicyVersion = UpdatePolicyVersion;
            var maintenanceCron = MaintenanceCron;
            var maintenanceDuration = MaintenanceDuration;
            var maintenanceEnabled = MaintenanceEnabled;
            var maintenanceTimeZone = MaintenanceTimeZone;
            var maintenanceTimeZoneId = MaintenanceTimeZoneId;

            var targetMachines = existingMachines.FilterByWildcards(m => m?.Name, wpName);

            foreach (var machine in targetMachines.OrderBy(m => m.Name)
                .WithProgressBar(this, $"Updating machines in {drive.NameColonSeparator}", m => m.Name)
                .WithCancellation(cancelHandler.Token))
            {
                if (machine.Scope == "AutomationCloudRobot")
                {
                    WriteWarning($"\"{machine.Name}\": Updating a machine with a Scope of 'AutomationCloudRobot' is not supported.");
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
                bool robotUsersSpecified = robotUsers is not null;
                RobotUser[]? resolvedRobotUsers = null;
                if (robotUsersSpecified)
                {
                    // CSV export joins robot users into one comma-separated cell, so split it (honoring
                    // backtick-escaped commas) to match New-OrchMachine -- otherwise a multi-user cell
                    // "Alice,Bob" bound as a single wildcard and matched no robot on re-import.
                    var processedRobotUsers = robotUsers!.SplitValuesByUnescapedCommasPreservingEscapes()?.ToArray();
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
                    !string.IsNullOrEmpty(maintenanceCron) ||
                    (maintenanceDuration is not null && maintenanceDuration != 0) ||
                    !string.IsNullOrEmpty(maintenanceEnabled) ||
                    !string.IsNullOrEmpty(maintenanceTimeZone);

                // Resolve the -MaintenanceTimeZone display name to a Windows timezone id (writes an
                // error on no/multiple match, exactly as before). Null when not supplied or unmatched.
                string? resolvedTimezoneIdFromName = null;
                if (!string.IsNullOrEmpty(maintenanceTimeZone))
                {
                    var tzProbe = new MaintenanceWindow();
                    tzProbe.AssignIdFromName(
                        maintenanceTimeZone,
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
                    HeadlessSlots = HeadlessSlots,
                    HostingSlots = hostingSlots,
                    AppTestSlots = appTestSlots,
                    PerformanceTestSlots = performanceTestSlots,
                    AgentSlots = AgentSlots,
                    FunctionSlots = functionSlots,
                    AutomationCloudSlots = automationCloudSlots,
                    AutomationCloudTestAutomationSlots = automationCloudTestAutomationSlots,
                    AutomationType = automationType,
                    TargetFramework = targetFramework,
                    UpdatePolicyType = updatePolicyType,
                    UpdatePolicyVersion = updatePolicyVersion,
                    Tags = tags,
                    RobotUsersSpecified = robotUsersSpecified,
                    ResolvedRobotUsers = resolvedRobotUsers,
                    MaintenanceSpecified = maintenanceSpecified,
                    MaintenanceCron = maintenanceCron,
                    MaintenanceDuration = maintenanceDuration,
                    MaintenanceEnabled = maintenanceEnabled,
                    MaintenanceTimeZoneId = maintenanceTimeZoneId,
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
                        // "machineDto must not be null" is what the PATCH endpoint answers when
                        // it cannot bind the body — typically because one property does not
                        // exist on that Orchestrator version. The error itself stays the API's
                        // own words; the reading of it goes to the warning stream beside it.
                        var hint = DescribeUnboundPayload(ex, patch, api);
                        if (hint is not null) WriteWarning($"\"{target}\": {hint}");
                        WriteError(new ErrorRecord(new OrchException(target, ex), "AddMachineError", ErrorCategory.InvalidOperation, machine));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds the warning that accompanies the PATCH endpoint's "machineDto must not be null":
    /// it names the properties the body carried and the server's API version, so the user can
    /// tell which one that Orchestrator does not model. The error record itself keeps the API's
    /// own words — this only reads them. Returns null for any other failure, leaving it alone.
    ///
    /// Verified causes: MaintenanceWindow and Tags on a 21.10.4 (API 13.0) server, AppTestSlots
    /// on a 24.10.11 Automation Suite (API 18.0).
    /// </summary>
    internal static string? DescribeUnboundPayload(Exception ex, ExtendedMachine payload, double? apiVersion)
    {
        if (ex.Message?.Contains("machineDto must not be null", StringComparison.OrdinalIgnoreCase) != true) return null;

        List<string> sent = [];
        if (payload.Description is not null) sent.Add(nameof(payload.Description));
        if (payload.UnattendedSlots is not null) sent.Add(nameof(payload.UnattendedSlots));
        if (payload.NonProductionSlots is not null) sent.Add(nameof(payload.NonProductionSlots));
        if (payload.TestAutomationSlots is not null) sent.Add(nameof(payload.TestAutomationSlots));
        if (payload.HeadlessSlots is not null) sent.Add(nameof(payload.HeadlessSlots));
        if (payload.HostingSlots is not null) sent.Add(nameof(payload.HostingSlots));
        if (payload.AppTestSlots is not null) sent.Add(nameof(payload.AppTestSlots));
        if (payload.PerformanceTestSlots is not null) sent.Add(nameof(payload.PerformanceTestSlots));
        if (payload.AgentSlots is not null) sent.Add(nameof(payload.AgentSlots));
        if (payload.FunctionSlots is not null) sent.Add(nameof(payload.FunctionSlots));
        if (payload.AutomationCloudSlots is not null) sent.Add(nameof(payload.AutomationCloudSlots));
        if (payload.AutomationCloudTestAutomationSlots is not null) sent.Add(nameof(payload.AutomationCloudTestAutomationSlots));
        if (payload.AutomationType is not null) sent.Add(nameof(payload.AutomationType));
        if (payload.TargetFramework is not null) sent.Add(nameof(payload.TargetFramework));
        if (payload.Tags is not null) sent.Add(nameof(payload.Tags));
        if (payload.RobotUsers is not null) sent.Add(nameof(payload.RobotUsers));
        if (payload.UpdatePolicy is not null) sent.Add(nameof(payload.UpdatePolicy));
        if (payload.MaintenanceWindow is not null) sent.Add(nameof(payload.MaintenanceWindow));

        string version = apiVersion is null ? "an unknown API version" : $"API {apiVersion}";
        return $"The Orchestrator could not bind the update payload ({version}). " +
               $"The request set: {string.Join(", ", sent)}. One of these properties most likely does not exist " +
               $"on this Orchestrator version — retry without it to find out which.";
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
        public int? HeadlessSlots { get; init; }
        public int? HostingSlots { get; init; }
        public int? AppTestSlots { get; init; }
        public int? PerformanceTestSlots { get; init; }
        public int? AgentSlots { get; init; }
        public int? FunctionSlots { get; init; }
        public int? AutomationCloudSlots { get; init; }
        public int? AutomationCloudTestAutomationSlots { get; init; }
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
        dirty |= payload.AssignNumberIfNotNull(input.HeadlessSlots, source, m => m.HeadlessSlots, (m, v) => m.HeadlessSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.HostingSlots, source, m => m.HostingSlots, (m, v) => m.HostingSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.AppTestSlots, source, m => m.AppTestSlots, (m, v) => m.AppTestSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.PerformanceTestSlots, source, m => m.PerformanceTestSlots, (m, v) => m.PerformanceTestSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.AgentSlots, source, m => m.AgentSlots, (m, v) => m.AgentSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.FunctionSlots, source, m => m.FunctionSlots, (m, v) => m.FunctionSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.AutomationCloudSlots, source, m => m.AutomationCloudSlots, (m, v) => m.AutomationCloudSlots = v);
        dirty |= payload.AssignNumberIfNotNull(input.AutomationCloudTestAutomationSlots, source, m => m.AutomationCloudTestAutomationSlots, (m, v) => m.AutomationCloudTestAutomationSlots = v);
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
