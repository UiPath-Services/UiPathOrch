using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchFolderUser")]
[OutputType(typeof(Entities.UserRoles))]
public class GetFolderUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserUserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserFullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter]
    public SwitchParameter IncludeInherited { get; set; }

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

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedFolderUsers.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Type",
        "UserName",
        "FullName",
        "FolderRoles",
    ];

    private class FolderUserUserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            //var drivesFolders = ResolveDrivesFoldersWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

            // Exclude UserNames already selected via parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);

            // Only include FullNames selected via parameter
            var wpFullName = GetFakeBoundParameters(fakeBoundParameters, "FullName").ConvertToWildcardPatternList();
            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(fu => wp.IsMatch(fu.UserEntity!.UserName))
                    .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .ExcludeByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
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
            //var drivesFolders = ResolveDrivesFoldersWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

            // Only include UserNames selected via parameter
            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();

            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

            // Exclude FullNames already selected via parameter from the candidates
            var wpFullName = CreateSelfExclusionList(commandAst, "FullName", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df =>
            {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(fu => wp.IsMatch(fu.UserEntity?.FullName))
                    .ExcludeByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.FullName), userRoles.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private static void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.UserRoles> output)
    {
        // Write data rows for each folder user
        foreach (var p in output)
        {
            string[] line = [
                EscapeCsvValue(p.Path, true),
                EscapeCsvValue(p?.UserEntity?.Type),

                // For DirectoryGroup, output FullName in the UserName column...
                // This is because PmBulkResolveByName() is case-sensitive.
                // This is necessary to allow importing groups with Add-OrchFolderUser.
                p?.UserEntity?.Type == "DirectoryGroup" ?
                    EscapeCsvValue(p?.UserEntity?.FullName) :
                    EscapeCsvValue(p?.UserEntity?.UserName),

                EscapeCsvValue(p?.UserEntity?.FullName),
                EscapeCsvValue(string.Join(",", p?.Roles?.Select(r => WildcardPattern.Escape(r.Name).Replace(",", "`,"))!))
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpType = Type.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                return IncludeInherited
                    ? df.drive.FolderUsersWithInherited.Get(df.folder)
                    : df.drive.FolderUsersWithNoInherited.Get(df.folder);
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var userRoles = result.GetResult(cancelHandler.Token);
                if (userRoles is null) continue;

                var (drive, _) = result.Source;
                var targets = userRoles
                    // Match -UserName against tenant UserName OR EmailAddress
                    // (B2B aware); UserEntity itself lacks EmailAddress, so we
                    // resolve via drive.Users.Get() inside the helper.
                    .FilterFolderUsersByUserName(drive, UserName)
                    .FilterByNames(u => u?.UserEntity?.FullName, FullName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.Type!)
                    .ThenBy(u => u.UserEntity!.UserName!)
                    .ThenBy(u => u.UserEntity!.FullName);

                if (writer is not null)
                {
                    WriteCsvContent(writer, targets);
                }
                else
                {
                    WriteObject(targets, true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
