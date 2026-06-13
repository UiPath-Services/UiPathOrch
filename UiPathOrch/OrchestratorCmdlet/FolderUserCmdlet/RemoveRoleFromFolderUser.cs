using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;


namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchRoleFromFolderUser", SupportsShouldProcess = true)]
public class RemoveRoleFromFolderUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserFullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FolderRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class FolderUserUserNameCompleter : OrchArgumentCompleter
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

            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(eu => wp.IsMatch(eu.UserEntity!.UserName!))
                    .ExcludeByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    var ret = new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                    yield return ret;
                }
            }
        }
    }

    private class FolderUserFullNameCompleter : OrchArgumentCompleter
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

            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(eu => wp.IsMatch(eu.UserEntity?.FullName))
                    .ExcludeByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(userRoles);
                    var ret = new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.FullName), userRoles.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                    yield return ret;
                }
            }
        }
    }

    private class RolesCompleter : OrchArgumentCompleter
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

            var wpFullName = GetFakeBoundParameters(fakeBoundParameters, "FullName").ConvertToWildcardPatternList();
            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();
            var wpRoles = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType))
                {
                    if (userRoles.Roles is not null)
                    {
                        foreach (var role in userRoles.Roles
                            .Where(r => wp.IsMatch(r.Name))
                            .ExcludeByWildcards(role => role?.Name, wpRoles)
                            .OrderBy(role => role.Name))
                        {
                            string tiphelp = TipHelp(userRoles);
                            var ret = new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
                            yield return ret;
                        }
                    }
                }
            }
        }
    }

    // This was not multi-threaded to begin with
    protected override void ProcessRecord()
    {
        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        var wpType = Type.ConvertToWildcardPatternList();

        // The first element may come from CSV input, so split the first element by commas
        //if (Roles is not null && Roles.Length > 0) Roles = Roles[0].Split(',').Concat(Roles.Skip(1)).ToArray();
        var wpRoles = Roles.SplitValuesByUnescapedCommasPreservingEscapes().ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                // Match -Roles on role.Name (the field this cmdlet's RolesCompleter emits, and what
                // Add-OrchRoleToFolderUser matches on) so a tab-completed value resolves and Add/Remove
                // are symmetric. (Was role.DisplayName, which silently missed roles whose DisplayName
                // differs from Name.)
                var tenantRoles = drive.Roles.Get().Where(role => role.Type != "Tenant").FilterByWildcards(role => role?.Name, wpRoles);
                try
                {
                    // Extract the users to be edited
                    var existingUsers = drive.FolderUsersWithNoInherited.Get(folder);
                    List<UserRoles> editingUsers = existingUsers
                        .FilterByNames(eu => eu?.UserEntity?.FullName, FullName)
                        // -UserName matches tenant UserName OR EmailAddress (B2B);
                        // see FilterFolderUsersByUserName.
                        .FilterFolderUsersByUserName(drive, UserName)
                        .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                        .ToList();

                    foreach (var user in editingUsers.OrderBy(user => user.UserEntity!.FullName).WithCancellation(cancelHandler.Token))
                    {
                        IEnumerable<SimpleRole> existingRoles = user.Roles;

                        // Extract roles to be removed
                        var targetRoles = existingRoles!.FilterByWildcards(role => role?.Name, wpRoles);
                        if (!targetRoles.Any()) continue;
                        string strTargetRoles = string.Join(", ", targetRoles.Select(role => role.Name));

                        // Remove the roles to be deleted
                        IEnumerable<SimpleRole> keepingRoles = existingRoles!.ExcludeByWildcards(role => role?.Name, wpRoles!);

                        string targetUser = user.GetPSPath();
                        try
                        {
                            if (ShouldProcess(targetUser, $"Remove Roles {strTargetRoles}"))
                            {
                                drive.OrchAPISession.AssignUser(folder.Id ?? 0, user.Id ?? 0, keepingRoles.Select(role => role.Id ?? 0));
                                drive.FolderUsersWithInherited.ClearCache(folder);
                                drive.FolderUsersWithNoInherited.ClearCache(folder);
                                drive.ClearFolderCache(folder);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(targetUser, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, user);
                            WriteError(errorRecord);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string targetFolder = folder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(targetFolder, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
