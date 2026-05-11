using System.Management.Automation;
using UiPath.PowerShell.Positional;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUser")]
[OutputType(typeof(Entities.User))]
public class GetUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    // Deprecated. Routes to Get-OrchUserDetail via the shared helper. Kept
    // for backward compat; will be removed in a future major release.
    [Parameter]
    public SwitchParameter ExpandDetails { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedUsers.csv";

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        // Detail path: triggered by either the deprecated -ExpandDetails switch
        // or by -ExportCsv (the CSV row shape includes detail fields, so the
        // user-facing contract has always been "CSV implies detail enrichment").
        // -ExportCsv stays supported (output type matches CSV row shape: User);
        // only -ExpandDetails emits a deprecation warning.
        bool useDetailPath = ExpandDetails.IsPresent || !string.IsNullOrEmpty(ExportCsv);

        if (useDetailPath)
        {
            if (ExpandDetails.IsPresent)
            {
                WriteWarning(
                    "'-ExpandDetails' on Get-OrchUser is deprecated and will be removed in a " +
                    "future major release. Use 'Get-OrchUserDetail' instead.");
            }

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, GetUserDetailCommand.CsvHeaders);

            GetUserDetailCommand.EmitDetailedUsers(this, drives, wpUserName, wpFullName, wpType, writer);

            if (!string.IsNullOrEmpty(ExportCsv))
            {
                WriteCSVExportedMessage(this, providerCsvPath);
            }
            return;
        }

        // List-only path: emit shallow User entries from each drive.
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var users = drive.GetUsers();
                var targetUsers = users
                    .FilterByWildcards(u => u?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.Type, wpType)
                    .OrderBy(u => u.UserName)
                    .ToList();

                if (targetUsers.Count == 0) continue;

                WriteObject(targetUsers, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
