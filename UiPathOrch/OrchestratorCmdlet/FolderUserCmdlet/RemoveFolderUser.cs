using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchFolderUser", SupportsShouldProcess = true)]
public class RemoveFolderUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
            var depth = ResolveDepth(fakeBoundParameters);

            // Extract path from parameter. If not specified, target the current directory.
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // Exclude UserNames already selected via parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);

            // Only include FullNames selected via parameter
            var wpFullName = GetFakeBoundParameters(fakeBoundParameters, "FullName").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(e => wp.IsMatch(e.UserEntity!.UserName))
                    .ExcludeByWildcards(e => e?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(e => e?.UserEntity?.FullName, wpFullName)
                    .OrderBy(e => e.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class FullNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
            var depth = ResolveDepth(fakeBoundParameters);

            // Extract path from parameter. If not specified, target the current directory.
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // Only include UserNames selected via parameter
            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();

            // Exclude FullNames already selected via parameter from the candidates
            var wpFullName = CreateSelfExclusionList(commandAst, "FullName", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(e => wp.IsMatch(e.UserEntity!.FullName))
                    .FilterByWildcards(e => e?.UserEntity?.UserName, wpUserName)
                    .ExcludeByWildcards(e => e?.UserEntity?.FullName, wpFullName)
                    .OrderBy(e => e.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.FullName), userRoles.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        if (UserName is not null && UserName.All(u => string.IsNullOrEmpty(u))) UserName = null;
        if (FullName is not null && FullName.All(f => string.IsNullOrEmpty(f))) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                //drive.FolderUsersWithInherited.ClearCache(); // Also clear this to maintain cache consistency
                //drive.FolderUsersWithNoInherited.ClearCache();
                var folderUsers = drive.FolderUsersWithNoInherited.Get(folder);

                var filteredUsers = folderUsers
                    // -UserName matches tenant UserName OR EmailAddress (B2B);
                    // see FilterFolderUsersByUserName.
                    .FilterFolderUsersByUserName(drive, wpUserName)
                    .FilterByWildcards(fu => fu?.UserEntity?.FullName, wpFullName);

                if (NoMatchWarning.IsPresent && !filteredUsers.Any())
                {
                    // A somewhat rough implementation, but it works correctly during CSV import so it's good enough...
                    // A proper implementation would need to process the UserName array one by one from the beginning.
                    WriteWarning($"No match found for UserName '{UserName?[0]}' and FullName '{FullName?[0]}'.");
                    continue;
                }

                foreach (var folderUser in filteredUsers.OrderBy(u => u.UserEntity!.UserName).WithCancellation(cancelHandler.Token))
                {
                    var targetUser = folderUser.GetPSPath();
                    if (!string.IsNullOrEmpty(folderUser?.UserEntity?.FullName))
                    {
                        targetUser += $" ({folderUser.UserEntity.FullName})";
                    }

                    if (ShouldProcess(targetUser, $"Remove FolderUser"))
                    {
                        try
                        {
                            drive.OrchAPISession.UnassignUserFromFolder(folder.Id ?? 0, folderUser?.Id ?? 0);
                            drive.FolderUsersWithInherited.ClearCache();
                            drive.FolderUsersWithNoInherited.ClearCache();
                            drive.ClearFolderCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(targetUser, ex), "RemoveFolderUserError", ErrorCategory.InvalidOperation, folderUser));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
