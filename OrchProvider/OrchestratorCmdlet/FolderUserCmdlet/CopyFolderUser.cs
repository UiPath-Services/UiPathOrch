using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchFolderUser", SupportsShouldProcess = true)]
public class CopyFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    // TODO: Could this be changed to string[]?
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

            // Exclude UserNames already selected via parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);
            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(fu => wp.IsMatch(fu.UserEntity!.UserName))
                    .ExcludeByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName!))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(Path);
        var srcDrivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);

        var userMapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, UserMappingCsv);

        // Do nothing if source and destination are the same
        if (srcRootFolder == dstRootFolder) return;

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, "Copying folder users...");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (_, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                // No need to search for dstFolder if there are no entities to copy
                //srcDrive.FolderUsersWithInherited.ClearCache();
                //srcDrive.FolderUsersWithNoInherited.ClearCache();
                var srcEntities = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType);
                if (!srcEntities.Any()) continue;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
            if (dstFolder is null || srcFolder == dstFolder) continue;

            try
            {
                Core.OrchProvider.CopyFolderUsers(this,
                    srcDrive, srcFolder, wpUserName, wpType,
                    dstDrive, dstFolder, reporter,
                    false, cancelHandler.Token, userMapping);
                dstDrive.FolderUsersWithInherited.ClearCache();
                dstDrive.FolderUsersWithNoInherited.ClearCache();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderUserError", ErrorCategory.InvalidOperation, dstFolder));
            }
        }
    }
}
