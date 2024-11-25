using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Search, "OrchPmDirectoryUser")]
    [OutputType(typeof(Entities.PmDirectoryEntityInfo))]
    public class SearchPmDirectoryUser : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
                if (string.IsNullOrEmpty(wordToComplete))
                {
                    yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                    yield break;
                }

                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

                var drives = ResolveDrives(fakeBoundParameters);

                bool bFound = false;
                foreach (var drive in drives)
                {
                    var users = drive.SearchPmDirectoryUsers(wordToComplete);
                    if (users == null) continue;

                    foreach (var user in users
                        .ExcludeByWildcards(e => e?.identityName, wpUserName)
                        .OrderBy(e => e.identityName))
                    {
                        bFound = true;
                        string tiphelp = TipHelp(user);
                        yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                    }
                }
                if (!bFound)
                {
                    yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                string partitionGlobalId = drive.GetPartitionGlobalId();

                foreach (var userName in UserName!)
                {
                    try
                    {
                        var users = drive.SearchPmDirectoryUsers(userName);
                        WriteObject(users, true);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchPmDirectoryUserError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
