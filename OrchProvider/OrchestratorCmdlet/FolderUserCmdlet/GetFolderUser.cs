using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchFolderUser")]
    [OutputType(typeof(Entities.UserRoles))]
    public class GetFolderUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(FullNameCompleter))]
        [SupportsWildcards]
        public string[]? FullName { get; set; }

        [Parameter]
        public SwitchParameter IncludeInherited { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedFolderUsers.csv";
        private static readonly string[] CsvHeaders = [
            "Path",
            "Type",
            "UserName",
            "FullName",
            "FolderRoles",
        ];

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.UserRoles> output)
        {
            // 各プロセスに対してデータ行を書き込む
            foreach (var p in output)
            {
                var line = new StringBuilder();

                line.Append($"{EscapeCsvValue(p.Path, true)},");
                line.Append($"{EscapeCsvValue(p?.UserEntity?.Type)},");
                line.Append($"{EscapeCsvValue(p?.UserEntity?.UserName, true)},");
                line.Append($"{EscapeCsvValue(p?.UserEntity?.FullName)},");
                line.Append($"{EscapeCsvValue(string.Join(",", p?.Roles?.Select(r => WildcardPattern.Escape(r.Name))!))}");

                writer.WriteLine(line.ToString());
            }
        }

        private class UserNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                //var drivesFolders = ResolveDrivesFoldersWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

                // パラメータで選択済みの UserName は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.UserName_FullName.Parameters, wordToComplete);

                // パラメータで選択された FullName のみ対象とする
                var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.UserName_FullName.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, IncludeInherited));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(fu => wp.IsMatch(fu.UserEntity!.UserName))
                        .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                        .ExcludeByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                        .OrderBy(u => u.UserEntity!.UserName))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserEntity!.UserName), e.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
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
                //var drivesFolders = ResolveDrivesFoldersWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

                // パラメータで選択された UserName のみ対象とする
                var paramUserName = GetParameterValues(commandAst, "UserName", Positional.UserName_FullName.Parameters);
                var wpUserName = paramUserName.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みの FullName は、候補から除外する
                var wpFullName = CreateWPListFromParameter(commandAst, "FullName", Positional.UserName_FullName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, IncludeInherited));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(fu => wp.IsMatch(fu.UserEntity?.FullName))
                        .ExcludeByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                        .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                        .OrderBy(u => u.UserEntity!.FullName))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserEntity!.FullName), e.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpUserName = UserName?.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();
            var wpFullName = FullName?.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetUsersForFolder(df.folder, IncludeInherited.IsPresent));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var userRoles = result.GetResult(cancelHandler.Token);
                    if (userRoles == null) continue;

                    var targets = userRoles
                        .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                        .OrderBy(u => u.UserEntity!.Type!)
                        .ThenBy(u => u.UserEntity!.UserName!)
                        .ThenBy(u => u.UserEntity!.FullName);

                    if (writer != null)
                    {
                        WriteCsvContent(writer, targets);
                    }
                    else
                    {
                        WriteObject(targets, true);
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetFolderUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
