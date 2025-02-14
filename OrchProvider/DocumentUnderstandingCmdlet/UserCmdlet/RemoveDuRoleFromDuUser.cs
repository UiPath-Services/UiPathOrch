using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_Role;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "DuRoleFromDuUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.DuUser))]
class RemoveDuRoleFromDuUserCommand : OrchestratorPSCmdlet
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
                    if (ShouldProcess(user.GetPSPath(), "Remove DuRoleFromDuUser"))
                    {
                        try
                        {
                            var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                            var (_, tenantKey) = drive.ParentDrive.GetTenantId();

                            // not implemented yet
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
