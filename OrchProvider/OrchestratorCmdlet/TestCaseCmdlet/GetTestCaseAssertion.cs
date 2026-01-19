using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseAssertion", DefaultParameterSetName = "ByTestSetExecutionName")]
[OutputType(typeof(Entities.TestCaseAssertion))]
public class GetTestCaseAssertionCmdlet : OrchestratorPSCmdlet
{
    // パラメータセット1: TestSetExecutionName 直接指定
    [Parameter(Position = 0, ParameterSetName = "ByTestSetExecutionName")]
    [ArgumentCompleter(typeof(TestSetExecutionNameCompleter))]
    public string? TestSetExecutionName { get; set; }

    // パラメータセット2: Id 直接指定
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
    [ArgumentCompleter(typeof(TestCaseExecutionIdCompleter))]
    [Alias("TestCaseExecutionId")]
    public Int64[] Id { get; set; } = null!;

    // パラメータセット3: パイプ入力 (TestCaseExecution または TestSetExecution)
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipeline")]
    public object? InputObject { get; set; }

    // 共通パラメータ
    [Parameter]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public string? Destination { get; set; }

    private string? _resolvedDestination;
    private ConsoleCancelHandler? _cancelHandler;
    private HashSet<(Int64 folderId, Int64 testCaseExecutionId)> _processedIds = [];

    /// <summary>
    /// キャッシュまたは API から TestSetExecution を名前で検索し、Id を返す。
    /// </summary>
    private static Int64? ResolveTestSetExecutionId(OrchDriveInfo drive, Folder folder, string name)
    {
        if (drive._dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
        {
            var found = cached.Values.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found.Id;
            }
        }

        var filter = $"&$filter=(Name%20eq%20%27{Uri.EscapeDataString(name)}%27)";
        var results = drive.GetTestSetExecutions(folder, filter, 0, 1);
        var execution = results.FirstOrDefault();
        return execution?.Id;
    }

