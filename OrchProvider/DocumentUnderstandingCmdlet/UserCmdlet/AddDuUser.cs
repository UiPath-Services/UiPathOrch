using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Commands.CsvHelper;
using TPositional = UiPath.PowerShell.Positional.Type_Name_Role;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "DuUser", SupportsShouldProcess = true)]
public class AddDuRoleToDuUserCommand : OrchestratorPSCmdlet
{
    Dictionary<((OrchDuDriveInfo drive, DuProject), string type, string name), CsvLine>? _csvLines = null;

    private class CsvLine(AddDuRoleToDuUserCommand cmdlet) : CsvLineBase
    {
        public HashSet<string> Roles { get; set; } = new(cmdlet.Roles ?? []);

        public void Update(AddDuRoleToDuUserCommand cmdl)
        {
            this.Roles.UnionWith(cmdl.Roles?.Where(r => !string.IsNullOrEmpty(r)) ?? []);
        }
    }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<DirectoryTypes>))]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmDirectoryNameCompleter4Du<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleCompleter))]
    [SupportsWildcards]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    // AddFolderUser.cs の同名のクラスと、実装がほぼ同一だ。
    //private class UserNameCompleter4Du : OrchArgumentCompleter
    //{
    //    //  0: User, 1: Group, 2: Machine, 3: Robot, 4: ExternalApplication

    //    public override IEnumerable<CompletionResult> CompleteArgument(
    //        string commandName,
    //        string parameterName,
    //        string wordToComplete,
    //        CommandAst commandAst,
    //        IDictionary fakeBoundParameters)
    //    {
    //        if (string.IsNullOrEmpty(wordToComplete))
    //        {
    //            yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
    //            yield break;
    //        }

    //        var paramType = GetParameterValue(commandAst, "Type", TPositional.Parameters);
    //        if (!DirectoryTypeItems.Items.TryGetValue(paramType ?? "", out var objectType))
    //        {
    //            //yield return new CompletionResult(PathTools.EscapePSText("Invalid Type."));
    //            yield break;
    //        }

    //        var drives = ResolveDuDrives(fakeBoundParameters);

    //        // フォルダに割り当て済みのユーザーを候補から除外する処理は、いったん実装せずとした
    //        //var existingMemberIds = GetExistingMemberIds(drives, wpName);
    //        // アサイン済みのユーザーを除外するため、アサイン済みのユーザーを取得
    //        //ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, false));

    //        var paramUserName = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);
    //        bool bFound = false;
    //        foreach (var drive in drives)
    //        {
    //            string partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
    //            var ret = drive.ParentDrive.SearchDirectory(wordToComplete);
    //            if (ret is null) continue;

    //            foreach (var e in ret
    //                .Where(e => e.type == objectType)
    //                //.ExcludeByTexts(e => e.identityName!, assignedUsers?.Select(u => u.Id.ToString()!) ?? [])
    //                .ExcludeByClassValues(e => e?.identityName, paramUserName)
    //                .OrderBy(e => e.identityName))
    //            {
    //                bFound = true;
    //                string tiphelp = e.identityName;
    //                yield return new CompletionResult(PathTools.EscapePSText(e?.identityName), e?.identityName, CompletionResultType.Text, tiphelp);
    //            }
    //        }
    //        if (!bFound)
    //        {
    //            yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
    //        }
    //    }
    //}

    // この RoleCompleter は、ユーザーにアサインされていないロールだけを列挙する
    private class RoleCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            //var drives = ResolveDuDrives(fakeBoundParameters);
            var drivesProjects = ResolveDuPath(commandAst, fakeBoundParameters);

            // この名前のユーザーにアサイン済みの Role は除外する
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // パラメータで選択済みの Role は除外する
            var wpRole = CreateWPListFromParameter(commandAst, "Role", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuRoles());

            foreach (var result in results)
            {
                var (drive, project) = result.Source;

                var users = drive.GetDuUsers(project)
                    .FilterByWildcards(u => u?.displayName, wpName).ToList();

                foreach (var role in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpRole)
                    .OrderBy(e => e?.name))
                {
                    // 対象のすべてのユーザーについて、このロールがアサイン済みであれば表示しない
                    // これだと、Inherited のロールも表示しないけど、いいか。
                    if (users.Count != 0 && users.All(u => u.roleAssignmentDtos?.Select(r => r.roleId).Contains(role.id) ?? false)) continue;

                    string tiphelp = role.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(role.name), role.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _csvLines ??= new Dictionary<((OrchDuDriveInfo drive, DuProject project), string type, string name), CsvLine>(new SecondAndThirdItemIgnoreCaseComparer<OrchDuDriveInfo, DuProject>());

        // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();
        Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        Name = Name.Split1stValueByUnescapedCommas()?.ToArray();
        Roles = Roles.Split1stValueByUnescapedCommas()?.ToArray();

        var projects = OrchDuDriveInfo.EnumFolders(Path);
        var wpEntityType = Type.ConvertToWildcardPatternList();
        var specifiedTypes = DirectoryTypes.Parameters.SelectByWildcards(t => t, wpEntityType);

        foreach (var project in projects)
        {
            foreach (var specifiedType in specifiedTypes)
            {
                foreach (var name in Name!)
                {
                    if (!_csvLines.TryGetValue((project, specifiedType, name), out var line))
                    {
                        _csvLines[(project, specifiedType, name)] = new CsvLine(this);
                    }
                    else
                    {
                        line.Update(this);
                    }
                }
            }
        }
    }

    private static void AddUserToPayload(UserRoleAssignmentsCmd payload, DuProject project, string tenantKey, string userId, int securityPrincipalType, IEnumerable<DuRole> roles)
    {
        foreach (var role in roles)
        {
            DuRoleAssignment assign = new()
            {
                roleId = role.id,
                scope = $"/tenant/{tenantKey}/DocumentUnderstanding/projects/{project.id}",
                securityPrincipalId = userId,
                securityPrincipalType = securityPrincipalType
            };
            payload.roleAssignmentsToAdd!.Add(assign);
        }
    }

    protected override void EndProcessing()
    {
        // ここの実装は Add-OrchPmGroupMember cmdlet と良く似ているので、適宜参照してほしい。

        if (_csvLines is null) return;

        // 追加したい名前を一括してディレクトリに問い合わせ。
        // 種別ごとにまとめて、問い合わせる必要がある。
        // ロボットは一括して問い合わせできないので別途。
        // ここ、ほぼ Add-OrchPmGroupMember の実装と同一だな。。実装を共有できると良いけど、

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var lines in _csvLines.GroupBy(line => (line.Key.Item1.drive, line.Key.type)))
        {
            var (drive, type) = lines.Key;

            Dictionary<string, PmGroupMember?>? entries = null;
            switch (type)
            {
                case "DirectoryUser":
                    entries = drive.ParentDrive.PmBulkResolveByName("user", lines, line => line.Key.name);
                    break;
                case "DirectoryGroup":
                    entries = drive.ParentDrive.PmBulkResolveByName("group", lines, line => line.Key.name);
                    break;
                case "DirectoryApplication":
                    entries = drive.ParentDrive.PmBulkResolveByName("application", lines, line => line.Key.name);
                    break;
                case "DirectoryRobotUser":
                    foreach (var line in lines)
                    {
                        var addingMember = drive.ParentDrive.SearchPmDirectory(line.Key.name)?
                            .Where(t => t.objectType == type)
                            .FirstOrDefault(t => string.Compare(t.identityName, line.Key.name, true) == 0);

                        // 当たらなかったら警告を表示
                        if (addingMember is null)
                        {
                            WriteWarning($"\"{drive.NameColonSeparator}\": \"{line.Key.name}\" ({type}) not found. Ignoring.");
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
            }
        }

        // ここから本番
        // メンバーをプロジェクトに追加していく。
        // ドライブでグループ化し、このプロジェクトに追加するユーザー一覧をすべてペイロードに集約する
        UserRoleAssignmentsCmd payload = new()
        {
            roleAssignmentsToAdd = [],
            roleAssignmentsToDelete = []
        };

        HashSet<DuProject> updatedProjects = []; // 最後に、キャッシュをクリアするのに使う
        foreach (var lines in _csvLines.GroupBy(line => line.Key.Item1.drive))
        {

            var drive = lines.Key;
            var (_, tenantKey) = drive.ParentDrive.GetTenantId();

            foreach (var line in lines)
            {
                var project = line.Key.Item1.Item2;
                var type = line.Key.type;
                var name = line.Key.name;
                var wpRoles = line.Value.Roles.ConvertToWildcardPatternList();

                var rolesAvailable = drive.GetDuRoles();
                if (rolesAvailable is null || rolesAvailable.Length == 0) continue;

                PmGroupMember? entry = null;
                switch (type)
                {
                    case "DirectoryRobotUser":
                        var robotEntry = drive.ParentDrive.SearchPmDirectory(name)?
                            .Where(t => string.Compare(t.objectType, "DirectoryRobotUser", true) == 0)
                            .FirstOrDefault(t => string.Compare(t.identityName, name, true) == 0);
                        if (robotEntry is null) continue;
                        if (ShouldProcess($"Robot: {robotEntry.identityName} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, robotEntry.identifier!, 0,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryUser":
                        entry = drive.ParentDrive.PmBulkResolveByName("user", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"User: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 0,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryGroup":
                        entry = drive.ParentDrive.PmBulkResolveByName("group", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"Group: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 1,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryApplication":
                        entry = drive.ParentDrive.PmBulkResolveByName("application", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"Application: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 2,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                }
            }

            if (payload.roleAssignmentsToAdd.Count == 0) continue;

            var projects = drive.GetDuProjects();

            try
            {
                var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();

                drive.ParentDrive.OrchAPISession.SetDuRoleToDuUser(partitionGlobalId, payload);

                foreach (var project in updatedProjects)
                {
                    drive._dicDuUsers?.TryRemove((partitionGlobalId, tenantKey, project.id)!, out _);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "AddDuRoleToDuUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
