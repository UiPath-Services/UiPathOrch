using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional; // AvailableUserBundlesItems (license code → display name)

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicensedUser")]
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

    // Columns match Add-PmLicenseToPmLicensedUser (and
    // Remove-PmLicenseFromPmLicensedUser) parameter names exactly, so the
    // exported CSV round-trips: one row per (user, license), letting the user
    // delete rows to drop individual licenses before re-importing. License is
    // the human-readable display name (e.g. "Attended - Named User"), which is
    // what the -License parameter accepts. Email is the column the Add cmdlet's
    // Position-0 parameter binds (its -UserName alias accepts the same value).
    private static readonly string[] CsvHeaders = [
        "Path",
        "Email",
        "License"
    ];

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

    // Builds one CSV row (Path, Email, License) with every field escaped, in
    // CsvHeaders order. Pure / static so the round trip — write here, read back
    // via the project's RFC-4180 splitter, bind to Add-PmLicenseToPmLicensedUser
    // — is unit-testable, and so a comma/quote in Email can't shift columns.
    // Prefers email (the Add cmdlet's canonical identifier) and falls back to
    // name when a licensed user has no email recorded.
    internal static string[] BuildLicenseCsvRow(string? drivePath, string? email, string? license) =>
    [
        EscapeCsvValue(drivePath, true),
        EscapeCsvValue(email, true),
        EscapeCsvValue(license)
    ];

    private static void WriteCsvContent(StreamWriter writer, string drivePath, NuLicensedUser user)
    {
        string? identifier = !string.IsNullOrEmpty(user.email) ? user.email : user.name;
        foreach (var license in BuildLicenseDisplayNames(user))
        {
            writer.WriteCsvLine(BuildLicenseCsvRow(drivePath, identifier, license));
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
        // needs no extra per-user fetch (mirrors Get-PmLicensedGroup's list path).
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
