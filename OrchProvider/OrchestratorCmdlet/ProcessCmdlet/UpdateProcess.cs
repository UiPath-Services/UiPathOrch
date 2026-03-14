using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using System.Text.Json.Nodes;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchProcess", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Release))]
public class UpdateProcessCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageVersionCompleter))]
    public string? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EntryPointCompleter))]
    [SupportsWildcards]
    public string? EntryPoint { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(InputArgumentsCompleter))]
    public string? InputArguments { get; set; }

    // This parameter does not accept command-line input
    // Since we can just treat "" in CSV as 45, the type is int
    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public int? SpecificPriorityValue { get; set; }

    // This parameter does not accept CSV import
    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobPriorityItems>))]
    public string? Priority { get; set; }

    //  "RobotSize": null,

    // Hide process for attended users
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public string? HiddenForAttendedUser { get; set; }

    // Allow live streaming
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RemoteControlAccessItems>))]
    public string? RemoteControlAccess { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? RetentionAction { get; set; }

    // If 0, correct to 30
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item30>))]
    public int? RetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string? RetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? StaleRetentionAction { get; set; }

    // If 0, correct to 180
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item180>))]
    public int? StaleRetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string? StaleRetentionBucket { get; set; }

    #region ProcessSettings
    // Job recording (Screenshot)
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public string? ErrorRecordingEnabled { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item100>))]
    public int? Quality { get; set; } // 100

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item500>))]
    public int? Frequency { get; set; } // 500

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item40>))]
    public int? Duration { get; set; } // 40

    // Automatically Start Process
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public string? AutoStartProcess { get; set; }

    // Process can’t be stopped from UiPath Assistant
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public string? AlwaysRunning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? A4R_Enabled { get; set; } // Enable Healing Agent

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? A4R_HealingEnabled { get; set; } // Enable Healing Agent self-healing
    #endregion

    #region VideoRecordingSettings
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<VideoRecordingTypeItems>))]
    // "None"
    // "Failed": Record and store failed jobs
    // "All": Record all jobs
    public string? VideoRecordingType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    // "None"
    // "Failed": Record and store failed queue transactions
    [ArgumentCompleter(typeof(StaticTextsCompleter<QueueItemVideoRecordingTypeItems>))]
    public string? QueueItemVideoRecordingType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item180>))]
    public int? MaxDurationSeconds { get; set; } // Default value is unknown. Possibly 180.
    #endregion

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TagsCompleter))]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    internal class PackageVersionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetReleases(df.folder).FilterByWildcards(p => p?.Name, wpName));

            foreach (var result in results)
            {
                foreach (var process in result)
                {
                    if (string.IsNullOrEmpty(process.ProcessKey)) continue;

                    var (drive, folder) = result.Source;
                    var versions = drive.GetPackageVersions(folder, process.ProcessKey);
                    foreach (var version in versions
                        .Where(v => v.Version != process.ProcessVersion))
                    {
                        string tiphelp = process.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    private class EntryPointCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
            var paramVersion = GetParameterValue(commandAst, "Version", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var processes = drive.GetReleases(folder);
                var targetProcesses = processes.SelectByWildcards(p => p?.Name, wpName);
                var feedId = drive.FolderFeedId.Get(folder);
                foreach (var p in targetProcesses)
                {
                    var searchVersion = (!string.IsNullOrEmpty(paramVersion)) ? paramVersion : p.ProcessVersion;
                    var entryPoints = drive.GetPackageEntryPoints(feedId, p.ProcessKey!, searchVersion!);

                    string tiphelp = $"{p?.GetPSPath()}:{searchVersion}";
                    foreach (var e in entryPoints.Where(e => wp.IsMatch(e.Path)))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(e.Path), e.Path, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    private class InputArgumentsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude IDs already selected by other parameters from candidates
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", Positional.Name.Parameters);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetReleases(df.folder));

            foreach (var result in results)
            {
                foreach (var release in result
                    .FilterByWildcards(p => p?.Name, wpName)
                    .OrderBy(p => p.Name))
                {
                    var jsonArray = JsonNode.Parse(release.Arguments?.Input ?? "")?.AsArray();
                    var nameDictionary = jsonArray?
                        .OfType<JsonObject>()
                        .Select(obj =>
                        {
                            if (obj.TryGetPropertyValue("name", out var nameNode) && nameNode is JsonValue nameValue && nameValue.TryGetValue(out string? name))
                            {
                                return new { name, obj };
                            }
                            return null;
                        })
                        .Where(x => x is not null)
                        .ToDictionary(x => x!.name ?? "", x => "");

                    // Serialize the dictionary back to JSON
                    string outputJson = JsonSerializer.Serialize(nameDictionary, JsonTools.jsoOneLine);

                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult($"'{outputJson}'", outputJson, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    // Dedicated to process Tags
    private class TagsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude IDs already selected by other parameters from candidates
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetReleases(df.folder));

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
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        SpecificPriorityValue ??= ConvertPriorityToSpecificPriorityValue(Priority);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<Release> processes = null;
            try
            {
                processes = drive.GetReleases(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                continue;
            }
            if (processes is null) continue;

            var targetProcesses = processes.SelectByWildcards(p => p?.Name, wpName);

            // GetReleaseById() must be called before entering the iteration loop.
            // (Calling it inside the loop would break the iteration.)
            // Since it needs to be done anyway, it's better to run it on a separate thread.
            using var results = OrchThreadPool.RunForEach(targetProcesses,
                proc => proc.GetPSPath(),
                proc => proc,
                proc => drive.GetReleaseById(folder, proc.Id!.Value));

            foreach (var result in results)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                var process = result.GetResult(cancelHandler.Token);
                if (process is null) continue;

                string target = process.GetPSPath();

                // Create post data with only the properties needed for EditRelease
                Release newRelease = new()
                {
                    Id = process.Id,
                    Key = process.Key,
                    Name = process.Name,
                    Description = process.Description,
                    ProcessKey = process.ProcessKey,
                    ProcessVersion = process.ProcessVersion,
                    EntryPointId = process.EntryPointId,
                    EnvironmentVariables = process.EnvironmentVariables,
                    InputArguments = process.InputArguments,
                    SpecificPriorityValue = process.SpecificPriorityValue,
                    JobPriority = process.JobPriority,
                    RobotSize = process.RobotSize,
                    HiddenForAttendedUser = process.HiddenForAttendedUser,
                    //ResourceOverwrites = process.ResourceOverwrites,
                    RemoteControlAccess = process.RemoteControlAccess,
                    RetentionAction = process.RetentionAction,
                    RetentionPeriod = process.RetentionPeriod,
                    RetentionBucketId = process.RetentionBucketId,
                    StaleRetentionAction = process.StaleRetentionAction,
                    StaleRetentionPeriod = process.StaleRetentionPeriod,
                    StaleRetentionBucketId = process.StaleRetentionBucketId,
                    Tags = process.Tags,
                    ProcessSettings = OrchCollectionExtensions.DeepCopy(process.ProcessSettings),
                    VideoRecordingSettings = OrchCollectionExtensions.DeepCopy(process.VideoRecordingSettings),
                };

                // From version 19.0 onwards, Retention can be obtained by calling GetReleaseById(), so the following is unnecessary
                if (drive.OrchAPISession.ApiVersion < 19)
                {
                    try
                    {
                        // Retrieve and set the existing Retention
                        var retention = drive.OrchAPISession.GetReleaseRetention(folder.Id!.Value, process!.Id!.Value);
                        newRelease.RetentionAction = retention?.Action;
                        newRelease.RetentionPeriod = retention?.Period;
                        newRelease.RetentionBucketId = retention?.BucketId;
                    }
                    catch (Exception ex)
                    {
                        string msg2 = $"Get release retention failed.";
                        WriteError(new ErrorRecord(new OrchException(target, msg2, ex), "GetReleaseRetentionError", ErrorCategory.InvalidOperation, target));
                    }
                }

                #region Set values specified by parameters

                bool dirty = false;
                dirty |= newRelease.AssignStringIfNotNull(NewName,                      r => r.Name,                      (r, v) => r.Name = v);
                dirty |= newRelease.AssignStringIfNotNull(Description,                  r => r.Description,               (r, v) => r.Description = v);
                dirty |= newRelease.AssignStringIfNotNull(InputArguments,               r => r.InputArguments,            (r, v) => r.InputArguments = v);
                dirty |= newRelease.AssignNumberIfNotNullOrZero(SpecificPriorityValue,  r => r.SpecificPriorityValue,     (r, v) => r.SpecificPriorityValue = v);
                dirty |= newRelease.AssignBoolIfNotNull(HiddenForAttendedUser,          r => r.HiddenForAttendedUser,     (r, v) => r.HiddenForAttendedUser = v);
                dirty |= newRelease.AssignStringIfNotNull(RemoteControlAccess,          r => r.RemoteControlAccess,       (r, v) => r.RemoteControlAccess = v);
                dirty |= newRelease.AssignStringIfNotNull(RetentionAction,              r => r.RetentionAction,           (r, v) => r.RetentionAction = v);
                dirty |= newRelease.AssignNumberIfNotNullOrZero(RetentionPeriod,        r => r.RetentionPeriod,           (r, v) => r.RetentionPeriod = v);
                dirty |= newRelease.AssignStringIfNotNull(StaleRetentionAction,         r => r.StaleRetentionAction,      (r, v) => r.StaleRetentionAction = v);
                dirty |= newRelease.AssignNumberIfNotNullOrZero(StaleRetentionPeriod,   r => r.StaleRetentionPeriod,      (r, v) => r.StaleRetentionPeriod = v);

                #region Convert RetentionBucket to RetentionBucketId
                // TODO: AssignIdFromName has no change-detection overload, so dirty is captured via closure in the setter lambda.
                newRelease.AssignIdFromName(
                    RetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (s.RetentionBucketId != v) { s.RetentionBucketId = v; dirty = true; } },
                    this, target, "RetentionBucket");

                newRelease.AssignIdFromName(
                    StaleRetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (s.StaleRetentionBucketId != v) { s.StaleRetentionBucketId = v; dirty = true; } },
                    this, target, "StaleRetentionBucket");
                #endregion

                // TODO: Tags are not compared against existing values — dirty is set unconditionally when tags are specified.
                var effectiveTags = Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                if (effectiveTags is not null && effectiveTags.Length != 0)
                {
                    newRelease.AssignTags(effectiveTags, (r, v) => r.Tags = v);
                    dirty = true;
                }

                newRelease.ProcessSettings ??= new();
                dirty |= newRelease.ProcessSettings.AssignBoolIfNotNull(ErrorRecordingEnabled, r => r.ErrorRecordingEnabled, (r, v) => r.ErrorRecordingEnabled = v);
                dirty |= newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Duration,      r => r.Duration,              (r, v) => r.Duration = v);
                dirty |= newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Frequency,     r => r.Frequency,             (r, v) => r.Frequency = v);
                dirty |= newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Quality,       r => r.Quality,               (r, v) => r.Quality = v);
                dirty |= newRelease.ProcessSettings.AssignBoolIfNotNull(AutoStartProcess,      r => r.AutoStartProcess,      (r, v) => r.AutoStartProcess = v);
                dirty |= newRelease.ProcessSettings.AssignBoolIfNotNull(AlwaysRunning,         r => r.AlwaysRunning,         (r, v) => r.AlwaysRunning = v);

                newRelease.ProcessSettings.AutopilotForRobots ??= new();
                dirty |= newRelease.ProcessSettings.AutopilotForRobots.AssignBoolIfNotNull(A4R_Enabled,        r => r.Enabled,        (r, v) => r.Enabled = v);
                dirty |= newRelease.ProcessSettings.AutopilotForRobots.AssignBoolIfNotNull(A4R_HealingEnabled, r => r.HealingEnabled, (r, v) => r.HealingEnabled = v);

                newRelease.VideoRecordingSettings ??= new();
                dirty |= newRelease.VideoRecordingSettings.AssignStringIfNotNull(VideoRecordingType,          r => r.VideoRecordingType,          (r, v) => r.VideoRecordingType = v);
                dirty |= newRelease.VideoRecordingSettings.AssignStringIfNotNull(QueueItemVideoRecordingType, r => r.QueueItemVideoRecordingType, (r, v) => r.QueueItemVideoRecordingType = v);
                dirty |= newRelease.VideoRecordingSettings.AssignNumberIfNotNullOrZero(MaxDurationSeconds,    r => r.MaxDurationSeconds,          (r, v) => r.MaxDurationSeconds = v);
                #endregion

                // When ProcessVersion is changed, EntryPointId must be reassigned
                string currentProcessVersion = newRelease.ProcessVersion;
                dirty |= newRelease.AssignStringIfNotNull(Version, r => r.ProcessVersion, (r, v) => r.ProcessVersion = v);
                bool bVersionChanged = (currentProcessVersion != newRelease.ProcessVersion);

                #region Convert EntryPoint to EntryPointId
                if (bVersionChanged || !string.IsNullOrEmpty(EntryPoint))
                {
                    try
                    {
                        var feedId = drive.FolderFeedId.Get(folder);
                        // When Version was changed but EntryPoint was not specified, we need to check the current entryPointPath.
                        var resolvedEntryPoint = EntryPoint;
                        if (bVersionChanged && string.IsNullOrEmpty(resolvedEntryPoint))
                        {
                            // Check the current EntryPath
                            var entryPoint = drive.GetPackageEntryPoints(feedId, newRelease?.ProcessKey ?? "", currentProcessVersion!)
                                .FirstOrDefault(e => e.Id == newRelease?.EntryPointId);
                            resolvedEntryPoint = entryPoint?.Path;
                        }
                        if (!newRelease.AssignIdFromName(
                                resolvedEntryPoint,
                                () => drive.GetPackageEntryPoints(feedId, newRelease?.ProcessKey ?? "", newRelease?.ProcessVersion ?? ""),
                                e => e.Path!,
                                e => e.Id!,
                                (s, v) => s!.EntryPointId = v,
                                this, target, "EntryPoint"))
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "GetEntryPointError", ErrorCategory.InvalidOperation, folder));
                        continue;
                    }
                }
                #endregion

                // RetentionPeriod is mandatory, so set it just in case.
                if (string.IsNullOrEmpty(newRelease!.RetentionAction))
                {
                    newRelease.RetentionAction = (string.IsNullOrEmpty(RetentionAction)) ? "Delete" : RetentionAction;
                }
                if (newRelease.RetentionAction == "Delete")
                {
                    newRelease.RetentionBucketId = null;
                }
                newRelease.RetentionPeriod ??= 30;

                // Since we are not calling something like GetStaleReleaseRetention(), the careful handling above is probably unnecessary.
                //newRelease.StaleRetentionAction = string.IsNullOrEmpty(StaleRetentionAction) ? "Delete" : StaleRetentionAction;
                //if (newRelease.StaleRetentionAction == "Delete")
                //{
                //    newRelease.StaleRetentionBucketId = null;
                //}
                //newRelease.StaleRetentionPeriod ??= 180;

                if (newRelease.SpecificPriorityValue is not null)
                {
                    newRelease.JobPriority = null; // This is specified via SpecificPriorityValue
                }

                if (!dirty)
                {
                    WriteVerbose($"Skipping '{target}': no changes detected.");
                    continue;
                }

                if (ShouldProcess(target, "Update Process"))
                {
                    try
                    {
                        drive.OrchAPISession.EditRelease(folder.Id!.Value, newRelease);
                        drive._dicReleases?.TryRemove(folder.Id ?? 0, out var _);
                        //drive._dicReleaseList?.TryRemove(folder.Id ?? 0, out var _);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateProcessError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
