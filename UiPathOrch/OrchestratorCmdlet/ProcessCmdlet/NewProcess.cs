using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using System.Text.Json.Nodes;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchProcess", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Release))]
public class NewProcessCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageVersionCompleter))]
    [SupportsWildcards]
    public string? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

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

    // Hide process for attended users
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
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
    [ArgumentCompleter(typeof(BoolCompleter))]
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
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? AutoStartProcess { get; set; }

    // Process can’t be stopped from UiPath Assistant
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? AlwaysRunning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? A4R_Enabled { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? A4R_HealingEnabled { get; set; }
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
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

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

            // Exclude IDs already selected by other parameters from candidates
            var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();
            var wpVersion = GetFakeBoundParameters(fakeBoundParameters, "Version").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                var feedId = df.drive.FolderFeedId.Get(df.folder);
                return df.drive.GetPackages(df.folder).FilterByWildcards(p => p?.Id, wpId);
            });

            foreach (var result in results)
            {
                var (drive, folder) = result.Source;
                var feedId = drive.FolderFeedId.Get(folder);

                foreach (var package in result)
                {
                    var versions = drive.GetPackageVersions(folder, package.Id!);

                    foreach (var version in versions.FilterByWildcards(v => v?.Version, wpVersion))
                    {
                        var entryPoints = drive.GetPackageEntryPoints(feedId, version.Id!, version.Version!);
                        foreach (var entryPoint in entryPoints
                            .Where(e => wp.IsMatch(e.Path))
                            .OrderBy(e => e.Path))
                        {
                            yield return new CompletionResult(PathTools.EscapePSText(entryPoint.Path), entryPoint.Path, CompletionResultType.ParameterValue, version.GetPSPath());
                        }
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
            var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();
            var wpVersion = GetFakeBoundParameters(fakeBoundParameters, "Version").ConvertToWildcardPatternList();
            var wpEntryPoint = GetFakeBoundParameters(fakeBoundParameters, "EntryPoint").ConvertToWildcardPatternList();

            //var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                var packages = df.drive.GetPackages(df.folder)
                    .FilterByWildcards(p => p?.Id, wpId)
                    .FilterByWildcards(p => p?.Version, wpVersion);

                var feedId = df.drive.FolderFeedId.Get(df.folder);

                var r2 = ParallelResults.GroupBy(packages, package =>
                {
                    return df.drive.GetPackageEntryPoints(feedId, package.Id!, package.Version!);
                });

                List<(Package, IEnumerable<PackageEntryPoint>)> r = [];
                foreach (var r3 in r2)
                {
                    var package = r3.Source;
                    r.Add((package, r3));
                }
                return r;
            });

            foreach (var result in results)
            {
                foreach (var (package, entryPoints) in result)
                {
                    foreach (var entryPoint in entryPoints
                        .FilterByWildcards(e => e?.Path, wpEntryPoint)
                        .OrderBy(e => e.Path))
                    {
                        var jsonArray = JsonNode.Parse(entryPoint.InputArguments ?? "")?.AsArray();
                        var nameDictionary = jsonArray?
                            .OfType<JsonObject>()
                            .Where(obj => obj.TryGetPropertyValue("name", out var nameNode))
                            .ToDictionary(obj => obj["name"]?.ToString() ?? "", obj => "");

                        // Serialize the dictionary back to JSON
                        string outputJson = JsonSerializer.Serialize(nameDictionary);

                        string tiphelp = System.IO.Path.Combine(package.GetPSPath(), entryPoint.Path!);
                        yield return new CompletionResult($"'{outputJson}'", outputJson, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpPackageId = Id.ConvertToWildcardPatternList();

        if (Version == "") Version = null;
        var wpVersion = (Version is not null) ? new WildcardPattern(Version, WildcardOptions.IgnoreCase) : null;

        //HiddenForAttendedUser ??= false;
        SpecificPriorityValue ??= ConvertPriorityToSpecificPriorityValue(Priority);
        SpecificPriorityValue ??= 45; // If null, correct to 45

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            var packages = drive.GetPackages(folder);
            if (packages is null || packages.Count == 0) continue;

            var targetPackages = packages.SelectByWildcards(id => id?.Id, wpPackageId);
            if (!targetPackages.Any())
            {
                WriteWarning($"{folder.GetPSPath()}: No packages found with PackageId '{Id![0]}'.");
                continue;
            }

            //foreach (var id in packages.Select(p => p.Id).SelectByWildcards(id => id, wpPackageId))
            foreach (var targetPackage in targetPackages.WithCancellation(cancelHandler.Token))
            {
                string name = Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = targetPackage.Id;
                }

                string target = System.IO.Path.Combine(folder.GetPSPath(), name!);

                Package? version = null;
                if (Version is null)
                {
                    // Get the latest version of the package
                    version = targetPackage;
                }
                else
                {
                    var versions = drive.GetPackageVersions(folder, targetPackage.Id!)
                        .Where(v => wpVersion?.IsMatch(v.Version) ?? true) // If wpVersion is null, target all versions
                        .ToList();

                    if (versions.Count == 0)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"No versions matched with '{Version}'"), "NewProcessError", ErrorCategory.InvalidOperation, folder));
                        continue;
                    }

                    if (versions.Count > 1)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"Resolved to multiple versions with '{Version}'"), "NewProcessError", ErrorCategory.InvalidOperation, folder));
                        continue;
                    }
                    version = versions[0];
                }

                Release release = new()
                {
                    ProcessKey = targetPackage.Id,
                    ProcessVersion = version.Version,
                    Name = WildcardPattern.Unescape(name),
                };

                // Inherit Description from the package
                var desc = string.IsNullOrEmpty(Description) ? version?.Description : Description;
                release.AssignStringIfNotNull(desc, (r, v) => r.Description = v);

                release.AssignBoolIfNotNull(HiddenForAttendedUser, (r, v) => r.HiddenForAttendedUser = v);
                release.AssignStringIfNotNullOrEmpty(RemoteControlAccess, (r, v) => r.RemoteControlAccess = v);
                release.AssignStringIfNotNullOrEmpty(InputArguments, (r, v) => r.InputArguments = v);
                release.AssignNumberIfNotNullOrZero(SpecificPriorityValue, (r, v) => r.SpecificPriorityValue = v);
                release.AssignTags(Tags, (r, v) => r.Tags = v);

                #region Convert EntryPoint to EntryPointId
                var feedId = drive.FolderFeedId.Get(folder);
                // EntryPointId on Releases is a v15+ concept. On older OCs (ApiVersion < 12)
                // /odata/Processes/.../GetPackageEntryPoints returns 404, so skip the
                // EntryPoint → EntryPointId resolution. Releases POSTed without
                // EntryPointId pick up the package's main entry point server-side.
                if (drive.OrchAPISession.ApiVersion >= 12)
                {
                    if (!release.AssignIdFromName(
                        EntryPoint,
                        () => drive.GetPackageEntryPoints(feedId, targetPackage.Id!, version!.Version!),
                        e => e.Path!,
                        e => e.Id!,
                        (s, v) => s.EntryPointId = v,
                        this, target, "EntryPoint"))
                    {
                        continue;
                    }

                    // If EntryPoint is not specified, retrieve the default MainEntryPoint
                    if (release.EntryPointId is null)
                    {
                        try
                        {
                            var entryPoint = drive.OrchAPISession.GetPackageMainEntryPoint(feedId, targetPackage.Id!, version!.Version!);
                            release.EntryPointId = entryPoint?.Id;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "GetMainEntryPointError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
                #endregion

                release.ProcessSettings = new();
                release.ProcessSettings.AssignBoolIfNotNull(AlwaysRunning, (p, v) => p.AlwaysRunning = v);
                release.ProcessSettings.AssignBoolIfNotNull(AutoStartProcess, (p, v) => p.AutoStartProcess = v);
                release.ProcessSettings.AssignBoolIfNotNull(ErrorRecordingEnabled, (p, v) => p.ErrorRecordingEnabled = v);
                release.ProcessSettings.AssignNumberIfNotNullOrZero(Quality, (p, v) => p.Quality = v);
                release.ProcessSettings.AssignNumberIfNotNullOrZero(Frequency, (p, v) => p.Frequency = v);
                release.ProcessSettings.AssignNumberIfNotNullOrZero(Duration, (p, v) => p.Duration = v);

                release.ProcessSettings.AutopilotForRobots = new();
                release.ProcessSettings.AssignBoolIfNotNull(A4R_Enabled, (p, v) => p.AutopilotForRobots!.Enabled = v);
                release.ProcessSettings.AssignBoolIfNotNull(A4R_HealingEnabled, (p, v) => p.AutopilotForRobots!.HealingEnabled = v);

                // Verified on OC 22.10.1 (15.0)
                // Verified on OC 23.4.0 (16.0)
                // Verified on OC 23.10.6 (17.0)
                if (drive.OrchAPISession.ApiVersion >= 17)
                {
                    release.RetentionAction = (string.IsNullOrEmpty(RetentionAction)) ? "Delete" : RetentionAction;
                    release.RetentionPeriod = (RetentionPeriod is null || RetentionPeriod == 0) ? 30 : RetentionPeriod;
                    #region Convert RetentionBucket to RetentionBucketId
                    release.AssignIdFromName(
                        RetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.RetentionBucketId = v,
                        this, target, "RetentionBucket");
                    #endregion

                    release.StaleRetentionAction = (string.IsNullOrEmpty(StaleRetentionAction)) ? "Delete" : StaleRetentionAction;
                    release.StaleRetentionPeriod = (StaleRetentionPeriod is null || StaleRetentionPeriod == 0) ? 30 : StaleRetentionPeriod;
                    #region Convert StaleRetentionBucket to StaleRetentionBucketId
                    release.AssignIdFromName(
                        StaleRetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.StaleRetentionBucketId = v,
                        this, target, "StaleRetentionBucket");
                    #endregion

                    release.VideoRecordingSettings = new();
                    release.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(VideoRecordingType, (s, v) => s.VideoRecordingType = v);
                    release.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(QueueItemVideoRecordingType, (s, v) => s.QueueItemVideoRecordingType = v);
                    release.VideoRecordingSettings.AssignNumberIfNotNullOrZero(MaxDurationSeconds, (s, v) => s.MaxDurationSeconds = v);
                }

                if (drive.OrchAPISession.ApiVersion >= 19)
                {
                    if (string.IsNullOrEmpty(release.RetentionAction) || release.RetentionAction == "None")
                    {
                        release.RetentionAction = "Delete";
                    }
                }

                if (ShouldProcess(target + $":{version?.Version}", "New Process"))
                {
                    try
                    {
                        var newProcess = drive.OrchAPISession.PostRelease(folder.Id!.Value, release);
                        if (newProcess is not null)
                        {
                            newProcess.Path = folder.GetPSPath();
                            WriteObject(newProcess);
                            drive.Releases.ClearCache(folder);
                            //drive._dicReleaseList?.TryRemove(folder.Id ?? 0, out var _);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewProcessError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
