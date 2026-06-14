using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional; // AvailableUserBundlesItems (license code → display name)

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroupLicense")]
[OutputType(typeof(Entities.NuLicensedGroup))]
[OutputType(typeof(Entities.NuLicensedGroupMember))]
public class GetPmGroupLicenseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ParameterSetName = "ExpandAllocation")]
    public SwitchParameter ExpandAllocation { get; set; }

    // TODO: This needs to be implemented. We want to make this CSV importable by the Add-PmGroupLicense cmdlet.
    //[Parameter(ParameterSetName = "License")]
    //public SwitchParameter License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmLicensedGroups.csv";

    // Columns match Add-PmGroupLicense (and
    // Remove-PmGroupLicense) parameter names exactly, so the
    // exported CSV round-trips: one row per (group, license), letting the user
    // delete rows to drop individual licenses before re-importing. License is
    // the human-readable display name (e.g. "Attended - Named User"), which is
    // what the -License parameter accepts.
    private static readonly string[] CsvHeaders = [
        "Path",
        "GroupName",
        "License"
    ];

    // One row per license the group holds, ordered by display name. The group's
    // userBundleLicenses are short codes (e.g. "ATTUNU"); convert each to the
    // display name the -License parameter expects. Unknown codes (a newer server
    // bundle this build doesn't map) fall back to the raw code so nothing is
    // silently lost. Pure / static so the row shape is unit-testable without a
    // live server.
    internal static List<string> BuildLicenseDisplayNames(NuLicensedGroup group)
        => (group.userBundleLicenses ?? [])
            .Select(code => AvailableUserBundlesItems.Items.TryGetValue(code, out var name) ? name : code)
            .OrderBy(license => license)
            .ToList();

    // Builds one CSV row (Path, GroupName, License) with every field escaped, in
    // CsvHeaders order. Pure / static so the round trip — write here, read back
    // via the project's RFC-4180 splitter, bind to Add-PmGroupLicense
    // — is unit-testable, and so a comma/quote in GroupName can't shift columns.
    internal static string[] BuildLicenseCsvRow(string? drivePath, string? groupName, string? license) =>
    [
        EscapeCsvValue(drivePath, true),
        EscapeCsvValue(groupName, true),
        EscapeCsvValue(license)
    ];

    private static void WriteCsvContent(StreamWriter writer, string drivePath, NuLicensedGroup group)
    {
        foreach (var license in BuildLicenseDisplayNames(group))
        {
            writer.WriteCsvLine(BuildLicenseCsvRow(drivePath, group.name, license));
        }
    }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpGroupName = GetFakeBoundParameters(fakeBoundParameters, "GroupName").ConvertToWildcardPatternList();
            var wpUserName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicensedGroups.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var group in result
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name))
                {
                    var users = drive.GetPmLicensedGroupAllocations(group);
                    foreach (var user in users
                        .Where(u => wp.IsMatch(u?.name))
                        .ExcludeByWildcards(u => u?.name!, wpUserName)
                        .OrderBy(u => u?.name))
                    {
                        string tiphelp = TipHelp(drive, user);
                        yield return new CompletionResult(PathTools.EscapePSText(user?.name), user?.name, CompletionResultType.Text, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        if (ExpandAllocation.IsPresent && writer is null)
        {
            // Two-phase parallel fetch (see Get-OrchUserDetail). Phase 1:
            // per-drive licensed-group list + name filter. Phase 2: the
            // per-group allocation fetch (GetPmLicensedGroupAllocations).
            // Shared cap=4 semaphore; per-org caches serialize same-partition
            // fetches internally. Emission stays on the pipeline thread.
            // -ExportCsv does NOT come here: its license data lives on the
            // group list itself, so it uses the cheap list-only path below.
            using var cancelHandler = new ConsoleCancelHandler();

            using var pool = OrchThreadPool.RunForEachChained(
                drives,
                drive => drive.NameColonSeparator,
                drive => (object)drive,
                drive => drive.PmLicensedGroups.Get()
                    .FilterByWildcards(g => g?.name, wpGroupName)
                    .OrderBy(g => g?.name)
                    .Select(group => (drive, group)),
                t => t.group.GetPSPath(t.drive.NameColonSeparator),
                t => (object)t.group,
                t => t.drive.GetPmLicensedGroupAllocations(t.group),
                cancelHandler.Token);

            foreach (var task in pool)
            {
                try
                {
                    var entities = task.GetResult(cancelHandler.Token);
                    if (entities is null) continue;

                    var (drive, group) = task.Source;
                    var targetEntities = entities
                        .FilterByWildcards(u => u?.name, wpUserName)
                        .OrderBy(u => u?.name);

                    string pathGroupName = System.IO.Path.Combine(drive.NameColonSeparator, group?.name ?? "");
                    WriteObject(targetEntities.Select(m => { var c = m.ShallowClone(); c.Path = drive.NameColonSeparator; c.GroupName = group?.name; c.PathGroupName = pathGroupName; return c; }), true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmGroupLicenseError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            // Phase 1 (per-drive list) failures.
            foreach (var (_, ex) in pool.Phase1Errors)
            {
                WriteError(new ErrorRecord(ex, "GetPmGroupLicenseError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
        else
        {
            // List-only mode: no per-item fan-out, single-phase parallel
            // fetch (per-org caches serialize same-partition internally).
            // Serves both plain output and -ExportCsv — the group list already
            // carries userBundleLicenses, so CSV export needs no extra fetch.
            using var poolResults = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.PmLicensedGroups.Get());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var poolResult in poolResults)
            {
                try
                {
                    var fetched = poolResult.GetResult(cancelHandler.Token);
                    if (fetched is null) continue;
                    var drive = poolResult.Source;
                    var targetGroups = fetched
                        .FilterByWildcards(g => g?.name, wpGroupName)
                        .OrderBy(g => g?.name);

                    if (writer is null)
                    {
                        WriteObject(targetGroups
                            .Select(g => { var c = g.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }), true);
                    }
                    else
                    {
                        foreach (var group in targetGroups)
                        {
                            WriteCsvContent(writer, drive.NameColonSeparator, group);
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmGroupLicenseError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