    /// <summary>
    /// TestSetExecutionId から TestCaseExecutionId[] を取得する。
    /// </summary>
    private static IEnumerable<Int64> GetTestCaseExecutionIds(OrchDriveInfo drive, Folder folder, Int64 testSetExecutionId)
    {
        // キャッシュから取得を試みる
        if (drive._dicTestCaseExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
        {
            var ids = cached
                .Where(e => e.TestSetExecutionId == testSetExecutionId)
                .Select(e => e.Id!.Value)
                .ToList();
            if (ids.Count > 0)
            {
                return ids;
            }
        }

        // キャッシュになければ API で取得
        var filter = $"&$filter=(TestSetExecutionId%20eq%20{testSetExecutionId})";
        var executions = drive.GetTestCaseExecutions(folder, filter, 0, ulong.MaxValue);
        return executions.Select(e => e.Id!.Value);
    }

    /// <summary>
    /// パス名に使用できない文字を _ に置換する
    /// </summary>
    private static string SanitizePathName(string name)
    {
        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private void ProcessAssertions(OrchDriveInfo drive, Folder folder, Int64 testCaseExecutionId)
    {
        // 重複チェック
        var key = (folder.Id!.Value, testCaseExecutionId);
        if (!_processedIds.Add(key))
        {
            return; // 既に処理済み
        }

        try
        {
            List<TestCaseAssertion>? assertions = null;
            Int64 folderId = folder.Id!.Value;

            // キャッシュから取得を試みる
            if (drive._dicTestCaseAssertions?.TryGetValue(folderId, out var folderCache) ?? false)
            {
                folderCache.TryGetValue(testCaseExecutionId, out assertions);
            }

            // キャッシュになければ API から取得
            if (assertions is null)
            {
                var execution = drive.OrchAPISession.GetTestCaseExecutionWithAssertions(folderId, testCaseExecutionId);
                assertions = execution?.TestCaseAssertions?.ToList() ?? [];

                // キャッシュに保存
                if (drive._dicTestCaseAssertions is null)
                {
                    lock (drive)
                    {
                        drive._dicTestCaseAssertions ??= [];
                    }
                }
                if (!drive._dicTestCaseAssertions.TryGetValue(folderId, out folderCache))
                {
                    folderCache = new();
                    drive._dicTestCaseAssertions[folderId] = folderCache;
                }
                folderCache[testCaseExecutionId] = assertions;
            }

            if (assertions.Count == 0) return;

            string folderPath = folder.GetPSPath();

            // TestSetExecutionName をキャッシュから取得
            string? testSetExecutionName = null;
            if (drive._dicTestCaseExecutions?.TryGetValue(folderId, out var tceCache) ?? false)
            {
                var tce = tceCache.FirstOrDefault(e => e.Id == testCaseExecutionId);
                testSetExecutionName = tce?.TestSetExecutionName;
            }

            // スクリーンショット保存先ディレクトリ
            string? screenshotDir = null;
            if (_resolvedDestination is not null)
            {
                // ディレクトリパスを構築: Destination/FolderName/TestSetExecutionName/
                var folderName = SanitizePathName(folder.DisplayName ?? folder.Id.ToString()!);
                if (!string.IsNullOrEmpty(testSetExecutionName))
                {
                    var sanitizedTestSetName = SanitizePathName(testSetExecutionName);
                    screenshotDir = System.IO.Path.Combine(_resolvedDestination, folderName, sanitizedTestSetName);
                }
                else
                {
                    screenshotDir = System.IO.Path.Combine(_resolvedDestination, folderName);
                }
            }

            foreach (var assertion in assertions)
            {
                _cancelHandler!.Token.ThrowIfCancellationRequested();

                assertion.Path = folderPath;
                assertion.TestSetExecutionName = testSetExecutionName;
                assertion.PathTestSetExecutionName = string.IsNullOrEmpty(testSetExecutionName)
                    ? folderPath
                    : System.IO.Path.Combine(folderPath, testSetExecutionName);
                assertion.TestCaseExecutionId = testCaseExecutionId;

                // スクリーンショットをダウンロード
                if (screenshotDir is not null && (assertion.HasScreenshot ?? false) && assertion.Id is not null)
                {
                    try
                    {
                        Directory.CreateDirectory(screenshotDir);
                        string fileName = $"{testCaseExecutionId}_{assertion.Id}.jpg";
                        string filePath = System.IO.Path.Combine(screenshotDir, fileName);
                        drive.OrchAPISession.DownloadAssertionScreenshot(folderId, assertion.Id.Value, filePath, _cancelHandler.Token);
                        assertion.ScreenshotPath = filePath;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteWarning($"Failed to download screenshot for assertion {assertion.Id}: {OrchException.ExtractMessage(ex.Message)}");
                    }
                }

                WriteObject(assertion);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            string target = folder.GetPSPath();
            WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseAssertionError", ErrorCategory.InvalidOperation, target));
        }
    }

    protected override void BeginProcessing()
    {
        // Destination が指定された場合、パスを解決
        if (Destination is not null)
        {
            _resolvedDestination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);
            if (!Directory.Exists(_resolvedDestination))
            {
                throw new DirectoryNotFoundException($"A directory '{_resolvedDestination}' does not exist.");
            }
        }

        _cancelHandler = new ConsoleCancelHandler();
    }

    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case "ByTestSetExecutionName":
                ProcessByTestSetExecutionName();
                break;
            case "ById":
                ProcessById();
                break;
            case "ByPipeline":
                ProcessByPipeline();
                break;
        }
    }

