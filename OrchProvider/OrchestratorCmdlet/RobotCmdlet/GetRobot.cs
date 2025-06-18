using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.FullName_Username;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchRobot")]
[OutputType(typeof(Entities.Robot))]
public class GetRobotCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FullNameCompleter))]
    public string[]? FullName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(UsernameCompleter))]
    public string[]? Username { get; set; } // Entities.Robot の定義を尊重した capitalization

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class FullNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpFullName = CreateWPListFromParameter(commandAst, "FullName", TPositional.Parameters, wordToComplete);
            var wpUsername = CreateWPListFromOtherParameters(commandAst, "Username", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drives, drive => drive.Robots.Get());

            foreach (var robot in results
                .Select(r => r.Item)
                .Where(r => wp.IsMatch(r.Name))
                .ExcludeByWildcards(r => r?.User?.FullName, wpFullName)
                .FilterByWildcards(r => r?.Username, wpUsername)
                .OrderBy(r => r.User?.FullName))
            {
                string tiphelp = robot.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(robot.User!.FullName), robot.User.FullName, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    private class UsernameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);
            var wpUsername = CreateWPListFromParameter(commandAst, "Username", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drives, drive => drive.Robots.Get());

            foreach (var robot in results
                .Select(r => r.Item)
                .Where(r => !string.IsNullOrEmpty(r.Username))
                .Where(r => wp.IsMatch(r.Name))
                .FilterByWildcards(r => r?.User?.FullName, wpFullName)
                .ExcludeByWildcards(r => r?.Username, wpUsername)
                .OrderBy(r => r.Username))
            {
                string tiphelp = robot.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(robot.Username), robot.Username, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpUsername = Username.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.Robots.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var robots = result.GetResult(cancelHandler.Token);
                if (robots is null) continue;

                WriteObject(robots
                    .FilterByWildcards(r => r?.User?.FullName, wpFullName)
                    .FilterByWildcards(r => r?.Username, wpUsername)
                    .OrderBy(r => r.User?.FullName)
                    .ThenBy(r => r.Username),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetRobotError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
