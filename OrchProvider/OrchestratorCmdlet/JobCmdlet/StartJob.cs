using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_RuntimeType_JobsCount;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Start, "OrchJob", SupportsShouldProcess = true)]
public class StartJobCommand : OrchestratorPSCmdlet
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
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class RuntimeTypeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.RuntimesForFolder.Get(df.folder));

            foreach (var machineRuntime in results
                .Select(r => r.Item)
                .Where(mr => wp.IsMatch(mr.Type))
                .Where(mr => mr.Total > 0)
                .OrderBy(mr => mr.Type))
            {
                string tiphelp = $"{machineRuntime.Available} Runtimes Available, {machineRuntime.Connected} Connected";
                yield return new CompletionResult(machineRuntime.Type, machineRuntime.Type, CompletionResultType.ParameterValue, tiphelp);
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
            // IsPrimitive は bool や char も含むため、それらは除外する
            return (type.IsPrimitive && type != typeof(bool) && type != typeof(char))
                   || type == typeof(decimal);
        }

        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.GetReleases(df.folder));

            foreach (var proc in results
                .Select(r => r.Item)
                .Where(p => wp.IsMatch(p.Name))
                .FilterByWildcards(r => r?.Name, wpName)
                .OrderBy(proc => proc.Name))
            {
                if (string.IsNullOrEmpty(proc?.Arguments?.Input)) continue;

                var args = JsonSerializer.Deserialize<InputArgument[]>(proc.Arguments.Input);
                if (args is null) continue;
                string json = "{" + string.Join(",", args.Select(a => {
                    string value;
                    // 型オブジェクトに変換して正確な比較を行う
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

    protected override void ProcessRecord()
    {
        if (!string.IsNullOrEmpty(RuntimeType) && !validRuntimeType.Contains(RuntimeType))
        {
            throw new Exception($"Invalid RuntimeType: {RuntimeType}.");
        }

        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name!.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var processes = drive.GetReleases(folder);
                foreach (var process in processes.FilterByWildcards(p => p?.Name, wpName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(process.GetPSPath(), "Start Job"))
                    {
                        try
                        {
                            WriteObject(drive.StartJobs(folder, process.Key!, RuntimeType, JobsCount, InputArguments), true);
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
