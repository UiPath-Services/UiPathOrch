using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional; // AvailableUserBundlesItems (license code → display name)

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmUserLicense")]
[OutputType(typeof(Entities.NuLicensedUser))]
public class GetUserLicenseUser : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EmailCompleter))]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmLicensedUsers.csv";

    // Columns round-trip into Add-PmUserLicense: UserName binds to
    // its Position-0 -Email parameter via that parameter's [Alias("UserName")],
    // License to -License, Path to -Path. The identifier column is UserName
    // (not Email) on purpose: the License Accountant API returns an empty
    // 'email' for every user (verified on live tenants) and carries the login
    // in 'name', so 'name' is the value that re-matches the user on import
    // (Add matches name OR email). Symmetric with Get-PmGroupLicense's
    // GroupName column. License is the friendly display name the -License
    // parameter accepts.
    private static readonly string[] CsvHeaders = [
        "Path",
        "UserName",
        "License"
    ];

    // True for rows that represent a real, re-importable user. orphan=true rows
    // are dangling license pools whose 'name' is a bundle display name rather
    // than a user, so they are left out of the export — re-importing one would
    // try to allocate to a user that doesn't exist. Pure / static for testing.
    internal static bool IsExportableUser(NuLicensedUser user) => !(user.orphan ?? false);

    // One row per license the user holds, ordered by display name. The user's
    // userBundleLicenses are short codes (e.g. "ATTUNU"); convert each to the
    // display name the -License parameter expects. Unknown codes (a newer server
    // bundle this build doesn't map) fall back to the raw code so nothing is
    // silently lost. Pure / static so the row shape is unit-testable without a
    // live server. Mirrors GetUserLicenseGroup.BuildLicenseDisplayNames.
    internal static List<string> BuildLicenseDisplayNames(NuLicensedUser user)
        => (user.userBundleLicenses ?? [])
            .Select(code => AvailableUserBundlesItems.Items.TryGetValue(code, out var name) ? name : code)
            .OrderBy(license => license)
            .ToList();

    // Builds one CSV row (Path, UserName, License) with every field escaped, in
    // CsvHeaders order. Pure / static so the round trip — write here, read back
    // via the project's RFC-4180 splitter, bind to Add-PmUserLicense
    // — is unit-testable, and so a comma/quote in the user name can't shift
    // columns.
    internal static string[] BuildLicenseCsvRow(string? drivePath, string? userName, string? license) =>
    [
        EscapeCsvValue(drivePath, true),
        EscapeCsvValue(userName, true),
        EscapeCsvValue(license)
    ];

    private static void WriteCsvContent(StreamWriter writer, string drivePath, NuLicensedUser user)
    {
        if (!IsExportableUser(user)) return;
        foreach (var license in BuildLicenseDisplayNames(user))
        {
            writer.WriteCsvLine(BuildLicenseCsvRow(drivePath, user.name, license));
        }
    }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wpEmail = GetFakeBoundParameters(fakeBoundParameters, "Email").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicensedUsers.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var user in result
                    .Where(e => !string.IsNullOrEmpty(e.name))
                    .ExcludeByWildcards(u => u?.name!, wpName)
                    .FilterByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u?.name))
                {
                    string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, user.name!);
                    yield return new CompletionResult(PathTools.EscapePSText(user.name), user.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    private class EmailCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpEmail = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicensedUsers.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var user in result
                    .Where(e => !string.IsNullOrEmpty(e.email))
                    .FilterByWildcards(u => u?.name!, wpName)
                    .ExcludeByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u?.email))
                {
                    string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, user.email!);
                    yield return new CompletionResult(PathTools.EscapePSText(user.email), user.email, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        var wpName = Name.ConvertToWildcardPatternList();
        var wpEmail = Email.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. Filtering / WriteObject stay on the pipeline thread. The
        // licensed-users list already carries userBundleLicenses, so -ExportCsv
        // needs no extra per-user fetch (mirrors Get-PmGroupLicense's list path).
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicensedUsers.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var targetEntities = entities
                    .FilterByWildcards(u => u?.name, wpName)
                    .FilterByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u?.name);

                if (writer is null)
                {
                    WriteObject(targetEntities.Select(e => { var c = e.ShallowClone(); c.Path = result.Source.NameColonSeparator; return c; }), true);
                }
                else
                {
                    foreach (var user in targetEntities)
                    {
                        WriteCsvContent(writer, result.Source.NameColonSeparator, user);
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicensedUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
