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
                // affecting the cached copy, then dirty-detect to skip PUT
                // when no field actually changed.
                HttpTrigger postTrigger = OrchCollectionExtensions.DeepCopy(trigger);

                bool dirty = false;

                // HttpTrigger-specific
                dirty |= postTrigger.AssignStringIfNotNull(NewName, trigger, t => t.Name, (t, v) => t.Name = v);
                dirty |= postTrigger.AssignStringIfNotNull(Method, trigger, t => t.Method, (t, v) => t.Method = v);
                dirty |= postTrigger.AssignStringIfNotNull(Slug, trigger, t => t.Slug, (t, v) => t.Slug = v);
                dirty |= postTrigger.AssignStringIfNotNull(CallingMode, trigger, t => t.CallingMode, (t, v) => t.CallingMode = v);
                dirty |= postTrigger.AssignBoolIfNotNull(RunAsCaller, trigger, t => t.RunAsCaller, (t, v) => t.RunAsCaller = v);

                // TriggerBase
                dirty |= postTrigger.AssignStringIfNotNull(Description, trigger, t => t.Description, (t, v) => t.Description = v);
                dirty |= postTrigger.AssignBoolIfNotNull(Enabled, trigger, t => t.Enabled, (t, v) => t.Enabled = v);
                dirty |= postTrigger.AssignStringIfNotNull(RuntimeType, trigger, t => t.RuntimeType, (t, v) => t.RuntimeType = v);
                dirty |= postTrigger.AssignBoolIfNotNull(ResumeOnSameContext, trigger, t => t.ResumeOnSameContext, (t, v) => t.ResumeOnSameContext = v);
                dirty |= postTrigger.AssignStringIfNotNull(StopStrategy, trigger, t => t.StopStrategy, (t, v) => t.StopStrategy = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(StopJobAfterSeconds, trigger, t => t.StopJobAfterSeconds, (t, v) => t.StopJobAfterSeconds = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(KillJobAfterSeconds, trigger, t => t.KillJobAfterSeconds, (t, v) => t.KillJobAfterSeconds = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(AlertPendingJobAfterSeconds, trigger, t => t.AlertPendingJobAfterSeconds, (t, v) => t.AlertPendingJobAfterSeconds = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(AlertRunningJobAfterSeconds, trigger, t => t.AlertRunningJobAfterSeconds, (t, v) => t.AlertRunningJobAfterSeconds = v);
                dirty |= postTrigger.AssignStringIfNotNull(RemoteControlAccess, trigger, t => t.RemoteControlAccess, (t, v) => t.RemoteControlAccess = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, trigger, t => t.ConsecutiveJobFailuresThreshold, (t, v) => t.ConsecutiveJobFailuresThreshold = v);
                dirty |= postTrigger.AssignStringIfNotNull(InputArguments, trigger, t => t.InputArguments, (t, v) => t.InputArguments = v);

                if (MachineRobots is not null)
                {
                    postTrigger.MachineRobots = DeserializeMachineRobotSessions(this, drive, folder, target, MachineRobots);
                    dirty = true;
                }

                // Resolve Release name -> ReleaseKey if a non-empty value
                // was supplied; only mark dirty when the key actually
                // changes vs. the existing trigger.
                postTrigger.AssignIdFromName(
                    Release,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Key!,
                    (t, v) => { if (trigger.ReleaseKey != v) { t.ReleaseKey = v; dirty = true; } },
                    this, target, "Release");

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
}
