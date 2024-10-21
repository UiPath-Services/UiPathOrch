using System.Collections;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName_Type_UserName;
using System.Data;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchPmMemberToPmGroup", SupportsShouldProcess = true)]
    public class AddPmMemberToPmGroupCommand : OrchestratorPSCmdlet
    {
        // Key: (drive, pmGroup), Value: (name, displayName, objectType, identifier)
        private readonly Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<(string name, string displayName, string objectType, string identifier)>> _parameterSets = [];
        private HashSet<(OrchDriveInfo drive, string name)>? _warnedNames = null;

        // Key: SearchPmDirectoryUsers() で返ったエントリの objectType
        // Value: options in console
        private static readonly Dictionary<string, string> DirectoryTypes = new()
        {
            { "DirectoryUser",        "DirectoryUser" },
            { "DirectoryRobotUser",   "DirectoryRobot" },
            { "Application",          "DirectoryApplication" }
        };

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<Positional.GroupName_Type_UserName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TypeCompleter))]
        [SupportsWildcards]
        public string[]? Type { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_Type_UserName>))]
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

        // TODO: KeyOfDictionaryCompleter で書き直す。ValueOfDictionaryCompleter が必要かもしれない。。
        private class TypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var wpType = CreateWPListFromParameter(commandAst, "Type", Positional.GroupName_Type_UserName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var t in DirectoryTypes.Values
                    .Where(c => wp.IsMatch(c))
                    .ExcludeByWildcards(o => o, wpType))
                {
                    yield return new CompletionResult(t);
                }
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
                if (string.IsNullOrEmpty(wordToComplete))
                {
                    yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                    yield break;
                }

                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", Positional.GroupName_Type_UserName.Parameters);
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.GroupName_Type_UserName.Parameters, wordToComplete);
                var wpType = CreateWPListFromOtherParameters(commandAst, "Type", Positional.GroupName_Type_UserName.Parameters);
                var types = DirectoryTypes.FilterByWildcards(d => d.Value, wpType).Select(d => d.Key);

                var drives = ResolveDrives(fakeBoundParameters);

                GetExistingMemberIds(drives, wpGroupName);
                //var existingMemberIds = GetExistingMemberIds(drives, wpGroupName);

                // グループに追加済みのユーザーは非表示にする
                var paramName = GetParameterValues(commandAst, parameterName, Positional.GroupName_Type_UserName.Parameters, wordToComplete);

                bool bFound = false;
                foreach (var drive in drives)
                {
                    var existingGroups = drive.GetPmGroups().Values;
                    var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                    var users = drive.SearchPmDirectoryUsers(wordToComplete);
                    if (users == null) continue;

                    foreach (var user in users
                        .Where(u => types.Contains(u?.objectType ?? ""))
                        .ExcludeByWildcards(e => e?.identityName, wpUserName)
                        .OrderBy(e => e.identityName))
                    {
                        // updatingGroups に含まれるすべてのグループが、メンバーとして user を含んでいれば continue する
                        bool isMemberOfAllGroups = true;
                        foreach (var group in updatingGroups)
                        {
                            if (group!.members == null || !group.members.Any(m => m.identifier == user.identifier))
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

        // PmBulkResolveByName() /api/Directory/BulkResolveByName/{partitionGlobalId} を使った実装は複雑すぎるのでやめる！
        // ↑はロボットを検索できないし。
        // 全部、SearchPmDirectoryUsers() /api/Directory/Search/{partitionGlobalId} で検索すれば良いじゃない。
        protected override void ProcessRecord()
        {
            GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
            Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
            UserName = UserName.Split1stValueByUnescapedCommas()?.ToArray();
            Path = Path.Split1stValueByUnescapedCommas()?.ToArray();

            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpGroupName = GroupName.ConvertToWildcardPatternList();
            var wpType = Type.ConvertToWildcardPatternList();
            var objectTypes = DirectoryTypes.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

            // CSV インポートしたとき、あるグループに対して複数のメンバー追加を一度の API call で実現する
            // ドライブ名とグループ名はワイルドカードを展開して保持
            // Name はそのまま保持
            foreach (var drive in drives)
            {
                var existingGroups = drive.GetPmGroups().Values;
                var updatingGroups = existingGroups
                    .Where(g => !string.IsNullOrEmpty(g?.name))
                    .FilterByWildcards(g => g!.name!, wpGroupName);

                foreach (var groupSummary in updatingGroups.Where(g => g != null))
                {
                    var group = drive.GetPmGroup(groupSummary?.id);

                    if (!_parameterSets.TryGetValue((drive, group!), out var members))
                    {
                        members = [];
                        _parameterSets[(drive, group!)] = members;
                    }

                    foreach (var name in UserName!)
                    {
                        var addingMembers = drive.SearchPmDirectoryUsers(name)?
                            .Where(t => objectTypes.Contains(t.objectType))
                            .Where(t => string.Compare(t.identityName, name, StringComparison.OrdinalIgnoreCase) == 0);

                        // 一件も当たらなかったら警告を表示して次のユーザーへ
                        if (!(addingMembers?.Any() ?? false))
                        {
                            _warnedNames ??= [];
                            if (!_warnedNames.Add((drive, name))) continue;
                            WriteWarning($"\"{drive.NameColonSeparator}\": No match found for UserName '{name}'.");
                            continue;
                        }

                        // ユーザーもしくはアプリの場合に限って、BulkResolveByName() を呼び出す必要がある
                        foreach (var addingMember in addingMembers ?? [])
                        {
                            if (addingMember.objectType == "DirectoryUser" || addingMember.objectType == "Application")
                            {
                                string searchKey = addingMember.objectType == "DirectoryUser" ? "user" : "application";

                                var entries = drive.PmBulkResolveByName(searchKey, [addingMember.identityName!]);
                                var entry = entries.FirstOrDefault(e => e.name == name);
                                if (entry?.identifier != null)
                                {
                                    members.Add((entry.name, entry.displayName, DirectoryTypes[addingMember.objectType!], entry.identifier)!);
                                }
                                else
                                {
                                    WriteWarning($"\"{group!.name}\": No match found for UserName '{name}' ({addingMember.identityName}).");
                                    continue;
                                }
                            }
                            else
                            {
                                members.Add((addingMember.identityName, addingMember.displayName, DirectoryTypes[addingMember.objectType!], addingMember.identifier)!);
                            }
                        }
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            // 同じグループごとに、ShouldProcess を問い合わせた方が使いやすい気がする。
            // そのため、パラメータをグループごとに分類してから、ShouldProcess を処理する。

            foreach (var parameterSet in _parameterSets
                .OrderBy(p => p.Key.drive.Name)
                .ThenBy(p => p.Key.group.name))
            {
                var (drive, group) = parameterSet.Key;
                var directoryEntries = parameterSet.Value;

                string partitionGlobalId = drive.GetPartitionGlobalId();

                var existingMemberIds = group.members?.Select(m => m.identifier).ToList() ?? [];

                List<string> identifiers = [];

                // OrderBy しない方が、CSV 内の出現順を尊重できるか？ でも .NET のバージョンで実装が違うかもしれないし
                // グループごとに集約した時点で、元の順はあまり関係ないような気がする。OrderBy しておくか。
                foreach (var (name, displayName, objectType, identifier) in directoryEntries
                    .OrderBy(e => e.objectType)
                    .ThenBy(e => e.name))
                {
                    string target = System.IO.Path.Combine(drive.NameColonSeparator, name);
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        target += $" ({displayName})";
                    }
                    if (existingMemberIds.Contains(identifier))
                    {
                        WriteWarning($"\"{group.GetPSPath()}\": This group contains {target} already.");
                        continue;
                    }

                    if (ShouldProcess(target, $"Add {objectType!} to {group.name}"))
                    {
                        identifiers.Add(identifier!);
                    }
                }

                if (identifiers.Count > 0)
                {
                    // 既存のグループにメンバーを追加
                    UpdateGroupCommand updateGroupCommand = new()
                    {
                        partitionGlobalId = partitionGlobalId,
                        name = group!.name,
                        directoryUserIDsToAdd = identifiers.ToArray(),
                        directoryUserIDsToRemove = []
                    };

                    try
                    {
                        var newGroup = drive.OrchAPISession.PutPmGroup(group.id, updateGroupCommand);
                        if (newGroup != null)
                        {
                            newGroup.Path = drive.NameColonSeparator;
                            WriteObject(newGroup);
                            drive._dicPmGroups = null;
                            drive._dicPmGroups_Exception.ClearCache();
                            drive._dicPmDirectoryUsers = null;
                            drive._dicSearchForUsersAndGroups = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "UpdatePmGroupError", ErrorCategory.InvalidOperation, group));
                    }
                }
            }
        }
    }
}
