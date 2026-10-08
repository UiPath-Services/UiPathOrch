using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Start, "OrchJob", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Job))]
public class StartJobCmdlet : OrchestratorPSCmdlet
{
    private static readonly string[] validRuntimeType = [
        "NonProduction",
        "Attended",
        "Unattended",
        "Development",
        "Studio",
        "RpaDeveloper",
        "StudioX",
        "CitizenDeveloper",
        "Headless",
        "RpaDeveloperPro",
        "StudioPro",
        "TestAutomation",
        "AutomationCloud",
        "Serverless",
        "AutomationKit",
        "ServerlessTestAutomation",
        "AutomationCloudTestAutomation",
        "AttendedStudioWeb"
    ];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RuntimeTypeCompleter))]
    public string? RuntimeType { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item1>))]
    public int? JobsCount { get; set; }

    [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(InputArgumentsCompleter))]
    public string? InputArguments { get; set; }

    // The parameters below carry the StartProcessDto property names, as New-OrchTrigger does.

    // This parameter does not accept CSV import
    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobPriorityItems>))]
    public string? Priority { get; set; }

    // Since we can just treat "" in CSV as 45, the type is int
    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public int? SpecificPriorityValue { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<SoftStop_Kill>))]
    public string? StopStrategy { get; set; }

    // Seconds after which a running job is stopped (the swagger's description).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StopProcessExpression { get; set; }

    // Grace period in seconds after a soft stop, after which the job is killed.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? KillProcessExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? AlertPendingExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? AlertRunningExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class RuntimeTypeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.RuntimesForFolder.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var machineRuntime in result
                    .Where(mr => wp.IsMatch(mr.Type))
                    .Where(mr => mr.Total > 0)
                    .OrderBy(mr => mr.Type))
                {
                    string tiphelp = $"{machineRuntime.Available} Runtimes Available, {machineRuntime.Connected} Connected";
                    yield return new CompletionResult(machineRuntime.Type, machineRuntime.Type, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class InputArgumentsCompleter : OrchArgumentCompleter
    {
        static bool IsNumericType(string? typeName)
        {
            if (typeName is null) return false;
            var type = Type.GetType(typeName);
            if (type is null)
                return false;
            // IsPrimitive includes bool and char, so exclude those
            return (type.IsPrimitive && type != typeof(bool) && type != typeof(char))
                   || type == typeof(decimal);
        }

        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Releases.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var proc in result
                    .Where(p => wp.IsMatch(p.Name))
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(proc => proc.Name))
                {
                    if (string.IsNullOrEmpty(proc?.Arguments?.Input)) continue;

                    var args = JsonSerializer.Deserialize<InputArgument[]>(proc.Arguments.Input);
                    if (args is null) continue;
                    string json = "{" + string.Join(",", args.Select(a =>
                    {
                        string value;
                        // Convert to a type object for accurate comparison
                        var type = Type.GetType(a.type ?? "string");
                        if (type == typeof(string)) { value = "\"\""; }
                        else if (type == typeof(bool)) { value = "false"; }
                        else if (type == typeof(DateTime)) { value = $"\"{DateTime.Today:yyyy-MM-dd HH:mm:ss}\""; }
                        else if (IsNumericType(a.type)) { value = "0"; }
                        else { value = "\"\""; }
                        return $"\"{a.name}\":{value}";
                    })) + "}";
                    yield return new CompletionResult(PathTools.EscapePSText(json), json, CompletionResultType.ParameterValue, proc.GetPSPath());
                }
            }
        }
    }

    // An empty CSV cell binds as "": send it as absent, as New-OrchTrigger does.
    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    protected override void ProcessRecord()
    {
        if (!string.IsNullOrEmpty(RuntimeType) && !validRuntimeType.Contains(RuntimeType))
        {
            WriteError(new ErrorRecord(
                new ArgumentException($"Invalid RuntimeType: '{RuntimeType}'. Valid values are: {string.Join(", ", validRuntimeType)}.", nameof(RuntimeType)),
                "InvalidRuntimeType",
                ErrorCategory.InvalidArgument,
                RuntimeType));
            return;
        }

        // Priority wins over SpecificPriorityValue, as in New-OrchTrigger.
        int? specificPriorityValue = SpecificPriorityValue;
        if (!string.IsNullOrEmpty(Priority))
        {
            specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);
            if (specificPriorityValue is null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"Invalid Priority: '{Priority}'. Valid values are: {string.Join(", ", JobPriorityItems.Items)}.", nameof(Priority)),
                    "InvalidPriority",
                    ErrorCategory.InvalidArgument,
                    Priority));
                return;
            }
        }

        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name!.ConvertToWildcardPatternList();
        HashSet<OrchDriveInfo> warnedDrives = [];

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var processes = drive.Releases.Get(folder);
                foreach (var process in processes.FilterByWildcards(p => p?.Name, wpName).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(process.GetPSPath(), "Start Job"))
                    {
                        try
                        {
                            StartProcess startProcess = new()
                            {
                                ReleaseKey = process.Key!,
                                Strategy = "ModernJobsCount",
                                RuntimeType = RuntimeType,
                                JobsCount = JobsCount,
                                InputArguments = InputArguments,
                                SpecificPriorityValue = specificPriorityValue,
                                StopStrategy = NullIfEmpty(StopStrategy),
                                StopProcessExpression = NullIfEmpty(StopProcessExpression),
                                KillProcessExpression = NullIfEmpty(KillProcessExpression),
                                AlertPendingExpression = NullIfEmpty(AlertPendingExpression),
                                AlertRunningExpression = NullIfEmpty(AlertRunningExpression),
                            };

                            // Say once per drive which of the given values this server's API does not take.
                            var dropped = OrchAPISession.StripStartProcessFieldsForApiVersion(startProcess, drive.OrchAPISession.ApiVersion);
                            if (dropped.Length > 0 && warnedDrives.Add(drive))
                            {
                                WriteWarning($"[{MyInvocation.MyCommand.Name}] {drive.NameColonSeparator} (API v{drive.OrchAPISession.ApiVersion}) does not accept {string.Join(", ", dropped)}; the job is started without them.");
                            }

                            WriteObject(drive.StartJobs(folder, startProcess), true);
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(process.GetPSPath(), ex), "StartJobError", ErrorCategory.InvalidOperation, process);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "StartJobError", ErrorCategory.InvalidOperation, folder);
                WriteError(errorRecord);
            }
        }
    }
}
