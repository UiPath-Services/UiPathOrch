using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// Update-OrchApiTrigger -- wraps PUT /odata/HttpTriggers({id}).
//
// PUT URL confirmed against browser dev-tools capture (yotsuda
// tenant 2026-05-21); the id segment is the HttpTrigger.Id GUID
// string. The mutation surface mirrors New-OrchApiTrigger, plus -NewName
// (rename) and dirty-detection so a no-op invocation skips the PUT.
[Cmdlet(VerbsData.Update, "OrchApiTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(HttpTrigger))]
public class UpdateApiTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string? Release { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    // --- HTTP-trigger-specific fields ---

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<HttpMethodItems>))]
    public string? Method { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Slug { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<HttpCallingModeItems>))]
    public string? CallingMode { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? RunAsCaller { get; set; }

    // --- TriggerBase fields ---
    // Kept to the fields the Orchestrator web "edit API trigger" form
    // exposes. See New-OrchApiTrigger for why callbacks / JobPriority /
    // RunAsMe / TargetFramework / RequiresUserInteraction /
    // JobFailuresGracePeriodInHours are intentionally not exposed.

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RuntimeTypes>))]
    public string? RuntimeType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ResumeOnSameContext { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<SoftStop_Kill>))]
    public string? StopStrategy { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? StopJobAfterSeconds { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? KillJobAfterSeconds { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AlertPendingJobAfterSeconds { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AlertRunningJobAfterSeconds { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RemoteControlAccessItems>))]
    public string? RemoteControlAccess { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ConsecutiveJobFailuresThreshold { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? InputArguments { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerMachineRobotsCompleter))]
    public string[]? MachineRobots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<HttpTrigger>? triggers = null;
            try
            {
                triggers = drive.ApiTriggers.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, folder));
            }
            if (triggers is null) continue;

            var targetTriggers = triggers.SelectByWildcards(t => t?.Name, wpName).OrderBy(t => t.Name);

            foreach (var trigger in targetTriggers
                .WithProgressBar(this, $"Updating API triggers in {folder.GetPSPath()}", t => t.Name)
                .WithCancellation(cancelHandler.Token))
            {
                string target = trigger.GetPSPath();

                // Deep-copy the existing entity so we can mutate without
                // affecting the cached copy, then dirty-detect (in the pure,
                // API-free ComputeApiTriggerUpdate core) to skip PUT when no
                // field actually changed.
                HttpTrigger postTrigger = OrchCollectionExtensions.DeepCopy(trigger);

                // Resolve the API-dependent inputs up front, then hand everything to the pure core.
                MachineRobotSession[]? resolvedMachineRobots = null;
                if (MachineRobots is not null)
                {
                    resolvedMachineRobots = DeserializeMachineRobotSessions(this, drive, folder, target, MachineRobots, out bool mrParseFailed);
                    if (mrParseFailed) continue; // malformed -MachineRobots: error already written, skip this trigger
                }

                // Resolve Release name -> ReleaseKey (writes the same not-found / ambiguous error as
                // before via AssignIdFromName). The setter only captures the resolved key + a resolved
                // flag; the actual diff happens in the core. A null -Release leaves the setter uncalled
                // (releaseResolved stays false → skipped); an empty -Release resolves to a null key
                // (clears, matching the previous behavior).
                bool releaseResolved = false;
                string? resolvedReleaseKey = null;
                postTrigger.AssignIdFromName(
                    Release,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Key!,
                    (t, v) => { resolvedReleaseKey = v; releaseResolved = true; },
                    this, target, "Release");

                bool dirty = ComputeApiTriggerUpdate(postTrigger, trigger, new ApiTriggerUpdateInputs
                {
                    NewName = NewName,
                    Method = Method,
                    Slug = Slug,
                    CallingMode = CallingMode,
                    RunAsCaller = RunAsCaller,
                    Description = Description,
                    Enabled = Enabled,
                    RuntimeType = RuntimeType,
                    ResumeOnSameContext = ResumeOnSameContext,
                    StopStrategy = StopStrategy,
                    StopJobAfterSeconds = StopJobAfterSeconds,
                    KillJobAfterSeconds = KillJobAfterSeconds,
                    AlertPendingJobAfterSeconds = AlertPendingJobAfterSeconds,
                    AlertRunningJobAfterSeconds = AlertRunningJobAfterSeconds,
                    RemoteControlAccess = RemoteControlAccess,
                    ConsecutiveJobFailuresThreshold = ConsecutiveJobFailuresThreshold,
                    InputArguments = InputArguments,
                    MachineRobotsSpecified = MachineRobots is not null,
                    ResolvedMachineRobots = resolvedMachineRobots,
                    ReleaseResolved = releaseResolved,
                    ResolvedReleaseKey = resolvedReleaseKey,
                });

                if (!dirty)
                {
                    continue;
                }

                if (ShouldProcess(target, "Update ApiTrigger"))
                {
                    try
                    {
                        drive.OrchAPISession.UpdateHttpTrigger(folder.Id!.Value, postTrigger);
                        drive.ApiTriggers.ClearCache(folder);
                        // PUT returns no body; re-fetch the updated trigger
                        // so the caller's output is fresh.
                        var updated = drive.OrchAPISession.GetHttpTrigger(folder.Id!.Value, postTrigger.Id!);
                        if (updated is not null)
                        {
                            updated.Path = folder.GetPSPath();
                            WriteObject(updated);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateApiTriggerError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Pure inputs for <see cref="ComputeApiTriggerUpdate"/>. The MachineRobots deserialization and
    /// the Release name -> key lookup (API round-trips) are resolved by the cmdlet first and passed
    /// in here, so change detection is fully testable without a live Orchestrator.
    /// </summary>
    internal sealed class ApiTriggerUpdateInputs
    {
        public string? NewName { get; init; }
        public string? Method { get; init; }
        public string? Slug { get; init; }
        public string? CallingMode { get; init; }
        public string? RunAsCaller { get; init; }
        public string? Description { get; init; }
        public string? Enabled { get; init; }
        public string? RuntimeType { get; init; }
        public string? ResumeOnSameContext { get; init; }
        public string? StopStrategy { get; init; }
        public int? StopJobAfterSeconds { get; init; }
        public int? KillJobAfterSeconds { get; init; }
        public int? AlertPendingJobAfterSeconds { get; init; }
        public int? AlertRunningJobAfterSeconds { get; init; }
        public string? RemoteControlAccess { get; init; }
        public int? ConsecutiveJobFailuresThreshold { get; init; }
        public string? InputArguments { get; init; }

        public bool MachineRobotsSpecified { get; init; }
        public MachineRobotSession[]? ResolvedMachineRobots { get; init; }

        /// <summary>True when -Release resolved to a key (an unresolved name left it false → skipped).</summary>
        public bool ReleaseResolved { get; init; }
        public string? ResolvedReleaseKey { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a deep copy of
    /// <paramref name="source"/>) and returns whether anything actually changed, so the caller can
    /// skip the PUT — and the full-record audit entry it produces — when the request is a no-op.
    /// Reproduces the cmdlet's field order exactly. No API access — unit-testable in isolation.
    /// </summary>
    internal static bool ComputeApiTriggerUpdate(HttpTrigger payload, HttpTrigger source, ApiTriggerUpdateInputs input)
    {
        bool dirty = false;

        // HttpTrigger-specific
        dirty |= payload.AssignStringIfNotNull(input.NewName, source, t => t.Name, (t, v) => t.Name = v);
        dirty |= payload.AssignStringIfNotNull(input.Method, source, t => t.Method, (t, v) => t.Method = v);
        dirty |= payload.AssignStringIfNotNull(input.Slug, source, t => t.Slug, (t, v) => t.Slug = v);
        dirty |= payload.AssignStringIfNotNull(input.CallingMode, source, t => t.CallingMode, (t, v) => t.CallingMode = v);
        dirty |= payload.AssignBoolIfNotNull(input.RunAsCaller, source, t => t.RunAsCaller, (t, v) => t.RunAsCaller = v);

        // TriggerBase
        dirty |= payload.AssignStringIfNotNull(input.Description, source, t => t.Description, (t, v) => t.Description = v);
        dirty |= payload.AssignBoolIfNotNull(input.Enabled, source, t => t.Enabled, (t, v) => t.Enabled = v);
        dirty |= payload.AssignStringIfNotNull(input.RuntimeType, source, t => t.RuntimeType, (t, v) => t.RuntimeType = v);
        dirty |= payload.AssignBoolIfNotNull(input.ResumeOnSameContext, source, t => t.ResumeOnSameContext, (t, v) => t.ResumeOnSameContext = v);
        dirty |= payload.AssignStringIfNotNull(input.StopStrategy, source, t => t.StopStrategy, (t, v) => t.StopStrategy = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.StopJobAfterSeconds, source, t => t.StopJobAfterSeconds, (t, v) => t.StopJobAfterSeconds = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.KillJobAfterSeconds, source, t => t.KillJobAfterSeconds, (t, v) => t.KillJobAfterSeconds = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.AlertPendingJobAfterSeconds, source, t => t.AlertPendingJobAfterSeconds, (t, v) => t.AlertPendingJobAfterSeconds = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.AlertRunningJobAfterSeconds, source, t => t.AlertRunningJobAfterSeconds, (t, v) => t.AlertRunningJobAfterSeconds = v);
        dirty |= payload.AssignStringIfNotNull(input.RemoteControlAccess, source, t => t.RemoteControlAccess, (t, v) => t.RemoteControlAccess = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.ConsecutiveJobFailuresThreshold, source, t => t.ConsecutiveJobFailuresThreshold, (t, v) => t.ConsecutiveJobFailuresThreshold = v);
        dirty |= payload.AssignStringIfNotNull(input.InputArguments, source, t => t.InputArguments, (t, v) => t.InputArguments = v);

        if (input.MachineRobotsSpecified)
        {
            // Compare on the fields the deserialized form carries (MachineId, RobotId, SessionId)
            // so re-sending the same assignment is a no-op instead of a full re-write.
            if (!OrchStringExtensions.UnorderedEquals(source.MachineRobots, input.ResolvedMachineRobots, m => $"{m.MachineId}|{m.RobotId}|{m.SessionId}"))
            {
                payload.MachineRobots = input.ResolvedMachineRobots;
                dirty = true;
            }
        }

        // Release name -> key resolved by the cmdlet; diff against the source, matching the original
        // inline `if (source.ReleaseKey != v)` guard.
        if (input.ReleaseResolved && source.ReleaseKey != input.ResolvedReleaseKey)
        {
            payload.ReleaseKey = input.ResolvedReleaseKey;
            dirty = true;
        }

        return dirty;
    }
}
