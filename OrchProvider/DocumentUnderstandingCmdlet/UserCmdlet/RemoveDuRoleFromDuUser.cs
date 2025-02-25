using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Role;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "DuRoleFromDuUser", SupportsShouldProcess = true)]
public class RemoveDuRoleFromDuUserCommand : OrchestratorPSCmdlet
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

    // この RoleCompleter は、ユーザーにアサインされているロールだけを列挙する
    private class RoleCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesProjects = ResolveDuPath(commandAst, fakeBoundParameters);

            // この名前のユーザーにアサイン済みの Role は除外する
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // パラメータで選択済みの Role は除外する
            var wpRole = CreateWPListFromParameter(commandAst, "Role", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var users)) continue;
                if (users is null) continue;

                var (drive, project) = result.Source;

                foreach (var user in users
                    .FilterByWildcards(u => u?.displayName, wpName)
                    .OrderBy(u => u.displayName))
                {
                    foreach (var role in user.roleAssignmentDtos?
                        .Where(r => !string.IsNullOrEmpty(r.roleName) && !r.inherited.GetValueOrDefault())
                        .Where(r => wp.IsMatch(r.roleName))
                        .ExcludeByWildcards(r => r?.roleName, wpRole)
                        .OrderBy(r => r?.roleName)!)
                    {
                        string tiphelp = System.IO.Path.Combine(project.GetPSPath(), role.roleName!);
                        yield return new CompletionResult(PathTools.EscapePSText(role.roleName), role.roleName, CompletionResultType.Text, tiphelp);
                    }
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
                    var existingRoles = user.roleAssignmentDtos;
                    if (existingRoles is null || existingRoles.Length == 0) continue;

                    var rolesToRemove = existingRoles
                        .Where(r => !r.inherited.GetValueOrDefault())
                        .FilterByWildcards(r => r?.roleName, wpRole);
                    if (!rolesToRemove.Any()) continue;

                    string target = $"User: '{user.GetPSPath()}' Roles: {string.Join(", ", rolesToRemove.Select(r => $"'{r.roleName}'"))}";

                    if (ShouldProcess(target, "Remove DuRoleFromDuUser"))
                    {
                        try
                        {
                            var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                            var (_, tenantKey) = drive.ParentDrive.GetTenantId();

                            UserRoleAssignmentsCmd payload = new()
                            {
                                roleAssignmentsToAdd = [],
                                roleAssignmentsToDelete = rolesToRemove
                                    .Where(r => r.id is not null)
                                    .Select(r => r.id!.Value).ToList()
                            };

                            drive.OrchAPISession.SetDuRoleToDuUser(partitionGlobalId, tenantKey, project.id, payload);
                            drive._dicDuUsers?.Remove((partitionGlobalId, tenantKey, project.id)!);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "RemoveDuRoleFromDuUserError", ErrorCategory.InvalidOperation, user));
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
