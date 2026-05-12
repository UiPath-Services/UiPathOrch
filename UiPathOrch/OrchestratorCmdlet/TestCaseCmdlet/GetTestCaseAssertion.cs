using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseAssertion", DefaultParameterSetName = "ByTestSetExecutionName")]
[OutputType(typeof(Entities.TestCaseAssertion))]
public class GetTestCaseAssertionCmdlet : OrchestratorPSCmdlet
{
    // Parameter set 1: Specify TestSetExecutionName directly
    [Parameter(Position = 0, ParameterSetName = "ByTestSetExecutionName")]
    [ArgumentCompleter(typeof(TestSetExecutionNameCompleter))]
    public string? TestSetExecutionName { get; set; }

    // Parameter set 2: Specify Id directly
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
    [ArgumentCompleter(typeof(TestCaseExecutionIdCompleter))]
    [Alias("TestCaseExecutionId")]
    public Int64[] Id { get; set; } = null!;

    // Parameter set 3: Pipeline input (TestCaseExecution or TestSetExecution)
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipeline")]
    public object? InputObject { get; set; }

    // Common parameters
    [Parameter]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ScreenshotPath { get; set; }

    private string? _resolvedScreenshotPath;
    private ConsoleCancelHandler? _cancelHandler;
    private HashSet<(Int64 folderId, Int64 testCaseExecutionId)> _processedIds = [];


    /// <summary>
    /// Gets TestCaseExecutionId[] from a TestSetExecutionId.
    /// </summary>
    private static IEnumerable<Int64> GetTestCaseExecutionIds(OrchDriveInfo drive, Folder folder, Int64 testSetExecutionId)
    {
        // Try to get from cache
        var cached = drive.TestCaseExecutions.GetCache(folder)?.Values;
        if (cached is not null)
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

        // If not in cache, get from API
        var filter = $"&$filter=(TestSetExecutionId%20eq%20{testSetExecutionId})";
        var executions = drive.GetTestCaseExecutions(folder, filter, 0, ulong.MaxValue);
        return executions.Select(e => e.Id!.Value);
    }

    /// <summary>
    /// Replaces characters that are invalid in path names with _
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
        // Duplicate check
        var key = (folder.Id!.Value, testCaseExecutionId);
        if (!_processedIds.Add(key))
        {
            return; // Already processed
        }

        try
        {
            Int64 folderId = folder.Id!.Value;

            // Returns cached list, or fetches via GetTestCaseExecutionWithAssertions
            // when absent. Initializer in OrchDriveInfo sets Path + TestCaseExecutionId.
            var assertions = drive.TestCaseAssertions.Get(folder, testCaseExecutionId);

            if (assertions.Count == 0) return;

            string folderPath = folder.GetPSPath();

            // Get TestSetExecutionName from cache
            string? testSetExecutionName = null;
            var tceCache = drive.TestCaseExecutions.GetCache(folder)?.Values;
            if (tceCache is not null)
            {
                var tce = tceCache.FirstOrDefault(e => e.Id == testCaseExecutionId);
                testSetExecutionName = tce?.TestSetExecutionName;
            }

            // Screenshot save destination directory
            string? screenshotDir = null;
            if (_resolvedScreenshotPath is not null)
            {
                // Build directory path: ScreenshotPath/FolderName/TestSetExecutionName/
                var folderName = SanitizePathName(folder.DisplayName ?? folder.Id.ToString()!);
                if (!string.IsNullOrEmpty(testSetExecutionName))
                {
                    var sanitizedTestSetName = SanitizePathName(testSetExecutionName);
                    screenshotDir = System.IO.Path.Combine(_resolvedScreenshotPath, folderName, sanitizedTestSetName);
                }
                else
                {
                    screenshotDir = System.IO.Path.Combine(_resolvedScreenshotPath, folderName);
                }
            }

            if (screenshotDir is not null)
            {
                Directory.CreateDirectory(screenshotDir);
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

                // Download screenshot
                if (screenshotDir is not null && (assertion.HasScreenshot ?? false) && assertion.Id is not null)
                {
                    try
                    {
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
        // If ScreenshotPath is specified, resolve the path
        if (ScreenshotPath is not null)
        {
            _resolvedScreenshotPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(ScreenshotPath);
            if (!Directory.Exists(_resolvedScreenshotPath))
            {
                throw new DirectoryNotFoundException($"A directory '{_resolvedScreenshotPath}' does not exist.");
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

        // If TestSetExecutionName is not specified, return the cache contents
        if (TestSetExecutionName is null)
        {
            WriteWarning("Since TestSetExecutionName was not specified, the contents of the cache will be output. To query the Orchestrator, please specify TestSetExecutionName parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                var folderCache = drive.TestCaseAssertions.GetCache(folder);
                if (folderCache is not null)
                {
                    string folderPath = folder.GetPSPath();

                    foreach (var kvp in folderCache)
                    {
                        var testCaseExecutionId = kvp.Key;
                        var assertions = kvp.Value;

                        // Get TestSetExecutionName from _dicTestCaseExecutions
                        string? testSetExecutionName = null;
                        var tceCache = drive.TestCaseExecutions.GetCache(folder)?.Values;
                        if (tceCache is not null)
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
                var testSetExecutionId = drive.ResolveTestSetExecutionId(folder, TestSetExecutionName);
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
        // Helper to get a property from PSObject
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
            // Duck typing: process if there is an Id or Name property
            var id = GetProperty<Int64?>(pso, "Id") ?? GetProperty<long?>(pso, "Id");
            var name = GetProperty<string>(pso, "Name");
            var path = GetProperty<string>(pso, "Path");

            // If Path is missing, use the -Path parameter; if that is also missing, use the current location
            if (string.IsNullOrEmpty(path))
            {
                path = Path?.FirstOrDefault() ?? SessionState.Path.CurrentLocation.Path;
            }

            var (drive, folder) = SessionState.ResolveToSingleFolder(path);

            if (id is not null)
            {
                // If Id exists, process it as TestCaseExecutionId
                _cancelHandler!.Token.ThrowIfCancellationRequested();
                ProcessAssertions(drive, folder, id.Value);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                // If Name exists, process it as TestSetExecutionName
                try
                {
                    var testSetExecutionId = drive.ResolveTestSetExecutionId(folder, name);
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
