using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchUser")]
    [OutputType(typeof(Entities.User))]
    public class GetUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(TenantUserNameCompleter<Positional.UserName_FullName>))]
        public string[]? UserName { get; set; }

        [Parameter(Position = 1)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FullNameCompleter))]
        public string[]? FullName { get; set; }

        [Parameter]
        public SwitchParameter ExpandDetails { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName_FullName>))]
        public string[]? Path { get; set; }

        //[Parameter]
        //public string? ExportCsv { get; set; }

        //[Parameter]
        //[ArgumentCompleter(typeof(EncodingCompleter))]
        //[EncodingArgumentTransformation]
        //public Encoding? CsvEncoding { get; set; }

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
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.FullName), e.FullName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpUserName = UserName.ConvertToWildcardPatternList();
            var wpFullName = FullName.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drives, 
                drive => drive.NameColonSeparator, 
                drive => drive, 
                drive => drive.GetUsers());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var users = result.GetResult(cancelHandler.Token);
                    if (users == null) continue;

                    var drive = result.Source;

                    var targetUsers = users
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .OrderBy(u => u.FullName);

                    if (ExpandDetails.IsPresent)
                    {
                        foreach (var user in targetUsers)
                        {
                            var detailedUser = drive!.GetUser(user);
                            WriteObject(detailedUser);
                        }
                    }
                    else
                    {
                        WriteObject(targetUsers, true);
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
