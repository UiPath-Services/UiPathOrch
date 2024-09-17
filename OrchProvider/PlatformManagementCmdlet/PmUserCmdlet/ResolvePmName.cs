using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Type_UserName;
using Types = UiPath.PowerShell.Positional.User_Group_Application;

namespace UiPath.PowerShell.Commands
{
    // TODO: OrchProvider.Format.ps1xml を直して、取得した users の各エントリに Path を設定しないと。
    [Cmdlet(VerbsDiagnostic.Resolve, "OrchPmNames")]
    [OutputType(typeof(Entities.Member))]
    class ResolvePmDirectoryName: OrchestratorPSCmdlet
    {
        private static readonly string[] types = ["User", "Group", "Application"];

        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Types>))]
        [SupportsWildcards]
        public string[]? Type { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Type_UserName>))]
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

                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.Type_UserName.Parameters, wordToComplete);

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
            var wpType = Type?.Select(t => new WildcardPattern(t, WildcardOptions.IgnoreCase)).ToList();
            var specifiedTypes = types.FilterByWildcards(t => t, wpType);

            foreach (var drive in drives)
            {
                string partitionGlobalId = drive.GetPartitionGlobalId();

                foreach (var t in specifiedTypes)
                {
                    try
                    {
                        var users = drive.PmBulkResolveByName(t, UserName!);
                        WriteObject(users, true);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "ResolvePmNamesError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
