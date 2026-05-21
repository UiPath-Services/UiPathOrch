using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// New-OrchApiTrigger -- wraps POST to the HttpTrigger create endpoint.
//
// Surface: all writable HttpTrigger + TriggerBase properties. The
// wrapped server endpoint URL is currently /odata/HttpTriggers based on
// OData convention; the user has flagged that the actual create/update
// endpoints are non-public and a re-captured dev-tools trace will pin
// down the canonical URL. See OrchAPISession.CreateHttpTrigger.
[Cmdlet(VerbsCommon.New, "OrchApiTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(HttpTrigger))]
public class NewApiTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string? Release { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    // --- HTTP-trigger-specific fields ---

    // HTTP verb the trigger answers (server default is POST).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<HttpMethodItems>))]
    public string? Method { get; set; }

    // URL slug suffix; server auto-generates when null/empty.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Slug { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<HttpCallingModeItems>))]
    public string? CallingMode { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? SuccessCallbackUrl { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? FailureCallbackUrl { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Secret { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? CallbackMode { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? AllowInsecureSsl { get; set; }

    // Run the job as the API caller (vs. as the trigger owner). Observed
    // on POST/PUT payloads as 2026-05-21.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? RunAsCaller { get; set; }

    // --- TriggerBase fields ---

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? JobPriority { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? RunAsMe { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RuntimeTypes>))]
    public string? RuntimeType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? TargetFramework { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ResumeOnSameContext { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? RequiresUserInteraction { get; set; }

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
    public int? JobFailuresGracePeriodInHours { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? InputArguments { get; set; }

    // JSON-serialized MachineRobotSession[] (re-using the same shape as
    // New-OrchTrigger's MachineRobots).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? MachineRobots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);

        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();
        Name = Name.Split1stValueByUnescapedCommas()?.ToArray();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                var newTrigger = new HttpTrigger
                {
                    Name = WildcardPattern.Unescape(name),
                };

                // HttpTrigger-specific
                newTrigger.AssignStringIfNotNullOrEmpty(Method, (t, v) => t.Method = v);
                newTrigger.AssignStringIfNotNullOrEmpty(Slug, (t, v) => t.Slug = v);
                newTrigger.AssignStringIfNotNullOrEmpty(CallingMode, (t, v) => t.CallingMode = v);
                newTrigger.AssignStringIfNotNullOrEmpty(SuccessCallbackUrl, (t, v) => t.SuccessCallbackUrl = v);
                newTrigger.AssignStringIfNotNullOrEmpty(FailureCallbackUrl, (t, v) => t.FailureCallbackUrl = v);
                newTrigger.AssignStringIfNotNullOrEmpty(Secret, (t, v) => t.Secret = v);
                newTrigger.AssignStringIfNotNullOrEmpty(CallbackMode, (t, v) => t.CallbackMode = v);
                newTrigger.AssignBoolIfNotNull(AllowInsecureSsl, (t, v) => t.AllowInsecureSsl = v);
                newTrigger.AssignBoolIfNotNull(RunAsCaller, (t, v) => t.RunAsCaller = v);

                // TriggerBase
                newTrigger.AssignStringIfNotNullOrEmpty(Description, (t, v) => t.Description = v);
                newTrigger.AssignBoolIfNotNull(Enabled, (t, v) => t.Enabled = v);
                newTrigger.AssignNumberIfNotNullOrZero(JobPriority, (t, v) => t.JobPriority = v);
                newTrigger.AssignBoolIfNotNull(RunAsMe, (t, v) => t.RunAsMe = v);
                newTrigger.AssignStringIfNotNullOrEmpty(RuntimeType, (t, v) => t.RuntimeType = v);
                newTrigger.AssignStringIfNotNullOrEmpty(TargetFramework, (t, v) => t.TargetFramework = v);
                newTrigger.AssignBoolIfNotNull(ResumeOnSameContext, (t, v) => t.ResumeOnSameContext = v);
                newTrigger.AssignBoolIfNotNull(RequiresUserInteraction, (t, v) => t.RequiresUserInteraction = v);
                newTrigger.AssignStringIfNotNullOrEmpty(StopStrategy, (t, v) => t.StopStrategy = v);
                newTrigger.AssignNumberIfNotNullOrZero(StopJobAfterSeconds, (t, v) => t.StopJobAfterSeconds = v);
                newTrigger.AssignNumberIfNotNullOrZero(KillJobAfterSeconds, (t, v) => t.KillJobAfterSeconds = v);
                newTrigger.AssignNumberIfNotNullOrZero(AlertPendingJobAfterSeconds, (t, v) => t.AlertPendingJobAfterSeconds = v);
                newTrigger.AssignNumberIfNotNullOrZero(AlertRunningJobAfterSeconds, (t, v) => t.AlertRunningJobAfterSeconds = v);
                newTrigger.AssignStringIfNotNullOrEmpty(RemoteControlAccess, (t, v) => t.RemoteControlAccess = v);
                newTrigger.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, (t, v) => t.ConsecutiveJobFailuresThreshold = v);
                newTrigger.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours, (t, v) => t.JobFailuresGracePeriodInHours = v);
                newTrigger.AssignStringIfNotNullOrEmpty(InputArguments, (t, v) => t.InputArguments = v);

                newTrigger.MachineRobots = DeserializeMachineRobotSessions(this, drive, folder, target, MachineRobots);
                // The server requires Tags, MachineRobots, and Slug to be
                // present on the body, even when empty. Observed against
                // POST /odata/HttpTriggers on yotsuda 2026-05-21: omitting
                // any of the three returns a generic 500 "An error has
                // occurred." (errorCode 0) with no useful message; the
                // working UI payload always includes Tags=[],
                // MachineRobots=[{}], and a non-empty Slug. Default the
                // Slug to the trigger Name so the cmdlet stays usable
                // without forcing every caller to invent one.
                newTrigger.Tags ??= System.Array.Empty<Tag>();
                if (newTrigger.MachineRobots is null || newTrigger.MachineRobots.Length == 0)
                {
                    newTrigger.MachineRobots = new[] { new MachineRobotSession() };
                }
                if (string.IsNullOrEmpty(newTrigger.Slug))
                {
                    newTrigger.Slug = newTrigger.Name;
                }

                // Resolve Release name -> ReleaseKey (HttpTrigger uses
                // ReleaseKey, not ReleaseId).
                newTrigger.AssignIdFromName(
                    Release,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Key!,
                    (t, v) => t.ReleaseKey = v,
                    this, target, "Release");

                if (string.IsNullOrEmpty(newTrigger.ReleaseKey))
                {
                    WriteError(new ErrorRecord(
                        new OrchException(target, $"Release '{Release}' not found in folder '{folder.GetPSPath()}'."),
                        "NewApiTriggerReleaseNotFound", ErrorCategory.InvalidOperation, folder));
                    continue;
                }

                if (ShouldProcess(target, "New ApiTrigger"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateHttpTrigger(folder.Id!.Value, newTrigger);
                        if (created is not null)
                        {
                            drive.ApiTriggers.ClearCache(folder);
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewApiTriggerError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
