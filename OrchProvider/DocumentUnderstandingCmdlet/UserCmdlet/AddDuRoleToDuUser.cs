using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Role;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "DuRoleToDuUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.DuUser))]
class AddDuRoleToDuUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
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

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

            // パラメータで選択済みの DocumentTypeName は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesProjects, dp => {
                var (drive, project) = dp;
                var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                var (_, tenantKey) = drive.ParentDrive.GetTenantId();
                return drive.GetDuUsers(partitionGlobalId, tenantKey, project);
            });

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var user in entities!
                    .Where(e => wp.IsMatch(e?.displayName))
                    .ExcludeByWildcards(e => e?.displayName!, wpName)
                    .OrderBy(e => e?.displayName))
                {
                    string tiphelp = user.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(user.displayName), user.displayName, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    // TODO: Get-DuRole の completer と共通化する
    private class RoleCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveDuDrives(fakeBoundParameters);

            // パラメータで選択済みの DocumentTypeName は、候補から除外する
            var wpRole = CreateWPListFromParameter(commandAst, "Role", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drives, drive => drive.GetDuRoles());

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var role in entities!
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpRole)
                    .OrderBy(e => e?.name))
                {
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
        var wpRole = Role.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp =>
            {
                var (drive, project) = dp;
                var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                var (_, tenantKey) = drive.ParentDrive.GetTenantId();
                return drive.GetDuUsers(partitionGlobalId, tenantKey, project);
            });

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
                        rolesToAdd = targetRoles
                            .Where(ar => !existingRoles.Any(er => string.Compare(er.roleName, ar.name, true) == 0));
                    }

                    if (!rolesToAdd.Any()) continue;

                    if (ShouldProcess(user.GetPSPath(), "Add DuRoleToDuUser"))
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
