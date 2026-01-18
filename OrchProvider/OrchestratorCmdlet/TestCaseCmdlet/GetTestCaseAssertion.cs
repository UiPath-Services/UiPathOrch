using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseAssertion", DefaultParameterSetName = "ById")]
[OutputType(typeof(Entities.TestCaseAssertion))]
public class GetTestCaseAssertionCmdlet : OrchestratorPSCmdlet
{
    // パラメータセット1: Id 直接指定
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
    [ArgumentCompleter(typeof(TestCaseExecutionIdCompleter))]
    [Alias("TestCaseExecutionId")]
    public Int64[] Id { get; set; } = null!;

    // パラメータセット2: TestSetExecutionName 直接指定
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByTestSetExecutionName")]
    [ArgumentCompleter(typeof(TestSetExecutionNameCompleter))]
    public string TestSetExecutionName { get; set; } = null!;

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
    private HashSet<(Int64 folderId, Int64 testCaseExecutionId)> _processedIds = new();

    /// <summary>
    /// キャッシュまたは API から TestSetExecution を名前で検索し、Id を返す。
    /// </summary>
    private Int64? ResolveTestSetExecutionId(OrchDriveInfo drive, Folder folder, string name)
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
    private IEnumerable<Int64> GetTestCaseExecutionIds(OrchDriveInfo drive, Folder folder, Int64 testSetExecutionId)
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
            var execution = drive.OrchAPISession.GetTestCaseExecutionWithAssertions(folder.Id!.Value, testCaseExecutionId);
            if (execution?.TestCaseAssertions is null) return;

            // スクリーンショット保存先ディレクトリ
            string? screenshotDir = null;
            if (_resolvedDestination is not null)
            {
                screenshotDir = System.IO.Path.Combine(_resolvedDestination, folder.DisplayName ?? folder.Id.ToString()!);
            }

            foreach (var assertion in execution.TestCaseAssertions)
            {
                _cancelHandler!.Token.ThrowIfCancellationRequested();

                assertion.Path = folder.GetPSPath();
                assertion.TestCaseExecutionId = testCaseExecutionId;

                // スクリーンショットをダウンロード
                if (screenshotDir is not null && (assertion.HasScreenshot ?? false) && assertion.Id is not null)
                {
                    try
                    {
                        Directory.CreateDirectory(screenshotDir);
                        string fileName = $"{testCaseExecutionId}_{assertion.Id}.jpg";
                        string filePath = System.IO.Path.Combine(screenshotDir, fileName);
                        drive.OrchAPISession.DownloadAssertionScreenshot(folder.Id!.Value, assertion.Id.Value, filePath, _cancelHandler.Token);
                        assertion.ScreenshotPath = filePath;
                    }
                    catch (Exception ex)
                    {
                        WriteWarning($"Failed to download screenshot for assertion {assertion.Id}: {OrchException.ExtractMessage(ex.Message)}");
                    }
                }

                WriteObject(assertion);
            }
        }
        catch (OrchException ex)
        {
            WriteError(new ErrorRecord(ex, "GetTestCaseAssertionError", ErrorCategory.InvalidOperation, ex.Target));
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
            case "ById":
                ProcessById();
                break;
            case "ByTestSetExecutionName":
                ProcessByTestSetExecutionName();
                break;
            case "ByPipeline":
                ProcessByPipeline();
                break;
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

    private void ProcessByTestSetExecutionName()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path);

        foreach (var (drive, folder) in drivesFolders)
        {
            _cancelHandler!.Token.ThrowIfCancellationRequested();

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
    }

    private void ProcessByPipeline()
    {
        // PSObject をアンラップ
        var input = InputObject;
        if (input is PSObject pso)
        {
            input = pso.BaseObject;
        }

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
            _cancelHandler!.Token.ThrowIfCancellationRequested();

            var testCaseExecutionIds = GetTestCaseExecutionIds(drive, folder, tse.Id.Value);
            foreach (var testCaseExecutionId in testCaseExecutionIds)
            {
                _cancelHandler.Token.ThrowIfCancellationRequested();
                ProcessAssertions(drive, folder, testCaseExecutionId);
            }
        }
        else
        {
            WriteWarning($"InputObject must be TestCaseExecution or TestSetExecution, but got {input?.GetType().Name ?? "null"}.");
        }
    }

    protected override void EndProcessing()
    {
        _cancelHandler?.Dispose();
    }
}
