using System.Collections;
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
    //private const string UserNameSet = "UserNameSet";
    //private const string UserSet = "UserSet";

    // Feature request from Mishima-san (KDDI): allow specifying User Principal Name in Add-DuUser.
    // The code below would be needed for that, but I can't think of a good implementation.
    // Either sacrifice performance, or add complex parameters..
    // Personally, neither option is acceptable..
    //[Parameter(ParameterSetName = UserNameSet, Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(DuUserNameCompleter))]
    //[SupportsWildcards]
    //public string[]? UserName { get; set; }

    // Ideally, we would not list users who have no non-inherited roles.
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DuNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleCompleter))]
    [SupportsWildcards]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    // This RoleCompleter only enumerates roles that are assigned to the user
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

            // Filter to roles assigned to users with this name
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // Filter to roles assigned to users with this name
            //var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

            // Exclude already-selected Role values from completion candidates
            var wpRole = CreateWPListFromParameter(commandAst, "Role", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

            foreach (var result in results)
            {
                var (drive, project) = result.Source;
                foreach (var user in result
                    .FilterByWildcards(u => u?.Name, wpName)
                    //.FilterByWildcards(u => u?.UserName, wpUserName)
                    .OrderBy(u => u.Name))
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
        var drivesProjects = SessionState.EnumDuFolders(Path, Recurse);
        var wpName = Name.ConvertToWildcardPatternList();
        //var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpRole = Roles.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

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
                    .FilterByWildcards(u => u?.Name, wpName)
                    //.FilterByWildcards(u => u?.UserName, wpUserName)
                    .OrderBy(u => u.Name))
                {
                    var existingRoles = user.roleAssignmentDtos;
                    if (existingRoles is null || existingRoles.Length == 0) continue;

                    var rolesToRemove = existingRoles
                        .Where(r => !r.inherited.GetValueOrDefault())
                        .FilterByWildcards(r => r?.roleName, wpRole);
                    if (!rolesToRemove.Any()) continue;

                    string target = $"{user.type}: '{user.GetPSPath()}' Roles: {string.Join(", ", rolesToRemove.Select(r => $"'{r.roleName}'"))}";

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
                                    .Select(r => r.id!).ToList()
                            };

                            drive.OrchAPISession.SetDuRoleToDuUser(partitionGlobalId, payload);
                            drive._dicDuUsers?.TryRemove((partitionGlobalId, tenantKey, project.id)!, out _);
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
