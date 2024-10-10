using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.User))]
    public class RemoveUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FullNameCompleter))]
        public string[]? FullName { get; set; }

        [Parameter]
        public SwitchParameter WarnOnNoMatch { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName_FullName>))]
        public string[]? Path { get; set; }

        private class UserNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.UserName_FullName.Parameters, wordToComplete);

                // パラメータで選択された FullName のみ対象とする
                var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.UserName_FullName.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.UserName))
                        .ExcludeByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.UserName))
                    {
                        string tiphelp = TipHelp2(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private class FullNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択された UserName のみ対象とする
                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.UserName_FullName.Parameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpFullName = CreateWPListFromParameter(commandAst, "FullName", Positional.UserName_FullName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.FullName))
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .ExcludeByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.FullName))
                    {
                        string tiphelp = TipHelp2(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.FullName), e.FullName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            if (UserName != null && UserName.All(u => string.IsNullOrEmpty(u))) UserName = null;
            if (FullName != null && FullName.All(f => string.IsNullOrEmpty(f))) FullName = null;

            if (UserName == null && FullName == null)
            {
                WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveUserError", ErrorCategory.InvalidOperation, this));
                return;
            }

            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpUserName = UserName.ConvertToWildcardPatternList();
            var wpFullName = FullName.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var users = drive.GetUsers();
                    var targetUsers = users
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .ToList();

                    if (WarnOnNoMatch.IsPresent && targetUsers.Count == 0)
                    {
                        WriteWarning($"No match found for UserName '{UserName?[0]}' and FullName '{FullName?[0]}'.");
                        continue;
                    }

                    foreach (var user in targetUsers.OrderBy(u => u.UserName))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string target = user.GetPSPath();
                        if (!string.IsNullOrEmpty(user.FullName))
                        {
                            target += $" ({user.FullName})";
                        }

                        if (ShouldProcess(target, "Remove User"))
                        {
                            try
                            {
                                drive.OrchAPISession.DeleteUser(user.Id ?? 0);
                                drive._dicUsers = null;
                                drive._dicUsersDetailed = null;
                            }
                            catch (Exception ex)
                            {
                                var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemoveUserError", ErrorCategory.InvalidOperation, user);
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
                    var errorRecord = new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive);
                    WriteError(errorRecord);
                }
            }
        }
    }
}
