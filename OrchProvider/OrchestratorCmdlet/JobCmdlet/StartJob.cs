using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
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

    [Parameter(Position = 0, Mandatory = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1)]
    [ArgumentCompleter(typeof(RuntimeTypeCompleter))]
    public string? RuntimeType { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item1>))]
    public int? JobsCount { get; set; }

    [Parameter(Position = 3)]
    [ArgumentCompleter(typeof(InputArgumentsCompleter))]
    public string? InputArguments { get; set; }

    [Parameter]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var processes = drive!.GetReleases(folder);
                foreach (var proc in processes
                    .Where(p => wp.IsMatch(p.Name))
                    .ExcludeByWildcards(r => r?.Name, wpName)
                    .OrderBy(proc => proc.Name))
                {
                    string tiphelp = proc.Name;
                    if (!string.IsNullOrEmpty(proc.Description))
                    {
                        tiphelp += $" ({proc.Description})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(proc.Name), proc.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

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

            // TODO: wordToComplete をワイルドカードにして、結果を絞り込む方が良い

            foreach (var (drive, folder) in drivesFolders)
            {
                var runtimeTypes = drive.OrchAPISession.GetRuntimesForFolder(folder.Id ?? 0);
                foreach (var type in runtimeTypes?.Where(type => type.Total > 0) ?? [])
                {
                    string tiphelp = $"{type.Available} Runtimes Available, {type.Connected} Connected";
                    yield return new CompletionResult(type.Type, type.Type, CompletionResultType.ParameterValue, tiphelp);
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

            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var processes = drive!.GetReleases(folder);
                foreach (var proc in processes
                    .Where(p => wp.IsMatch(p.Name))
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(proc => proc.Name))
                {
                    //if (proc is null || proc.Arguments is not null && !string.IsNullOrEmpty(proc.Arguments.Input))
                    if (string.IsNullOrEmpty(proc?.InputArguments)) continue;

                    yield return new CompletionResult($"'{proc.InputArguments}'");

                    // 最初のプロセスの引数のみ処理する
                    yield break;
                }
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
