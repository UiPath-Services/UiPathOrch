using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

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
        [ArgumentCompleter(typeof(DriveCompleter))]
        public string[]? Path { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedUsers.csv";
        private static readonly string[] CsvHeaders = [
            "Path",
            "UserName",
            "FullName",
            "Type",
            "MayHaveUserSession",
            "MayHaveRobotSession",
            "MayHaveUnattendedSession",
            "MayHavePersonalWorkspace",
            "RestrictToPersonalWorkspace",
            "UpdatePolicyType",
            "UpdatePolicyVersion",
            "UR_UserName",
            "UR_Password",
            "UR_CredentialStore",
            "UR_CredentialExternalName",
            "UR_CredentialType",
            "UR_LimitConcurrentExecution",
            "ES_TracingLevel",
            "ES_StudioNotifyServer",
            "ES_LoginToConsole",
            "ES_ResolutionWidth",
            "ES_ResolutionHeight",
            "ES_ResolutionDepth",
            "ES_FontSmoothing",
            "ES_AutoDownloadProcess",
            "Roles"
        ];

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

        private static void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, User? p)
        {
            if (p == null) return;

            string ur_credentialStore = null;
            if (p.UnattendedRobot?.CredentialStoreId != null)
            {
                var credentialStores = drive.GetCredentialStores();
                var credentialStore = credentialStores.FirstOrDefault(c => c.Id == p.UnattendedRobot.CredentialStoreId);
                ur_credentialStore = credentialStore?.Name;
            }

            string[] line = [
                EscapeCsvValue(p.Path, true),
                EscapeCsvValue(p.UserName, true),
                EscapeCsvValue(p.FullName),
                EscapeCsvValue(p.Type),
                EscapeCsvValue(p.MayHaveUserSession),
                EscapeCsvValue(p.MayHaveRobotSession),
                EscapeCsvValue(p.MayHaveUnattendedSession),
                EscapeCsvValue(p.MayHavePersonalWorkspace),
                EscapeCsvValue(p.RestrictToPersonalWorkspace),
                EscapeCsvValue(p.UpdatePolicy?.Type),
                EscapeCsvValue(p.UpdatePolicy?.SpecificVersion),
                EscapeCsvValue(p.UnattendedRobot?.UserName),
                EscapeCsvValue(""), // p.UnattendedRobot?.Password // ここにはごみなテキストが入っているので、出力しないでおく
                EscapeCsvValue(ur_credentialStore, true),
                EscapeCsvValue(p.UnattendedRobot?.CredentialExternalName),
                EscapeCsvValue(p.UnattendedRobot?.CredentialType),
                EscapeCsvValue(p.UnattendedRobot?.LimitConcurrentExecution),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.TracingLevel),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.StudioNotifyServer),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.LoginToConsole),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionWidth),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionHeight),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.ResolutionDepth),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.FontSmoothing),
                EscapeCsvValue(p.UnattendedRobot?.ExecutionSettings?.AutoDownloadProcess),
                EscapeCsvValue(p.RolesList, true)
            ];

            WriteCsvLine(writer, line);
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpUserName = UserName.ConvertToWildcardPatternList();
            var wpFullName = FullName.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var users = drive.GetUsers();
                    var targetUsers = users
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .OrderBy(u => u.UserName)
                        .ToList();

                    // 詳細を取得する必要があれば、ユーザーごとにスレッドを起こして
                    // GetUser(user) を呼び出す
                    if (ExpandDetails.IsPresent || writer != null)
                    {

                        using var results = OrchThreadPool.RunForEach(targetUsers
                                .FilterByWildcards(u => u?.FullName, wpFullName)
                                .FilterByWildcards(u => u?.UserName, wpUserName)
                                .OrderBy(u => u.UserName),
                            user => user.GetPSPath(),
                            user => user,
                            user => drive.GetUser(user));

                        int index = 0;
                        string msg = "Get users... ";
                        using var reporter = new ProgressReporter(this, 1, targetUsers.Count, msg, msg);
                        foreach (var result in results)
                        {
                            try
                            {
                                var detailedUser = result.GetResult(cancelHandler.Token);
                                if (detailedUser == null) continue;

                                reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {detailedUser.GetPSPath()}");

                                if (writer != null) WriteCsvContent(writer, drive, detailedUser);
                                else WriteObject(detailedUser);
                            }
                            catch (OrchException ex)
                            {
                                WriteError(new ErrorRecord(ex, "GetUserError", ErrorCategory.InvalidOperation, ex.Target));
                            }
                        }
                    }
                    else
                    {
                        WriteObject(targetUsers, true);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
