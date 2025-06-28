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
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public string? HiddenForAttendedUser { get; set; }

    // Allow live streaming
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RemoteControlAccessItems>))]
    public string? RemoteControlAccess { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? RetentionAction { get; set; }

    // 0 だったら 30 に補正する
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

    // 0 だったら 180 に補正する
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item180>))]
    public int? StaleRetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string? StaleRetentionBucket { get; set; }

    # region ProcessSettings
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
    public int? MaxDurationSeconds { get; set; } // 既定値がわからない。180 かな。
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

            // パラメータで選択済みの Id は、候補から除外する
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

    // プロセスの Tags 専用
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

            // パラメータで選択済みの Id は、候補から除外する
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
            IEnumerable<Release> processes = null; ;
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

            // GetReleaseById() は、繰り返し処理に入る前に実行する必要がある。
            // (繰り返し処理の中で呼び出すと、繰り返しが壊れてしまう)
            // どうせその必要があるのだから、スレッド起こして実行した方が良いな。
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

                // 既存のプロセスの内容から、post data を作成
                Release newRelease = OrchCollectionExtensions.DeepCopy(process)!;

                // 19.0 以降では、GetReleaseById() を呼び出すことで Retention を取得できるため下記は不要
                if (drive.OrchAPISession.ApiVersion < 19)
                {
                    try
                    {
                        // 既存の Retention を取得して設定しておく
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

                #region パラメータで指定された値を設定

                newRelease.AssignStringIfNotNullOrEmpty(NewName,               (r, v) => r.Name = v);
                newRelease.AssignStringIfNotNull(Description,                  (r, v) => r.Description = v);
                newRelease.AssignStringIfNotNullOrEmpty(InputArguments,        (r, v) => r.InputArguments = v);
                newRelease.AssignNumberIfNotNullOrZero(SpecificPriorityValue,  (r, v) => r.SpecificPriorityValue = v);
                newRelease.AssignBoolIfNotNull(HiddenForAttendedUser,          (r, v) => r.HiddenForAttendedUser = v);
                newRelease.AssignStringIfNotNullOrEmpty(RemoteControlAccess,   (r, v) => r.RemoteControlAccess = v);
                newRelease.AssignStringIfNotNullOrEmpty(RetentionAction,       (r, v) => r.RetentionAction = v);
                newRelease.AssignNumberIfNotNullOrZero(RetentionPeriod,        (r, v) => r.RetentionPeriod = v);
                newRelease.AssignStringIfNotNullOrEmpty(StaleRetentionAction,  (r, v) => r.StaleRetentionAction = v);
                newRelease.AssignNumberIfNotNullOrZero(StaleRetentionPeriod,   (r, v) => r.StaleRetentionPeriod = v);

                #region RetentionBucket を RetentionBucketId に変換する
                newRelease.AssignIdFromName(
                    RetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.RetentionBucketId = v,
                    this, target, "RetentionBucket");

                newRelease.AssignIdFromName(
                    StaleRetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.StaleRetentionBucketId = v,
                    this, target, "StaleRetentionBucket");
                #endregion

                newRelease.AssignTags(Tags, (r, v) => r.Tags = v);

                newRelease.ProcessSettings ??= new();
                newRelease.ProcessSettings.AssignBoolIfNotNull(ErrorRecordingEnabled, (r, v) => r.ErrorRecordingEnabled = v);
                newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Duration,      (r, v) => r.Duration = v);
                newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Frequency,     (r, v) => r.Frequency = v);
                newRelease.ProcessSettings.AssignNumberIfNotNullOrZero(Quality,       (r, v) => r.Quality = v);
                newRelease.ProcessSettings.AssignBoolIfNotNull(AutoStartProcess,      (r, v) => r.AutoStartProcess = v);
                newRelease.ProcessSettings.AssignBoolIfNotNull(AlwaysRunning,         (r, v) => r.AlwaysRunning = v);

                newRelease.ProcessSettings.AutopilotForRobots ??= new();
                newRelease.ProcessSettings.AutopilotForRobots.AssignBoolIfNotNull(A4R_Enabled,        (r, v) => r.Enabled = v);
                newRelease.ProcessSettings.AutopilotForRobots.AssignBoolIfNotNull(A4R_HealingEnabled, (r, v) => r.HealingEnabled = v);

                newRelease.VideoRecordingSettings ??= new();
                newRelease.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(VideoRecordingType,          (r, v) => r.VideoRecordingType = v);
                newRelease.VideoRecordingSettings.AssignStringIfNotNullOrEmpty(QueueItemVideoRecordingType, (r, v) => r.QueueItemVideoRecordingType = v);
                newRelease.VideoRecordingSettings.AssignNumberIfNotNullOrZero(MaxDurationSeconds,           (r, v) => r.MaxDurationSeconds = v);
                #endregion

                // ProcessVersion を変更した場合には、EntryPointId をつけかえないといけない
                string currentProcessVersion = newRelease.ProcessVersion;
                newRelease.AssignStringIfNotNullOrEmpty(Version, (r, v) => r.ProcessVersion = v);
                bool bVersionChanged = (currentProcessVersion != newRelease.ProcessVersion);

                #region EntryPoint を EntryPointId に変換する
                if (bVersionChanged || !string.IsNullOrEmpty(EntryPoint))
                {
                    try
                    {
                        var feedId = drive.FolderFeedId.Get(folder);
                        // Version を変更したけど、EntryPoint は指定していない場合には、現在の entryPointPath を確認しないと。
                        if (bVersionChanged && string.IsNullOrEmpty(EntryPoint))
                        {
                            // 現在の EntryPath を確認
                            var entryPoint = drive.GetPackageEntryPoints(feedId, newRelease?.ProcessKey ?? "", currentProcessVersion!)
                                .FirstOrDefault(e => e.Id == newRelease?.EntryPointId);
                            EntryPoint = entryPoint?.Path;
                        }
                        if (!newRelease.AssignIdFromName(
                                EntryPoint,
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

                // RetentionPeriod は mandatory なので念のため。
                if (string.IsNullOrEmpty(newRelease!.RetentionAction))
                {
                    newRelease.RetentionAction = (string.IsNullOrEmpty(RetentionAction)) ? "Delete" : RetentionAction;
                }
                if (newRelease.RetentionAction == "Delete")
                {
                    newRelease.RetentionBucketId = null;
                }
                newRelease.RetentionPeriod ??= 30;

                // GetStaleReleaseRetention() みたいのは呼び出していないから、上みたいな丁寧な処理は不要か。
                //newRelease.StaleRetentionAction = string.IsNullOrEmpty(StaleRetentionAction) ? "Delete" : StaleRetentionAction;
                //if (newRelease.StaleRetentionAction == "Delete")
                //{
                //    newRelease.StaleRetentionBucketId = null;
                //}
                //newRelease.StaleRetentionPeriod ??= 180;

                if (newRelease.SpecificPriorityValue is not null)
                {
                    newRelease.JobPriority = null; // これは SpecificPriorityValue で指定する
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
