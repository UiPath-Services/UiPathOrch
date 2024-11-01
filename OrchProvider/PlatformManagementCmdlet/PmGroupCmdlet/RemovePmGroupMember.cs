using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName_Type_UserName;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchPmGroupMember", SupportsShouldProcess = true)]
    //[OutputType(typeof(Entities.IdGroup))]
    public class RemovePmMemberFromPmGroupCommand : OrchestratorPSCmdlet
    {
        // Key: (drive, group), Value: Members
        private Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<Member>>? _parameterSets = null;

        // CSV で重複した行を除去するために使う
        private Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<Member>>? _visitedUsersHash = null;

        // CSV で指定したユーザー名ワイルドカードの重複を除去するために使う
        private HashSet<(OrchDriveInfo drive, PmGroup group, string type, string userName)>? _visitedUserPatterns = null;

        private class DirectoryType(int type, string objectKind)
        {
            public static readonly Dictionary<string, DirectoryType> All = new()
            {
                { "DirectoryUser",        new DirectoryType(0, "user") },
                { "DirectoryRobotUser",   new DirectoryType(3, "robot") },
                { "DirectoryApplication", new DirectoryType(4, "application") },
            };

            public int Type { get; } = type;
            public string ObjectKind { get; } = objectKind;
        }

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<Positional.GroupName_Type_UserName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TypeCompleter))]
        [SupportsWildcards]
        public string[]? Type { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter]
        public SwitchParameter WarnOnNoMatch { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_Type_UserName>))]
        public string[]? Path { get; set; }

        private static List<Member> GetExistingMembers(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
        {
            var results = ParallelResults.ForEach(drives, drive =>
            {
                var groups = drive.GetPmGroups().Values
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name);
                return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
            });

            List<Member> existingMembers = [];
            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var group in entities!)
                {
                    if (!group.TryGetValue(out var detailedGroup)) continue;

                    existingMembers.AddRange(detailedGroup?.members ?? []);
                }
            }
            return existingMembers;
        }

        private class TypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの UserName は、候補から除外する
                var wpType = CreateWPListFromParameter(commandAst, "Type", Positional.GroupName_Type_UserName.Parameters, wordToComplete);
                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.GroupName_Type_UserName.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // 指定されたグループの既存のメンバーを取得する
                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", Positional.GroupName_Type_UserName.Parameters);
                var existingMembers = GetExistingMembers(drives, wpGroupName);

                foreach (var member in existingMembers
                    .Where(e => wp.IsMatch(e?.name))
                    .FilterByWildcards(m => m?.name, wpUserName)
                    .ExcludeByWildcards(m => m?.objectType, wpType)
                    .OrderBy(m => m.objectType))
                {
                    //string tiphelp = TipHelp(member);
                    yield return new CompletionResult(PathTools.EscapePSText(member.objectType), member.objectType, CompletionResultType.Text, member.objectType);
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
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの UserName は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.GroupName_Type_UserName.Parameters, wordToComplete);
                var wpType = CreateWPListFromOtherParameters(commandAst, "Type", Positional.GroupName_Type_UserName.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // 指定されたグループの既存のメンバーを取得する
                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", Positional.GroupName_Type_UserName.Parameters);
                var existingMemberIds = GetExistingMembers(drives, wpGroupName);

                // 各グループの詳細を取得する
                var results = ParallelResults.ForEach(drives, drive =>
                {
                    var groups = drive.GetPmGroups().Values
                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name);
                    return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
                });

                // グループのメンバーとなっている DirectoryUser を収集する
                List<Member> users = [];
                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!)
                    {
                        if (!e.TryGetValue(out var detailedGroup)) continue;

                        foreach (var member in detailedGroup?.members?
                            .FilterByWildcards(m => m?.objectType, wpType) ?? [])
                        {
                            users.Add(member);
                        }
                    }
                }

                // 条件に合致する DirectoryUser を候補として表示する
                foreach (var user in users
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpUserName)
                    .OrderBy(e => e?.name))
                {
                    string tiphelp = TipHelp(user);
                    yield return new CompletionResult(PathTools.EscapePSText(user.name), user.name, CompletionResultType.Text, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            // CSV に指定された GroupName はカンマで区切る
            var groupName = GroupName!
                .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(g => g.Trim())
                .ToArray();
            var wpGroupName = groupName.ConvertToWildcardPatternList();

            // CSV に指定された Type はカンマで区切る
            var type = Type!
                 .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                 .Select(t => t.Trim())
                 .ToArray();
            var wpType = type.ConvertToWildcardPatternList();

            var wpUserName = UserName.ConvertToWildcardPatternList();

            // 指定されたパラメータを保持する
            // ドライブ、グループ、ユーザーは展開する
            foreach (var drive in drives)
            {
                var existingGroups = drive.GetPmGroups();

                var targetGroups = existingGroups.Values
                    .Where(g => g != null)
                    .FilterByWildcards(g => g!.name ?? "", wpGroupName);

                if (WarnOnNoMatch.IsPresent && !(targetGroups?.Any() ?? false))
                {
                    WriteWarning($"No match found for GroupName '{drive.NameColonSeparator}{GroupName![0]}'.");
                    continue;
                }

                foreach (var group in targetGroups)
                {
                    // このグループのメンバを取得
                    var detailedGroup = drive.GetPmGroup(group?.id);

                    var targetMembers = detailedGroup?.members?
                        .FilterByWildcards(m => m?.objectType, wpType)
                        .FilterByWildcards(m => m?.name, wpUserName);

                    if (WarnOnNoMatch.IsPresent && !(targetMembers?.Any() ?? false))
                    {
                        _visitedUserPatterns ??= [];
                        // ちょっと雑だけど、CSV を処理する場合は配列にひとつの要素しかないのでこれで十分か。
                        if (!_visitedUserPatterns.Add((drive, group!, Type![0], UserName![0])))
                            continue;

                        WriteWarning($"No match found for UserName '{UserName![0]}' ({Type![0]}) in GroupName '{group?.GetPSPath()}'.");

                        continue;
                    }
                    if (targetMembers == null) continue; // !WarnOnNoMatch.IsPresent の場合を考慮

                    foreach (var member in targetMembers)
                    {
                        _visitedUsersHash ??= [];
                        if (!_visitedUsersHash.TryGetValue((drive, detailedGroup!), out var visitedUsers))
                        {
                            visitedUsers = [];
                            _visitedUsersHash[(drive, detailedGroup!)] = visitedUsers;
                        }

                        if (!visitedUsers!.Add(member))
                        {
                            // 処理済みなのでスキップする
                            continue;
                        }

                        string target = $"{member.name} ({member.displayName}) from {group?.GetPSPath()}";
                        if (ShouldProcess(target, $"Remove Member From Group"))
                        {
                            _parameterSets ??= [];
                            if (!_parameterSets.TryGetValue((drive, detailedGroup!), out var membersToRemove))
                            {
                                membersToRemove = [];
                                _parameterSets[(drive, detailedGroup!)] = membersToRemove;
                            }
                            membersToRemove!.Add(member);
                        }
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            if (_parameterSets == null) return;

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var param in _parameterSets
                .OrderBy(p => p.Key.drive.Name)
                .ThenBy(p => p.Key.group.name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                var (drive, group) = param.Key;
                var toBeRemoved = param.Value;

                if (toBeRemoved.Count == 0) continue;

                string partitionGlobalId = drive.GetPartitionGlobalId();

                try
                {
                    UpdateGroupCommand updateGroupCommand = new()
                    {
                        partitionGlobalId = partitionGlobalId,
                        name = group!.name,
                        directoryUserIDsToAdd = [],
                        directoryUserIDsToRemove = toBeRemoved.Select(m => m.identifier).ToArray()!
                    };

                    drive.OrchAPISession.PutPmGroup(group.id, updateGroupCommand);
                    drive._dicPmGroups = null;
                    drive._dicPmGroups_Exception.ClearCache();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "PutIdGroupError", ErrorCategory.InvalidOperation, group));
                }
            }
        }
    }
}
