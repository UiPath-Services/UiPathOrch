using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Role;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "DuRoleToDuUser", SupportsShouldProcess = true)]
public class AddDuRoleToDuUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DuUserNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleCompleter))]
    [SupportsWildcards]
    public string[]? Role { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

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

            var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuRoles());

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var roles)) continue;
                if (roles is null) continue;

                var (drive, project) = result.Source;

                var users = drive.GetDuUsers(project)
                    .FilterByWildcards(u => u?.displayName, wpName).ToList();

                foreach (var role in roles
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpRole)
                    .OrderBy(e => e?.name))
                {
                    // 対象のすべてのユーザーについて、このロールがアサイン済みであれば表示しない
                    // これだと、Inherited のロールも表示しないけど、いいか。
                    if (users.All(u => u.roleAssignmentDtos?.Select(r => r.roleId).Contains(role.id) ?? false)) continue;

                    string tiphelp = role.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(role.name), role.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpRole = Role.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.GetDuUsers(dp.project));
 
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var (drive, project) = result.Source;

                foreach (var user in entities
                    .FilterByWildcards(u => u?.displayName, wpName)
                    .OrderBy(u => u.displayName))
                {
                    var availableRoles = drive.GetDuRoles();
                    if (availableRoles is null) continue;

                    var targetRoles = availableRoles.FilterByWildcards(r => r?.name, wpRole);

                    var existingRoles = user.roleAssignmentDtos;

                    IEnumerable<DuRole> rolesToAdd;
                    if (existingRoles is null)
                    {
                        rolesToAdd = targetRoles;
                    }
                    else
                    {
                        rolesToAdd = targetRoles.Where(tr => existingRoles.All(er => er.roleId != tr.id)).ToList();
                    }

                    if (!rolesToAdd.Any()) continue;

                    string target = $"User: '{user.GetPSPath()}' Roles: {string.Join(", ", rolesToAdd.Select(r => $"'{r.name}'"))}";

                    if (ShouldProcess(target, "Add DuRoleToDuUser"))
                    {
                        try
                        {
                            var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                            var (_, tenantKey) = drive.ParentDrive.GetTenantId();

                            UserRoleAssignmentsCmd payload = new()
                            {
                                roleAssignmentsToAdd = [],
                                roleAssignmentsToDelete = []
                            };

                            foreach (var role in rolesToAdd)
                            {
                                DuRoleAssignment assign = new()
                                {
                                    roleId = role.id,
                                    scope = $"/tenant/{tenantKey}/DocumentUnderstanding/projects/{project.id}",
                                    securityPrincipalId = user?.securityPrincipalId,
                                    securityPrincipalType = 1
                                };
                                payload.roleAssignmentsToAdd.Add(assign);
                            }

                            drive.OrchAPISession.SetDuRoleToDuUser(partitionGlobalId, tenantKey, project.id, payload);
                            drive._dicDuUsers?.Remove((partitionGlobalId, tenantKey, project.id)!);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "AddDuRoleToDuUserError", ErrorCategory.InvalidOperation, user));
                        }
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
