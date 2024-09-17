using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.MachineName_HostMachineName_ServiceUserName_SessionId;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchMaintenanceMode", SupportsShouldProcess = true)]
    public class DisableMaintenanceModeCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter))]
        public string[]? MachineName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(HostMachineNameCompleter))]
        public string[]? HostMachineName { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ServiceUserNameCompleter))]
        public string[]? ServiceUserName { get; set; }

        [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(SessionIdCompleter))]
        public Int64[]? SessionId { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.MachineName_HostMachineName_ServiceUserName_SessionId>))]
        public string[]? Path { get; set; }

        internal class MachineNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                var wpMachineName     = CreateWPListFromParameter(commandAst, "MachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters, wordToComplete);
                var wpHostMachineName = CreateWPListFromOtherParameters(commandAst, "HostMachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpServiceUserName = CreateWPListFromOtherParameters(commandAst, "ServiceUserName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpSessionId       = CreateWPListFromOtherParameters(commandAst, "SessionId", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachineSessionRuntimes());
                //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var session in entities!
                        .Where(session => session.SessionId != null)
                        .Where(session => !string.IsNullOrEmpty(session.MachineName))
                        .Where(session => session.Runtimes != 0)
                        .Where(session => session.MaintenanceMode == "Enabled")
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
                var drives = ResolveDrives(fakeBoundParameters);

                var wpMachineName     = CreateWPListFromOtherParameters(commandAst, "MachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpHostMachineName = CreateWPListFromParameter(commandAst, "HostMachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters, wordToComplete);
                var wpServiceUserName = CreateWPListFromOtherParameters(commandAst, "ServiceUserName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpSessionId       = CreateWPListFromOtherParameters(commandAst, "SessionId", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachineSessionRuntimes());
                    //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var session in entities!
                        .Where(session => session.SessionId != null)
                        .Where(session => !string.IsNullOrEmpty(session.HostMachineName))
                        .Where(session => session.Runtimes != 0)
                        .Where(session => session.MaintenanceMode == "Enabled")
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
                var drives = ResolveDrives(fakeBoundParameters);

                var wpMachineName     = CreateWPListFromOtherParameters(commandAst, "MachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpHostMachineName = CreateWPListFromOtherParameters(commandAst, "HostMachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpServiceUserName = CreateWPListFromParameter(commandAst, parameterName, Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters, wordToComplete);
                var wpSessionId       = CreateWPListFromOtherParameters(commandAst, "SessionId", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachineSessionRuntimes());
                //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var session in entities!
                        .Where(session => session.SessionId != null)
                        .Where(session => !string.IsNullOrEmpty(session.ServiceUserName))
                        .Where(session => session.Runtimes != 0)
                        .Where(session => session.MaintenanceMode == "Enabled")
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
                var drives = ResolveDrives(fakeBoundParameters);

                var wpMachineName     = CreateWPListFromOtherParameters(commandAst, "MachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpHostMachineName = CreateWPListFromOtherParameters(commandAst, "HostMachineName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpServiceUserName = CreateWPListFromOtherParameters(commandAst, "ServiceUserName", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters);
                var wpSessionId       = CreateWPListFromParameter(commandAst, "SessionId", Positional.MachineName_HostMachineName_ServiceUserName_SessionId.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetMachineSessionRuntimes());
                //"&$filter=((Runtimes%20ne%200)%20and%20(((RuntimeType%20eq%20%273%27)%20or%20(RuntimeType%20eq%20%270%27)%20or%20(RuntimeType%20eq%20%277%27)%20or%20(RuntimeType%20eq%20%272%27)%20or%20(RuntimeType%20eq%20%278%27)%20or%20(RuntimeType%20eq%20%2712%27))))"););

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var session in entities!
                        .Where(session => session.SessionId != null)
                        .Where(session => session.Runtimes != 0)
                        .Where(session => session.MaintenanceMode == "Enabled")
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
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpMachineName = MachineName.ConvertToWildcardPatternList();
            var wpHostMachineName = HostMachineName.ConvertToWildcardPatternList();
            var wpServiceUserName = ServiceUserName.ConvertToWildcardPatternList();
            var wpSessionId = SessionId?.Select(n => new WildcardPattern(n.ToString(), WildcardOptions.IgnoreCase)).ToList();

            foreach (var drive in drives)
            {
                try
                {
                    var sessions = drive.GetMachineSessionRuntimes();

                    foreach (var session in sessions
                        .Where(session => session.SessionId != null)
                        .Where(session => session.Runtimes != 0)
                        .Where(session => session.MaintenanceMode == "Enabled")
                        .FilterByWildcards(session => session?.MachineName, wpMachineName)
                        .FilterByWildcards(session => session?.HostMachineName, wpHostMachineName)
                        .FilterByWildcards(session => session?.ServiceUserName, wpServiceUserName)
                        .FilterByStructValues(session => session.SessionId ?? 0, SessionId)
                        .DistinctBy(session => session.SessionId)
                        .OrderBy(session => session.SessionId))
                    {
                        string target = System.IO.Path.Combine(drive.NameColon, session.SessionId.ToString()!);
                        if (ShouldProcess(target, "Disable MaintenanceMode"))
                        {
                            try
                            {
                                drive.OrchAPISession.SetMaintenanceMode(session.SessionId, false);
                                drive._dicMachineSessionRuntimes = null;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "DisableMaintenanceModeError", ErrorCategory.InvalidOperation, session));
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
}
