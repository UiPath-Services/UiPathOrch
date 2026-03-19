using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableMaintenanceModeCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? MachineName { get; set; }
    public virtual string[]? HostMachineName { get; set; }
    public virtual string[]? ServiceUserName { get; set; }
    public virtual Int64[]? SessionId { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private static string _propValue = Enable.Value ? "Default" : "Enabled";

    internal class MachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpMachineName     = CreateSelfExclusionList(commandAst, "MachineName", wordToComplete);
            var wpHostMachineName = GetFakeBoundParameters(fakeBoundParameters, "HostMachineName").ConvertToWildcardPatternList();
            var wpServiceUserName = GetFakeBoundParameters(fakeBoundParameters, "ServiceUserName").ConvertToWildcardPatternList();
            var wpSessionId       = GetFakeBoundParameters(fakeBoundParameters, "SessionId").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.MachineSessionRuntimes.Get());
            //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

            foreach (var result in results)
            {
                foreach (var session in result
                    .Where(session => session.SessionId is not null)
                    .Where(session => !string.IsNullOrEmpty(session.MachineName))
                    .Where(session => session.Runtimes != 0)
                    .Where(session => session.MaintenanceMode == _propValue)
                    .Where(session => wp.IsMatch(session.MachineName))
                    .ExcludeByWildcards(session => session?.MachineName, wpMachineName)
                    .FilterByWildcards(session => session?.HostMachineName, wpHostMachineName)
                    .FilterByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                    .FilterByWildcards(session => session?.SessionId.ToString(), wpSessionId)
                    .DistinctBy(session => session.SessionId)
                    .OrderBy(session => session.MachineName))
                {
                    //string tiphelp = TipHelp(session);
                    yield return new CompletionResult(PathTools.EscapePSText(session.MachineName), session.MachineName, CompletionResultType.Text, session.MachineName);
                }
            }
        }
    }

    internal class HostMachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpMachineName     = GetFakeBoundParameters(fakeBoundParameters, "MachineName").ConvertToWildcardPatternList();
            var wpHostMachineName = CreateSelfExclusionList(commandAst, "HostMachineName", wordToComplete);
            var wpServiceUserName = GetFakeBoundParameters(fakeBoundParameters, "ServiceUserName").ConvertToWildcardPatternList();
            var wpSessionId       = GetFakeBoundParameters(fakeBoundParameters, "SessionId").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.MachineSessionRuntimes.Get());
            //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

            foreach (var result in results)
            {
                foreach (var session in result
                    .Where(session => session.SessionId is not null)
                    .Where(session => !string.IsNullOrEmpty(session.HostMachineName))
                    .Where(session => session.Runtimes != 0)
                    .Where(session => session.MaintenanceMode == _propValue)
                    .Where(session => wp.IsMatch(session.HostMachineName))
                    .FilterByWildcards(session => session?.MachineName, wpMachineName)
                    .ExcludeByWildcards(session => session?.HostMachineName, wpHostMachineName)
                    .FilterByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                    .FilterByWildcards(session => session?.SessionId.ToString(), wpSessionId)
                    .DistinctBy(session => session.SessionId)
                    .OrderBy(session => session.HostMachineName))
                {
                    //string tiphelp = TipHelp(session);
                    yield return new CompletionResult(PathTools.EscapePSText(session.HostMachineName), session.HostMachineName, CompletionResultType.Text, session.HostMachineName);
                }
            }
        }
    }

    internal class ServiceUserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpMachineName     = GetFakeBoundParameters(fakeBoundParameters, "MachineName").ConvertToWildcardPatternList();
            var wpHostMachineName = GetFakeBoundParameters(fakeBoundParameters, "HostMachineName").ConvertToWildcardPatternList();
            var wpServiceUserName = CreateSelfExclusionList(commandAst, "ServiceUserName", wordToComplete);
            var wpSessionId       = GetFakeBoundParameters(fakeBoundParameters, "SessionId").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.MachineSessionRuntimes.Get());
            //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

            foreach (var result in results)
            {
                foreach (var session in result
                    .Where(session => session.SessionId is not null)
                    .Where(session => !string.IsNullOrEmpty(session.ServiceUserName))
                    .Where(session => session.Runtimes != 0)
                    .Where(session => session.MaintenanceMode == _propValue)
                    .Where(session => wp.IsMatch(session.ServiceUserName))
                    .FilterByWildcards(session => session?.MachineName, wpMachineName)
                    .FilterByWildcards(session => session?.HostMachineName, wpHostMachineName)
                    .ExcludeByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                    .FilterByWildcards(session => session?.SessionId.ToString(), wpSessionId)
                    .DistinctBy(session => session.SessionId)
                    .OrderBy(session => session.ServiceUserName))
                {
                    //string tiphelp = TipHelp(session);
                    yield return new CompletionResult(PathTools.EscapePSText(session.ServiceUserName), session.ServiceUserName, CompletionResultType.Text, session.ServiceUserName);
                }
            }
        }
    }

    internal class SessionIdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpMachineName     = GetFakeBoundParameters(fakeBoundParameters, "MachineName").ConvertToWildcardPatternList();
            var wpHostMachineName = GetFakeBoundParameters(fakeBoundParameters, "HostMachineName").ConvertToWildcardPatternList();
            var wpServiceUserName = GetFakeBoundParameters(fakeBoundParameters, "ServiceUserName").ConvertToWildcardPatternList();
            var wpSessionId       = CreateSelfExclusionList(commandAst, "SessionId", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.MachineSessionRuntimes.Get());
            //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

            foreach (var result in results)
            {
                foreach (var session in result
                    .Where(session => session.SessionId is not null)
                    .Where(session => session.Runtimes != 0)
                    .Where(session => session.MaintenanceMode == _propValue)
                    .Where(session => wp.IsMatch(session.SessionId.ToString()))
                    .FilterByWildcards(session => session?.MachineName, wpMachineName)
                    .FilterByWildcards(session => session?.HostMachineName, wpHostMachineName)
                    .FilterByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                    .ExcludeByWildcards(session => session?.SessionId.ToString(), wpSessionId)
                    .OrderBy(session => session.SessionId))
                {
                    string tiphelp = TipHelp(session);
                    yield return new CompletionResult(PathTools.EscapePSText(session.SessionId.ToString()), session.SessionId.ToString(), CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        var wpMachineName = MachineName.ConvertToWildcardPatternList();
        var wpHostMachineName = HostMachineName.ConvertToWildcardPatternList();
        var wpServiceUserName = ServiceUserName.ConvertToWildcardPatternList();
        var wpSessionId = SessionId?.Select(n => n.ToString()).ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} MaintenanceMode";

        foreach (var drive in drives)
        {
            try
            {
                var sessions = drive.MachineSessionRuntimes.Get();

                foreach (var session in sessions
                    .Where(session => session.SessionId is not null)
                    .Where(session => session.Runtimes != 0)
                    .Where(session => session.MaintenanceMode == _propValue)
                    .FilterByWildcards(session => session?.MachineName, wpMachineName)
                    .FilterByWildcards(session => session?.HostMachineName, wpHostMachineName)
                    .FilterByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                    .FilterByStructValues(session => session.SessionId ?? 0, SessionId)
                    .DistinctBy(session => session.SessionId)
                    .OrderBy(session => session.SessionId))
                {
                    string target = System.IO.Path.Combine(drive.NameColon, session.SessionId.ToString()!);

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            drive.OrchAPISession.SetMaintenanceMode(session.SessionId, Enable.Value, Force.IsPresent);
                            drive.MachineSessionRuntimes.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}MaintenanceModeError";
                            WriteError(new ErrorRecord(new OrchException(target, ex), errorId, ErrorCategory.InvalidOperation, session));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachineSessionError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
