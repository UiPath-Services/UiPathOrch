using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.GroupName_Type_UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "PmGroupMember", SupportsShouldProcess = true)]
public class AddPmGroupMemberCommand : OrchestratorPSCmdlet
{
    // パラメータを、Path と PmGroup だけ展開して保持
    //private List<(OrchDriveInfo Drive, PmGroup Group, string Type, string UserName)>? _csvLines;
    //private HashSet<(OrchDriveInfo Drive, PmGroup Group, string Type, string UserName)>? _csvLines;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<DirectoryTypes>))]
    [SupportsWildcards]
    public string[]? Type { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private static void CacheExistingMemberIds(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
    {
        var results = ParallelResults.ForEach(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.PmGroups.Get(group?.id));
        });
    }

    private class PmUserNameCompleter : OrchArgumentCompleter
    {
        private static bool IsMemberOf(PmGroup group, PmDirectoryEntityInfo user)
        {
            // identifier では正しく確認できない。identifierName で確認する必要がある。
            // ひとつでも同じなら true だな
            return group.members?.Any(m => string.Compare(m.name, user.identityName, true) == 0) ?? false;
        }

        private static bool IsMemberOfAll(IEnumerable<PmGroup> groups, PmDirectoryEntityInfo user)
        {
            return groups.All(g => IsMemberOf(g, user));
        }

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

