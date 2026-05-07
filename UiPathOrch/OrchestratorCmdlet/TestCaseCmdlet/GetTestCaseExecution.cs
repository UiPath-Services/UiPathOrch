using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestCaseExecution", DefaultParameterSetName = "ByName")]
[OutputType(typeof(Entities.TestCaseExecution))]
public class GetTestCaseExecutionCmdlet : OrchestratorPSCmdlet
{
    // Parameter set 1: Specify by name
    [Parameter(Position = 0, ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(TestSetExecutionNameCompleter))]
    public string? TestSetExecutionName { get; set; }

    // Parameter set 2: Specify by Id (for pipeline input)
    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ById")]
    [Alias("Id")]
    public Int64 TestSetExecutionId { get; set; }

    // Common parameters
    [Parameter]
    [ArgumentCompleter(typeof(TestCaseExecutionEntryPointCompleter))]
    [SupportsWildcards]
    [Alias("EntryPointPath")]
    public string[]? Name { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    public string? Last { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartTimeAfter { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartTimeBefore { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    public ulong? Skip { get; set; }

    [Parameter(ParameterSetName = "ByName")]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }
    private HashSet<(Int64 folderId, Int64 testSetExecutionId)> _processedIds = [];



    private string? MakeFilter(Int64? testSetExecutionId)
    {
        var filter = new List<string>();

        if (testSetExecutionId is not null)
        {
            filter.Add($"(TestSetExecutionId eq {testSetExecutionId.Value})");
        }

        if (Last is not null)
        {
            var last = Last.ToLower() switch
            {
                "hour" => DateTime.UtcNow.AddHours(-1),
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                "3months" => DateTime.UtcNow.AddMonths(-3),
                "6months" => DateTime.UtcNow.AddMonths(-6),
                "year" => DateTime.UtcNow.AddYears(-1),
                "3years" => DateTime.UtcNow.AddYears(-3),
                _ => throw new ArgumentException("Invalid Last parameter. Valid values are 'Hour', 'Day', 'Week', 'Month', '3Months', '6Months', 'Year', '3Years'.")
            };
            filter.Add($"(StartTime ge {last:yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        if (StartTimeAfter is not null)
        {
            filter.Add($"(StartTime ge {StartTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        if (StartTimeBefore is not null)
        {
            filter.Add($"(StartTime lt {StartTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        string? ret = filter.CreateAndFilter(f => f);
        if (ret is null) return null;
        return $"&$filter={ret}";
    }

    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case "ByName":
                ProcessByName();
                break;
            case "ById":
                ProcessById();
                break;
        }
    }

    private void ProcessByName()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // If no parameters are specified, return the cache contents
        bool bOutCache = (
            TestSetExecutionName is null &&
            Last is null &&
            StartTimeAfter is null &&
            StartTimeBefore is null &&
            Skip is null &&
            First is null);

        if (bOutCache)
        {
            WriteWarning("Since TestSetExecutionName/Last/StartTimeAfter/StartTimeBefore/Skip/First were not specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one of these parameters.");

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicTestCaseExecutions?.TryGetValue(folder.Id!.Value, out var cached) ?? false)
                {
                    WriteObject(cached
                        .FilterByWildcards(e => e?.EntryPointPath, wpName)
                        .OrderBy(e => e.TestSetExecutionId)
                        .ThenBy(e => e.EntryPointPath),
                        true);
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // Resolve Id from TestSetExecutionName
                Int64? testSetExecutionId = null;
                if (!string.IsNullOrEmpty(TestSetExecutionName))
                {
                    testSetExecutionId = drive.ResolveTestSetExecutionId(folder, TestSetExecutionName);
                    if (testSetExecutionId is null)
                    {
                        WriteWarning($"TestSetExecution '{TestSetExecutionName}' not found in folder '{folder.GetPSPath()}'.");
                        continue;
                    }
                }

                string? filter = MakeFilter(testSetExecutionId);
                var entities = drive.GetTestCaseExecutions(folder, filter, skip, first);

                WriteObject(entities
                    .FilterByWildcards(e => e?.EntryPointPath, wpName)
                    .OrderBy(e => e.TestSetExecutionId)
                    .ThenBy(e => e.EntryPointPath),
                    true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseExecutionError", ErrorCategory.InvalidOperation, target));
            }
        }
    }

    private void ProcessById()
    {
        // If Path is not bound via pipeline, use the current location
        var path = Path ?? [SessionState.Path.CurrentLocation.Path];
        var (drive, folder) = SessionState.ResolveToSingleFolder(path[0]);

        // Duplicate check (skip if the same folder + TestSetExecutionId was already processed)
        var key = (folder.Id!.Value, TestSetExecutionId);
        if (!_processedIds.Add(key))
        {
            return; // Already processed
        }

        var wpName = Name.ConvertToWildcardPatternList();

        try
        {
            string? filter = MakeFilter(TestSetExecutionId);
            var entities = drive.GetTestCaseExecutions(folder, filter, 0, ulong.MaxValue);

            WriteObject(entities
                .FilterByWildcards(e => e?.EntryPointPath, wpName)
                .OrderBy(e => e.EntryPointPath),
                true);
        }
        catch (Exception ex)
        {
            string target = folder.GetPSPath();
            WriteError(new ErrorRecord(new OrchException(target, ex), "GetTestCaseExecutionError", ErrorCategory.InvalidOperation, target));
        }
    }
}
