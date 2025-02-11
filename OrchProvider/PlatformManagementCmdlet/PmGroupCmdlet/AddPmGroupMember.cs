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

[Cmdlet(VerbsCommon.Add, "OrchPmGroupMember", SupportsShouldProcess = true)]
public class AddPmGroupMemberCommand : OrchestratorPSCmdlet
{
    // Key: (drive, pmGroup), Value: (name, displayName, objectType, identifier)
    //private readonly Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<(string name, string displayName, string objectType, string identifier)>> _parameterSets = [];
    //private HashSet<(OrchDriveInfo drive, string name)>? _warnedNames = null;

    // パラメータを、Path と PmGroup だけ展開して保持
    private List<(OrchDriveInfo Drive, PmGroup Group, string Type, string UserName)>? _csvLines;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ValueOfDictionaryCompleter<DirectoryTypes>))]
    [SupportsWildcards]
    public string[]? Type { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private static List<string> GetExistingMemberIds(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
    {
        var results = ParallelResults.ForEach(drives, drive =>
        {
            var groups = drive.GetPmGroups().Values
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
        });

        List<string> existingMemberIds = [];
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var group in entities!)
            {
                if (!group.TryGetValue(out var detailedGroup)) continue;

                existingMemberIds.AddRange(detailedGroup!.members?.Select(m => m.identifier!) ?? []);
            }
        }
        return existingMemberIds;
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
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);
            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);
            //var types = DirectoryTypes.Items.FilterByWildcards(d => d.Value, wpType).Select(d => d.Key);

            var drives = ResolveDrives(fakeBoundParameters);

            GetExistingMemberIds(drives, wpGroupName);
            //var existingMemberIds = GetExistingMemberIds(drives, wpGroupName);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var existingGroups = drive.GetPmGroups().Values;
                var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                var users = drive.SearchPmDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    //.Where(u => types.Contains(u?.objectType ?? ""))
                    .FilterByWildcards(u => u?.objectType, wpType)
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .Where(u => u?.objectType == "DirectoryUser") // && u?.source != "local")
                    .OrderBy(e => e.identityName))
                {
                    // updatingGroups に含まれるすべてのグループが、メンバーとして user を含んでいれば continue する
                    bool isMemberOfAllGroups = true;
                    foreach (var group in updatingGroups)
                    {
                        if (group!.members is null || !group.members.Any(m => m.identifier == user.identifier))
                        {
                            isMemberOfAllGroups = false;
                            break;
                        }
                    }
                    if (isMemberOfAllGroups) continue;

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
        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        UserName = UserName.Split1stValueByUnescapedCommas()?.ToArray();
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();

        _csvLines ??= [];

        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();
        var objectTypes = DirectoryTypes.Items.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

        // bulk でユーザー情報を問い合わせるには、ユーザーを Type でグループ化しなければ。
        // いや、ここでは CSV の情報を集約するだけで十分か。
        // TODO: 指定したワイルドカードが、どのグループ名にも合致しない場合には警告出した方が良さそうだけど、ちと面倒だね。。
        foreach (var drive in drives)
        {
            var groups = drive.GetPmGroups();
            foreach (var group in groups.Values.FilterByWildcards(g => g?.name, wpGroupName))
            {
                foreach (var type in DirectoryTypes.Items.Values.FilterByWildcards(v => v, wpType))
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

        // ユーザー名の重複を除外しておく。
        // 違うグループに同じユーザーを追加することもあるから、ユーザー名の重複はこの cmdlet の正しい使用の範囲内だ。
        // この cmdlet においては、ユーザー名はワイルドカードをサポートしない（できない）ことに注意。
        // ユーザー名の比較は case を無視する。
        _csvLines = _csvLines
            .GroupBy(line => (line.Drive, line.Type, line.UserName), new ThirdItemIgnoreCaseComparer<OrchDriveInfo, string>())
            .Select(group => group.First())
            .ToList();

        HashSet<string> ignoreList = [];
        // 追加したい名前を一括してディレクトリに問い合わせ。
        // 種別ごとにまとめて、問い合わせる必要がある。
        // ロボットは一括して問い合わせできないので別途。
        foreach (var drive_type_userNames in _csvLines
            .Select(csvLine => (csvLine.Drive, csvLine.Type, csvLine.UserName))
            .Distinct()
            .GroupBy(g => (g.Drive, g.Type)))
        {
            var (drive, type) = drive_type_userNames.Key;

            Dictionary<string, PmGroupMember?>? entries = null;
            switch (type)
            {
                case "DirectoryUser":
                    entries = drive.PmBulkResolveByName("user", drive_type_userNames, m => m.UserName);
                    break;
                case "DirectoryGroup":
                    entries = drive.PmBulkResolveByName("group", drive_type_userNames, m => m.UserName);
                    break;
                case "DirectoryApplication":
                    entries = drive.PmBulkResolveByName("application", drive_type_userNames, m => m.UserName);
                    break;
                case "DirectoryRobot":
                    foreach (var name in drive_type_userNames)
                    {
                        var addingMember = drive.SearchPmDirectory(name.UserName)?
                            .Where(t => t.objectType == "DirectoryRobot")
                            .FirstOrDefault(t => string.Compare(t.identityName, name.UserName, true) == 0);

                        // 当たらなかったら警告を表示
                        if (addingMember is null)
                        {
                            WriteWarning($"\"{drive.NameColonSeparator}\": \"{name.UserName}\" ({type}) not found. Ignoring.");
                        }
                    }
                    break;
            }

            foreach (var entry in entries ?? [])
            {
                if (entry.Value is null)
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": \"{entry.Key}\" ({type}) not found. Ignoring.");
                }
                else if (entry.Value.objectType == "DirectoryGroup" && entry.Value.source == "local")
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": \"{entry.Key}\" ({type}) cannot be added because it is a local group. Ignoring.");
                }
            }
        }

        // ここから本番
        // メンバーをグループに追加していく。
        foreach (var drivesGroups in _csvLines.GroupBy(g => (g.Drive, g.Group)))
        {
            var (drive, group) = drivesGroups.Key;

            try
            {

                var partitionGlobalId = drive.GetPartitionGlobalId();

                // 更新対象の各グループのメンバー一覧を取得
                var detailedGroup = drive.GetPmGroup(group.id);
                if (detailedGroup is null) continue;

                HashSet<string> identifiers = [];
                PmGroupMember? entry = null;
                PmDirectoryEntityInfo? robotEntry = null;
                foreach (var (_, _, type, name) in drivesGroups)
                {
                    switch (type)
                    {
                        case "DirectoryRobot":
                            robotEntry = drive.SearchPmDirectory(name)?
                                .Where(t => t.objectType == "DirectoryRobot")
                                .FirstOrDefault(t => string.Compare(t.identityName, name, true) == 0);
                            if (robotEntry is not null)
                            {
                                if (group.members?.Any(m => m.identifier == robotEntry.identifier) ?? false)
                                {
                                    WriteWarning($"\"{group.GetPSPath()}\" already includes \"{robotEntry.identityName}\" ({type}).");
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
                            WriteWarning($"\"{group.GetPSPath()}\" already includes \"{entry.name}\" ({type}).");
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
                    UpdateGroupCommand updateGroupCommand = new()
                    {
                        partitionGlobalId = partitionGlobalId,
                        name = group!.name,
                        directoryUserIDsToAdd = identifiers.ToList(),
                        directoryUserIDsToRemove = []
                    };

                    var newGroup = drive.OrchAPISession.PutPmGroup(group.id, updateGroupCommand);
                    if (newGroup is not null)
                    {
                        newGroup.Path = drive.NameColonSeparator;
                        WriteObject(newGroup);
                        drive._dicPmGroups = null;
                        drive._dicPmGroups_Exception.ClearCache();
                        drive._dicSearchPmDirectory = null;
                        drive._dicSearchDirectory = null;
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