            var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);
            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);
            var wpType2 = DirectoryTypes2.Items.FilterByWildcards(d => d.Key, wpType).Select(d => d.Value).ConvertToWildcardPatternList();

            var drives = ResolvePmDrives(fakeBoundParameters);

            CacheExistingMemberIds(drives, wpGroupName);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var existingGroups = drive.PmGroups.Get();
                var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                var users = drive.SearchPmDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .FilterByWildcards(u => u?.objectType, wpType2)
                    .OrderBy(e => e.identityName))
                {
                    // updatingGroups に含まれるすべてのグループが、メンバーとして user を含んでいれば continue する
                    if (IsMemberOfAll(updatingGroups, user)) continue;

                    // もしローカルグループだったら continue する
                    if (user.objectType == "DirectoryGroup" && user.source == "local") continue;

                    // ちなみに、SearchPmDirectory() は 非機密アプリを返さないのでこれで OK

                    bFound = true;
                    string tiphelp = user.TipHelp();
                    yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{wordToComplete}')""");
            }
        }
    }

    private HashSet<(OrchDriveInfo drive, PmGroup group, string type, string userName)>? _csvLines;

    protected override void ProcessRecord()
    {
        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        UserName = UserName.Split1stValueByUnescapedCommas()?.ToArray();
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();

        // ユーザー名は case を無視して重複チェックする
        _csvLines ??= new(new ForthItemIgnoreCaseComparer<OrchDriveInfo, PmGroup, string>());

        var drives = SessionState.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList(); // Type はワイルドカードをサポートしない
        //var objectTypes = DirectoryTypes.Items.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

        // bulk でユーザー情報を問い合わせるには、ユーザーを Type でグループ化しなければ。
        // いや、ここでは CSV の情報を集約するだけで十分か。
        // TODO: 指定したワイルドカードが、どのグループ名にも合致しない場合には警告出した方が良さそうだけど、ちと面倒だね。。
        foreach (var drive in drives)
        {
            var groups = drive.PmGroups.Get();
            var filteredGroups = groups.FilterByWildcards(g => g?.name, wpGroupName).ToList();

            foreach (var group in filteredGroups)
            {
                foreach (var type in DirectoryTypes.Parameters.FilterByWildcards(v => v, wpType))
                {
                    foreach (var userName in UserName!)
                    {
                        _csvLines.Add((drive, group, type, userName));
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_csvLines is null) return;

        // 追加したい名前を一括してディレクトリに問い合わせ。
        // 種別ごとにまとめて、問い合わせる必要がある。
        // ロボットは一括して問い合わせできないので別途。
        foreach (var lines in _csvLines.GroupBy(g => (g.drive, g.type)))
        {
            var (drive, type) = lines.Key;

            Dictionary<string, PmGroupMember?>? entries = null;
            switch (type)
            {
                case "DirectoryUser":
                    entries = drive.PmBulkResolveByName("user", lines, line => line.userName);
                    break;
                case "DirectoryGroup":
                    entries = drive.PmBulkResolveByName("group", lines, line => line.userName);
                    break;
                case "DirectoryApplication":
                    entries = drive.PmBulkResolveByName("application", lines, line => line.userName);
                    break;
                case "DirectoryRobotUser":
                    foreach (var name in lines)
                    {
                        var addingMember = drive.SearchPmDirectory(name.userName)?
                            .Where(t => t.objectType == type)
                            .FirstOrDefault(t => string.Compare(t.identityName, name.userName, true) == 0);

                        // 当たらなかったら警告を表示
                        if (addingMember is null)
                        {
                            WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{name.userName}\" not found. Ignoring.");
                        }
                    }
                    break;
            }

            foreach (var entry in entries ?? [])
            {
                if (entry.Value is null)
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{entry.Key}\" not found. Ignoring.");
                }
                else if (entry.Value.objectType == "DirectoryGroup" && entry.Value.source == "local")
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{entry.Key}\" cannot be added because it is a local group. Ignoring.");
                }
            }
        }

        // ここから本番
        // メンバーをグループに追加していく。
        foreach (var drivesGroups in _csvLines.GroupBy(g => (g.drive, g.group)))
        {
            var (drive, group) = drivesGroups.Key;

            try
            {
                // 更新対象の各グループのメンバー一覧を取得
                var detailedGroup = drive.PmGroups.Get(group.id);
                if (detailedGroup is null) continue;

                HashSet<string> identifiers = [];
                PmGroupMember? entry = null;
                PmDirectoryEntityInfo? robotEntry = null;
                foreach (var (_, _, type, name) in drivesGroups)
                {
                    switch (type)
                    {
                        case "DirectoryRobotUser":
                            robotEntry = drive.SearchPmDirectory(name)?
                                .Where(t => string.Compare(t.objectType, "DirectoryRobotUser", true) == 0)
                                .FirstOrDefault(t => string.Compare(t.identityName, name, true) == 0);
                            if (robotEntry is not null)
                            {
                                if (group.members?.Any(m => m.identifier == robotEntry.identifier) ?? false)
                                {
                                    WriteWarning($"\"{group.GetPSPath()}\" already includes {type} \"{robotEntry.identityName}\".");
                                    continue;
                                }
                                if (ShouldProcess($"Item: {name} Destination: {group.GetPSPath()}", $"Add {type} to PmGroup"))
                                {
                                    identifiers.Add(robotEntry.identifier!);
                                }
                            }
                            continue;
                        case "DirectoryUser":
                            entry = drive.PmBulkResolveByName("user", [name], n => n).FirstOrDefault().Value;
                            break;
                        case "DirectoryGroup":
                            entry = drive.PmBulkResolveByName("group", [name], n => n).FirstOrDefault().Value;
                            if (entry?.source == "local") continue;
                            break;
                        case "DirectoryApplication":
                            entry = drive.PmBulkResolveByName("application", [name], n => n).FirstOrDefault().Value;
                            break;
                    }

                    if (entry is not null)
                    {
                        if (group.members?.Any(m => m.identifier == entry.identifier) ?? false)
                        {
                            WriteWarning($"\"{group.GetPSPath()}\" already includes {type} \"{entry.name}\".");
                            continue;
                        }
                        if (ShouldProcess($"Item: {name} Destination: {group.GetPSPath()}", $"Add {type} to PmGroup"))
                        {
                            identifiers.Add(entry.identifier!);
                            continue;
                        }
                    }
                }

                if (identifiers.Count > 0)
                {
                    // 既存のグループにメンバーを追加
                    var updatedGroup = drive.AddMemberToPmGroup(group.id, group.name, identifiers);
                    if (updatedGroup is not null)
                    {
                        WriteObject(updatedGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "UpdatePmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
