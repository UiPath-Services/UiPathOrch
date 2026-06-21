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

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchProcess", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Release))]
public class UpdateProcessCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
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
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
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
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
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
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    // Literal (non-wildcard) path. [Alias("PSPath")] lets `dir ... | Update-OrchProcess`
    // and Import-Csv of a dir export bind each item's own path via its PSPath note-property.
    // Same parameter set as -Path: folder objects expose only PSPath (no Path), content
    // entities expose only Path, so the two never collide on a pipeline record.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    internal class PackageVersionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Releases.Get(df.folder).FilterByWildcards(p => p?.Name, wpName));

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var paramVersion = GetFakeBoundParameter(fakeBoundParameters, "Version");

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var processes = drive.Releases.Get(folder);
                var targetProcesses = processes.SelectByWildcards(p => p?.Name, wpName);
                var feedId = drive.FolderFeedId.Get(folder);
                foreach (var p in targetProcesses)
                {
                    var searchVersion = (!string.IsNullOrEmpty(paramVersion)) ? paramVersion : p.ProcessVersion;
                    var entryPoints = drive.PackageEntryPoints.Get((feedId ?? "", p.ProcessKey!, searchVersion!));

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude IDs already selected by other parameters from candidates
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Releases.Get(df.folder));

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude IDs already selected by other parameters from candidates
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Releases.Get(df.folder));

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
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        SpecificPriorityValue ??= ConvertPriorityToSpecificPriorityValue(Priority);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<Release> processes = null;
            try
            {
                processes = drive.Releases.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                continue;
            }
            if (processes is null) continue;

            var targetProcesses = processes.SelectByWildcards(p => p?.Name, wpName).OrderBy(p => p.Name);

            // ReleasesDetailed.Get() must be called before entering the iteration loop.
            // (Calling it inside the loop would break the iteration.)
            // Since it needs to be done anyway, it's better to run it on a separate thread.
            using var results = OrchThreadPool.RunForEach(targetProcesses,
                proc => proc.GetPSPath(),
                proc => proc,
                proc => drive.ReleasesDetailed.Get(folder, proc.Id!.Value));

            using var reporter = new ProgressReporter(this, 1, results.Count, $"Updating processes in {folder.GetPSPath()}");
            foreach (var result in results.WithCancellation(cancelHandler.Token))
            {
                var process = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (process is null) continue;

                string target = process.GetPSPath();

                // Build PATCH payload with only user-specified properties
                Release newRelease = new() { Id = process.Id };

                #region Set values specified by parameters

                bool releaseDirty = false;
                releaseDirty |= newRelease.AssignStringIfNotNull(NewName, process, r => r.Name, (r, v) => r.Name = v);
                releaseDirty |= newRelease.AssignStringIfNotNull(Description, process, r => r.Description, (r, v) => r.Description = v);
                releaseDirty |= newRelease.AssignStringIfNotNull(InputArguments, process, r => r.InputArguments, (r, v) => r.InputArguments = v);
                releaseDirty |= newRelease.AssignNumberIfNotNullOrZero(SpecificPriorityValue, process, r => r.SpecificPriorityValue, (r, v) => r.SpecificPriorityValue = v);
                releaseDirty |= newRelease.AssignBoolIfNotNull(HiddenForAttendedUser, process, r => r.HiddenForAttendedUser, (r, v) => r.HiddenForAttendedUser = v);
                releaseDirty |= newRelease.AssignStringIfNotNull(RemoteControlAccess, process, r => r.RemoteControlAccess, (r, v) => r.RemoteControlAccess = v);
                #region Retention (uses separate PutReleaseRetention API)
                ReleaseRetentionSetting? retentionUpdate = null;
                {
                    var ret = new ReleaseRetentionSetting { ReleaseId = process.Id };
                    bool retDirty = false;
                    if (RetentionAction is not null && RetentionAction != (process.RetentionAction ?? "")) { ret.Action = RetentionAction; retDirty = true; }
                    if (RetentionPeriod is not null && RetentionPeriod != 0 && RetentionPeriod != process.RetentionPeriod) { ret.Period = RetentionPeriod; retDirty = true; }
                    ret.AssignIdFromName(
                        RetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => { if (process.RetentionBucketId != v) { s.BucketId = v; retDirty = true; } },
                        this, target, "RetentionBucket");
                    if (retDirty)
                    {
                        // PUT requires all fields; fill unspecified fields from current values
                        ret.Action ??= process.RetentionAction ?? "Delete";
                        ret.Period ??= process.RetentionPeriod ?? 30;
                        ret.BucketId ??= process.RetentionBucketId;
                        retentionUpdate = ret;
                    }
                }

                ReleaseRetentionSetting? staleRetentionUpdate = null;
                {
                    var ret = new ReleaseRetentionSetting { ReleaseId = process.Id, Type = "Stale" };
                    bool retDirty = false;
                    if (StaleRetentionAction is not null && StaleRetentionAction != (process.StaleRetentionAction ?? "")) { ret.Action = StaleRetentionAction; retDirty = true; }
                    if (StaleRetentionPeriod is not null && StaleRetentionPeriod != 0 && StaleRetentionPeriod != process.StaleRetentionPeriod) { ret.Period = StaleRetentionPeriod; retDirty = true; }
                    ret.AssignIdFromName(
                        StaleRetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => { if (process.StaleRetentionBucketId != v) { s.BucketId = v; retDirty = true; } },
                        this, target, "StaleRetentionBucket");
                    if (retDirty)
                    {
                        ret.Action ??= process.StaleRetentionAction ?? "Delete";
                        ret.Period ??= process.StaleRetentionPeriod ?? 30;
                        ret.BucketId ??= process.StaleRetentionBucketId;
                        staleRetentionUpdate = ret;
                    }
                }
                #endregion

                var effectiveTags = Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                if (effectiveTags is not null && effectiveTags.Length != 0)
                {
                    newRelease.AssignTags(effectiveTags, (r, v) => r.Tags = v);
                    releaseDirty = true;
                }

                #region ProcessSettings (only include if any sub-property is specified)
                {
                    var ps = new ProcessSettings();
                    var psSource = process.ProcessSettings ?? new();
                    bool psDirty = false;
                    psDirty |= ps.AssignBoolIfNotNull(ErrorRecordingEnabled, psSource, r => r.ErrorRecordingEnabled, (r, v) => r.ErrorRecordingEnabled = v);
                    psDirty |= ps.AssignNumberIfNotNullOrZero(Duration, psSource, r => r.Duration, (r, v) => r.Duration = v);
                    psDirty |= ps.AssignNumberIfNotNullOrZero(Frequency, psSource, r => r.Frequency, (r, v) => r.Frequency = v);
                    psDirty |= ps.AssignNumberIfNotNullOrZero(Quality, psSource, r => r.Quality, (r, v) => r.Quality = v);
                    psDirty |= ps.AssignBoolIfNotNull(AutoStartProcess, psSource, r => r.AutoStartProcess, (r, v) => r.AutoStartProcess = v);
                    psDirty |= ps.AssignBoolIfNotNull(AlwaysRunning, psSource, r => r.AlwaysRunning, (r, v) => r.AlwaysRunning = v);

                    var a4r = new AutopilotForRobotsSettings();
                    var a4rSource = psSource.AutopilotForRobots ?? new();
                    bool a4rDirty = false;
                    a4rDirty |= a4r.AssignBoolIfNotNull(A4R_Enabled, a4rSource, r => r.Enabled, (r, v) => r.Enabled = v);
                    a4rDirty |= a4r.AssignBoolIfNotNull(A4R_HealingEnabled, a4rSource, r => r.HealingEnabled, (r, v) => r.HealingEnabled = v);
                    if (a4rDirty) { ps.AutopilotForRobots = a4r; psDirty = true; }

                    if (psDirty) { newRelease.ProcessSettings = ps; releaseDirty = true; }
                }
                #endregion

                #region VideoRecordingSettings (only include if any sub-property is specified)
                {
                    var vrs = new VideoRecordingSettings();
                    var vrsSource = process.VideoRecordingSettings ?? new();
                    bool vrsDirty = false;
                    vrsDirty |= vrs.AssignStringIfNotNull(VideoRecordingType, vrsSource, r => r.VideoRecordingType, (r, v) => r.VideoRecordingType = v);
                    vrsDirty |= vrs.AssignStringIfNotNull(QueueItemVideoRecordingType, vrsSource, r => r.QueueItemVideoRecordingType, (r, v) => r.QueueItemVideoRecordingType = v);
                    vrsDirty |= vrs.AssignNumberIfNotNullOrZero(MaxDurationSeconds, vrsSource, r => r.MaxDurationSeconds, (r, v) => r.MaxDurationSeconds = v);

                    if (vrsDirty) { newRelease.VideoRecordingSettings = vrs; releaseDirty = true; }
                }
                #endregion

                #endregion

                // When ProcessVersion is changed, EntryPointId must be reassigned
                releaseDirty |= newRelease.AssignStringIfNotNull(Version, process, r => r.ProcessVersion, (r, v) => r.ProcessVersion = v);
                bool bVersionChanged = newRelease.ProcessVersion is not null;

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
                            var entryPoint = drive.PackageEntryPoints.Get((feedId ?? "", process.ProcessKey ?? "", process.ProcessVersion!))
                                .FirstOrDefault(e => e.Id == process.EntryPointId);
                            resolvedEntryPoint = entryPoint?.Path;
                        }
                        if (!newRelease.AssignIdFromName(
                                resolvedEntryPoint,
                                () => drive.PackageEntryPoints.Get((feedId ?? "", process.ProcessKey ?? "", newRelease.ProcessVersion ?? process.ProcessVersion ?? "")),
                                e => e.Path!,
                                e => e.Id!,
                                (s, v) => { if (process.EntryPointId != v) { s!.EntryPointId = v; releaseDirty = true; } },
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

                if (!releaseDirty && retentionUpdate is null && staleRetentionUpdate is null)
                {
                    continue;
                }

                if (ShouldProcess(target, "Update Process"))
                {
                    try
                    {
                        if (releaseDirty)
                        {
                            drive.OrchAPISession.PatchRelease(folder.Id!.Value, newRelease);
                        }

                        if (retentionUpdate is not null)
                        {
                            drive.OrchAPISession.PutReleaseRetention(folder.Id!.Value, process.Id!.Value, retentionUpdate);
                        }
                        if (staleRetentionUpdate is not null)
                        {
                            drive.OrchAPISession.PutReleaseRetention(folder.Id!.Value, process.Id!.Value, staleRetentionUpdate);
                        }

                        drive.Releases.ClearCache(folder);
                        drive.ReleasesDetailed.ClearCache(folder);
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
