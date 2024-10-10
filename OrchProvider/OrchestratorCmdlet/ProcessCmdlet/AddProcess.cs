using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using System.Text.Json.Nodes;
using System.Text.Json;

using Positional = UiPath.PowerShell.Positional.Id_Version;
using RetentionActionItems = UiPath.PowerShell.Positional.Delete_Archive;
using JobPriorityItems = UiPath.PowerShell.Positional.JobPriorityItems;
using RemoteControlAccessItems = UiPath.PowerShell.Positional.RemoteControlAccessItems;
using VideoRecordingTypeItems = UiPath.PowerShell.Positional.VideoRecordingTypeItems;
using QueueItemVideoRecordingTypeItems = UiPath.PowerShell.Positional.QueueItemVideoRecordingTypeItems;
using UiPath.PowerShell.Completer;

using BoolCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.True_False>;
using RetentionActionCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.Delete_Archive>;

using Item30 = UiPath.PowerShell.Positional.Item30;
using Item40 = UiPath.PowerShell.Positional.Item40;
using Item100 = UiPath.PowerShell.Positional.Item100;
using Item180 = UiPath.PowerShell.Positional.Item180;
using Item500 = UiPath.PowerShell.Positional.Item500;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchProcess", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Release))]
    public class AddProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageIdCompleter<Positional.Id_Version>))]
        [SupportsWildcards]
        public string[]? PackageId { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PackageVersionCompleter<Positional.Id_Version>))]
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

        // このパラメータはコマンドラインでの指定を受け付けない
        // CSV で "" が指定されたら 45 にしてしまえば良いので、この型は int にする
        [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
        public int? SpecificPriorityValue { get; set; }

        // このパラメータは CSV インポートを受け付けない
        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<JobPriorityItems>))]
        public string? Priority { get; set; }

        //  "RobotSize": null,

        // Hide process for attended users
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? HiddenForAttendedUser { get; set; }

        // Allow live streaming
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<RemoteControlAccessItems>))]
        public string? RemoteControlAccess { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(RetentionActionCompleter))]
        public string? RetentionAction { get; set; }

        // 0 だったら 30 に補正する
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item30>))]
        public int? RetentionPeriod { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BucketNameCompleter<Positional.Id_Version>))]
        [SupportsWildcards]
        public string? RetentionBucket { get; set; }

        # region ProcessSettings
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
        public int? MaxDurationSeconds { get; set; } // 既定値がわからない。180 かな。
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

                // パラメータで選択済みの Id は、候補から除外する
                var wpId = CreateWPListFromOtherParameters(commandAst, "Id", Positional.Id_Version.Parameters);
                var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", Positional.Id_Version.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df =>
                {
                    var packages = df.drive.GetPackages(df.folder)
                        .FilterByWildcards(p => p?.Id, wpId)
                        .FilterByWildcards(p => p?.Version, wpVersion);

                    var feedId = df.drive.GetFolderFeedId(df.folder);

                    var r2 = ParallelResults.ForEach(packages, package =>
                    {
                        return df.drive.GetPackageEntryPoints(feedId, package.Id!, package.Version!);
                    });

                    List<IEnumerable<PackageEntryPoint>> r = [];
                    foreach (var r3 in r2)
                    {
                        if (!r3.TryGetValue(out var entities)) continue;
                        if (entities != null)
                            r.Add(entities);
                    }
                    return r;
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var (drive, folder) = result.Source;

                    foreach (var e in entities!
                        .SelectMany(e => e)
                        .Where(e => wp.IsMatch(e.Path))
                        .OrderBy(e => e.Path))
                    {
                        //string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.Path), e.Path, CompletionResultType.ParameterValue, e.Path);
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

                // パラメータで選択済みの Id は、候補から除外する
                var wpId = CreateWPListFromOtherParameters(commandAst, "Id", Positional.Id_Version.Parameters);
                var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", Positional.Id_Version.Parameters);
                var wpEntryPoint = CreateWPListFromOtherParameters(commandAst, "EntryPoint", Positional.Id_Version.Parameters);

                //var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df =>
                {
                    var packages = df.drive.GetPackages(df.folder)
                        .FilterByWildcards(p => p?.Id, wpId)
                        .FilterByWildcards(p => p?.Version, wpVersion);

                    var feedId = df.drive.GetFolderFeedId(df.folder);

                    var r2 = ParallelResults.ForEach(packages, package =>
                    {
                        return df.drive.GetPackageEntryPoints(feedId, package.Id!, package.Version!);
                    });

                    List<(Package, IEnumerable<PackageEntryPoint>)> r = [];
                    foreach (var r3 in r2)
                    {
                        if (!r3.TryGetValue(out var entities)) continue;
                        var package = r3.Source;
                        if (entities != null)
                            r.Add((package, entities));
                    }
                    return r;
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var (package, entryPoints) in entities!)
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
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpPackageId = PackageId?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            if (Version == "") Version = null;
            var wpVersion = (Version != null) ? new WildcardPattern(Version, WildcardOptions.IgnoreCase) : null;

            //HiddenForAttendedUser ??= false;
            SpecificPriorityValue ??= ConvertPriorityToSpecificPriorityValue(Priority);
            SpecificPriorityValue ??= 45; // null であれば 45 に補正

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                var packages = drive.GetPackages(folder);
                if (packages == null || packages.Count == 0) continue;

                var targetPackageIds = packages.Select(p => p.Id).SelectByWildcards(id => id, wpPackageId);
                if (!targetPackageIds.Any())
                {
                    WriteWarning($"{folder.GetPSPath()}: No packages found with PackageId '{PackageId![0]}'.");
                    continue;
                }

                //foreach (var id in packages.Select(p => p.Id).SelectByWildcards(id => id, wpPackageId))
                foreach (var id in targetPackageIds)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string name = Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = id;
                    }

                    string target = System.IO.Path.Combine(folder.GetPSPath(), name!);

                    // 当該パッケージの最新のバージョンを確認する
                    var versions = drive.GetPackageVersions(folder, id!)
                        .Where(v => wpVersion?.IsMatch(v.Version) ?? true); // wpVersion が null なら全てのバージョンを対象にする

                    if (!versions.Any())
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"No versions mathced with '{Version}'"), "AddProcessError", ErrorCategory.InvalidOperation, folder));
                        continue;
                    }

                    // GetPackageVersions は Version でソートした結果を返してくるので
                    // Last() が最新バージョンとなる
                    var latest = versions.Last();

                    Release release = new()
                    {
                        ProcessKey = id,
                        ProcessVersion = latest.Version,
                        Name = WildcardPattern.Unescape(name),
                    };

                    release.AssignStringIfNotNull(Description, (r, v) => r.Description = v);
                    // Description をパッケージから継承する
                    if (string.IsNullOrEmpty(Description))
                    {
                        var package = drive.GetPackages(folder)
                            .Where(p => p.Id == id)
                            .FirstOrDefault(p => p?.Version == Version);
                        if (package != null)
                        {
                            Description = package.Description;
                        }
                    }

                    release.AssignBoolIfNotNull(HiddenForAttendedUser,   (r, v) => r.HiddenForAttendedUser = v);
                    release.AssignStringIfNotNullOrEmpty(RemoteControlAccess,   (r, v) => r.RemoteControlAccess = v);
                    release.AssignStringIfNotNullOrEmpty(InputArguments,        (r, v) => r.InputArguments = v);
                    release.AssignNumberIfNotNullOrZero(SpecificPriorityValue, (r, v) => r.SpecificPriorityValue = v);
                    release.AssignTags(Tags, (r, v) => r.Tags = v);

                    #region EntryPoint を EntryPointid に変換
                    var feedId = drive.GetFolderFeedId(folder);
                    release.AssignIdFromName(
                        EntryPoint,
                        () => drive.GetPackageEntryPoints(feedId, id!, latest.Version!),
                        e => e.Path!,
                        e => e.Id!,
                        (s, v) => s.EntryPointId = v,
                        this, target, "EntryPoint");
                    #endregion

                    // EntryPoint が指定されていなければ、既定の MainEntryPoint を取り出す
                    if (release.EntryPointId == null)
                    {
                        try
                        {
                            var entryPoint = drive.OrchAPISession.GetPackageMainEntryPoint(feedId, id!, latest.Version!);
                            release.EntryPointId = entryPoint?.Id;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "GetMainEntryPointError", ErrorCategory.InvalidOperation, folder));
                        }
                    }

                    release.ProcessSettings = new();
                    release.ProcessSettings.AssignBoolIfNotNull(AlwaysRunning, (p, v) => p.AlwaysRunning = v);
                    release.ProcessSettings.AssignBoolIfNotNull(AutoStartProcess, (p, v) => p.AutoStartProcess = v);
                    release.ProcessSettings.AssignBoolIfNotNull(ErrorRecordingEnabled, (p, v) => p.ErrorRecordingEnabled = v);
                    release.ProcessSettings.AssignNumberIfNotNullOrZero(Quality, (p, v) => p.Quality = v);
                    release.ProcessSettings.AssignNumberIfNotNullOrZero(Frequency, (p, v) => p.Frequency = v);
                    release.ProcessSettings.AssignNumberIfNotNullOrZero(Duration, (p, v) => p.Duration = v);

                    release.VideoRecordingSettings = new();
                    release.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(VideoRecordingType, (s, v) => s.VideoRecordingType = v);
                    release.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(QueueItemVideoRecordingType, (s, v) => s.QueueItemVideoRecordingType = v);
                    release.VideoRecordingSettings.AssignNumberIfNotNullOrZero(MaxDurationSeconds, (s, v) => s.MaxDurationSeconds = v);

                    release.RetentionAction = (string.IsNullOrEmpty(RetentionAction)) ? "Delete" : RetentionAction;
                    release.RetentionPeriod = (RetentionPeriod == null || RetentionPeriod == 0) ? 30 : RetentionPeriod;

                    #region RetentionBucket を RetentionBucketId に変換
                    release.AssignIdFromName(
                        RetentionBucket,
                        () => drive.GetBuckets(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.RetentionBucketId = v,
                        this, target, "RetentionBucket");
                    #endregion

                    if (ShouldProcess(target + $":{latest.Version}", "Add Process"))
                    {
                        try
                        {
                            var newProcess = drive.OrchAPISession.CreateRelease2(folder.Id!.Value, release);
                            if (newProcess != null)
                            {
                                newProcess.Path = folder.GetPSPath();
                                WriteObject(newProcess);
                                drive._dicReleases?.TryRemove(folder.Id ?? 0, out var _);
                                drive._dicReleaseList?.TryRemove(folder.Id ?? 0, out var _);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddProcessError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
        }
    }
}