    private void ProcessByTestSetExecutionName()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path);

        // TestSetExecutionName が指定されていなければ、キャッシュの内容を返す
        if (TestSetExecutionName is null)
        {
            WriteWarning("Since TestSetExecutionName was not specified, the contents of the cache will be output. To query the Orchestrator, please specify TestSetExecutionName parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                var folderId = folder.Id!.Value;
                if (drive._dicTestCaseAssertions?.TryGetValue(folderId, out var folderCache) ?? false)
                {
                    string folderPath = folder.GetPSPath();

                    foreach (var kvp in folderCache)
                    {
                        var testCaseExecutionId = kvp.Key;
                        var assertions = kvp.Value;

                        // TestSetExecutionName を _dicTestCaseExecutions から取得
                        string? testSetExecutionName = null;
                        if (drive._dicTestCaseExecutions?.TryGetValue(folderId, out var tceCache) ?? false)
                        {
                            var tce = tceCache.FirstOrDefault(e => e.Id == testCaseExecutionId);
                            testSetExecutionName = tce?.TestSetExecutionName;
                        }

                        foreach (var assertion in assertions)
                        {
                            assertion.Path = folderPath;
                            assertion.TestSetExecutionName = testSetExecutionName;
                            assertion.PathTestSetExecutionName = string.IsNullOrEmpty(testSetExecutionName)
                                ? folderPath
                                : System.IO.Path.Combine(folderPath, testSetExecutionName);
                            assertion.TestCaseExecutionId = testCaseExecutionId;
                            WriteObject(assertion);
                        }
                    }
                }
            }
            return;
        }

        foreach (var (drive, folder) in drivesFolders)
        {
            _cancelHandler!.Token.ThrowIfCancellationRequested();

            try
            {
                var testSetExecutionId = ResolveTestSetExecutionId(drive, folder, TestSetExecutionName);
                if (testSetExecutionId is null)
                {
                    WriteWarning($"TestSetExecution '{TestSetExecutionName}' not found in folder '{folder.GetPSPath()}'.");
                    continue;
                }

                var testCaseExecutionIds = GetTestCaseExecutionIds(drive, folder, testSetExecutionId.Value);
                foreach (var testCaseExecutionId in testCaseExecutionIds)
                {
                    _cancelHandler.Token.ThrowIfCancellationRequested();
                    ProcessAssertions(drive, folder, testCaseExecutionId);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseAssertionError", ErrorCategory.InvalidOperation, target));
            }
        }
    }

    private void ProcessById()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path);

        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var testCaseExecutionId in Id)
            {
                _cancelHandler!.Token.ThrowIfCancellationRequested();
                ProcessAssertions(drive, folder, testCaseExecutionId);
            }
        }
    }

    private void ProcessByPipeline()
    {
        // PSObject からプロパティを取得するヘルパー
        static T? GetProperty<T>(PSObject pso, string name)
        {
            var prop = pso.Properties[name];
            if (prop?.Value is T value)
                return value;
            return default;
        }

        var pso = InputObject as PSObject ?? new PSObject(InputObject);
        var input = pso.BaseObject;

        if (input is TestCaseExecution tce)
        {
            if (tce.Id is null || string.IsNullOrEmpty(tce.Path))
            {
                WriteWarning("TestCaseExecution is missing Id or Path.");
                return;
            }

            var (drive, folder) = SessionState.ResolveToSingleFolder(tce.Path);
            _cancelHandler!.Token.ThrowIfCancellationRequested();
            ProcessAssertions(drive, folder, tce.Id.Value);
        }
        else if (input is TestSetExecution tse)
        {
            if (tse.Id is null || string.IsNullOrEmpty(tse.Path))
            {
                WriteWarning("TestSetExecution is missing Id or Path.");
                return;
            }

            var (drive, folder) = SessionState.ResolveToSingleFolder(tse.Path);
            try
            {
                _cancelHandler!.Token.ThrowIfCancellationRequested();

                var testCaseExecutionIds = GetTestCaseExecutionIds(drive, folder, tse.Id.Value);
                foreach (var testCaseExecutionId in testCaseExecutionIds)
                {
                    _cancelHandler.Token.ThrowIfCancellationRequested();
                    ProcessAssertions(drive, folder, testCaseExecutionId);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseAssertionError", ErrorCategory.InvalidOperation, target));
            }
        }
        else
        {
            // ダックタイピング: Id または Name プロパティがあれば処理
            var id = GetProperty<Int64?>(pso, "Id") ?? GetProperty<long?>(pso, "Id");
            var name = GetProperty<string>(pso, "Name");
            var path = GetProperty<string>(pso, "Path");

            // Path がなければ -Path パラメータ、それもなければカレントロケーションを使用
            if (string.IsNullOrEmpty(path))
            {
                path = Path?.FirstOrDefault() ?? SessionState.Path.CurrentLocation.Path;
            }

            var (drive, folder) = SessionState.ResolveToSingleFolder(path);

            if (id is not null)
            {
                // Id があれば TestCaseExecutionId として処理
                _cancelHandler!.Token.ThrowIfCancellationRequested();
                ProcessAssertions(drive, folder, id.Value);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                // Name があれば TestSetExecutionName として処理
                try
                {
                    var testSetExecutionId = ResolveTestSetExecutionId(drive, folder, name);
                    if (testSetExecutionId is null)
                    {
                        WriteWarning($"TestSetExecution '{name}' not found in folder '{folder.GetPSPath()}'.");
                        return;
                    }

                    var testCaseExecutionIds = GetTestCaseExecutionIds(drive, folder, testSetExecutionId.Value);
                    foreach (var testCaseExecutionId in testCaseExecutionIds)
                    {
                        _cancelHandler!.Token.ThrowIfCancellationRequested();
                        ProcessAssertions(drive, folder, testCaseExecutionId);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = folder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseAssertionError", ErrorCategory.InvalidOperation, target));
                }
            }
            else
            {
                WriteWarning($"InputObject must have Id or Name property, but got {input?.GetType().Name ?? "null"}.");
            }
        }
    }

    protected override void EndProcessing()
    {
        _cancelHandler?.Dispose();
    }
}
