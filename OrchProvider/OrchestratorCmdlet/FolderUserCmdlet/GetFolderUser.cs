using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchFolderUser")]
[OutputType(typeof(Entities.UserRoles))]
public class GetFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserUserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserFullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter]
    public SwitchParameter IncludeInherited { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
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

    private class FolderUserUserNameCompleter : OrchArgumentCompleter
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
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            // パラメータで選択された FullName のみ対象とする
            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);
            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(fu => wp.IsMatch(fu.UserEntity!.UserName))
                    .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .ExcludeByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class FolderUserFullNameCompleter : OrchArgumentCompleter
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
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            // パラメータで選択済みの FullName は、候補から除外する
            var wpFullName = CreateWPListFromParameter(commandAst, "FullName", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(fu => wp.IsMatch(fu.UserEntity?.FullName))
                    .ExcludeByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.FullName), userRoles.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private static void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.UserRoles> output)
    {
        // 各フォルダーユーザーに対してデータ行を書き込む
        foreach (var p in output)
        {
            string[] line = [
                EscapeCsvValue(p.Path, true),
                EscapeCsvValue(p?.UserEntity?.Type),

                // DirectoryGroup については、UserName 列に FullName を出力する。。
                // これは、PmBulkResolveByName() が case を区別するためだ。
                // Add-OrchFolderUser でグループをインポートできるようにするには、こうしておく必要がある。。
                p?.UserEntity?.Type == "DirectoryGroup" ?
                    EscapeCsvValue(p?.UserEntity?.FullName) :
                    EscapeCsvValue(p?.UserEntity?.UserName),

                EscapeCsvValue(p?.UserEntity?.FullName),
                EscapeCsvValue(string.Join(",", p?.Roles?.Select(r => WildcardPattern.Escape(r.Name))!))
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var userRoles = result.GetResult(cancelHandler.Token);
                if (userRoles is null) continue;

                var targets = userRoles
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.Type!)
                    .ThenBy(u => u.UserEntity!.UserName!)
                    .ThenBy(u => u.UserEntity!.FullName);

                if (writer is not null)
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

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
