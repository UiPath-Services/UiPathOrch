using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicensedGroup")]
[OutputType(typeof(Entities.NuLicensedGroup))]
[OutputType(typeof(Entities.NuLicensedGroupMember))]
public class GetUserLicenseGroup : OrchestratorPSCmdlet
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

    // TODO: This needs to be implemented. We want to make this CSV importable by the Add-OrchPmLicenseToLicenseGroup cmdlet.
    //[Parameter(ParameterSetName = "License")]
    //public SwitchParameter License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmUserLicenseGroups.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "GroupName",
        "UserName",
        "DisplayName",
        "Email",
        "LastInUse"
    ];

    // This CSV is intended to be imported by Remove-OrchPmAllocationFromPmUserLicenseGroup,
    // so this format is fine.
    private static void WriteCsvContent(StreamWriter writer, IEnumerable<NuLicensedGroupMember> output)
    {
        foreach (var member in output)
        {
            string[] line = [
                EscapeCsvValue(member.Path, true),
                EscapeCsvValue(member.GroupName, true),
                EscapeCsvValue(member.name),
                EscapeCsvValue(member.displayName),
                EscapeCsvValue(member.email),
                EscapeCsvValue(member.lastInUse?.ToLocalTime())
            ];
            writer.WriteCsvLine(line);
        }
    }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
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
                        string tiphelp = TipHelp(user);
                        yield return new CompletionResult(PathTools.EscapePSText(user?.name), user?.name, CompletionResultType.Text, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            IEnumerable<NuLicensedGroup> groups = null;
            try
            {
                groups = drive.PmLicensedGroups.Get()
                    .FilterByWildcards(g => g?.name, wpGroupName)
                    .OrderBy(g => g?.name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmNamedUserLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            if (ExpandAllocation.IsPresent || writer is not null)
            {
                foreach (var group in groups)
                {
                    try
                    {
                        var entities = drive.GetPmLicensedGroupAllocations(group);
                        if (entities is null) continue;

                        var targetEntities = entities
                            .FilterByWildcards(u => u?.name, wpUserName)
                            .OrderBy(u => u?.name);

                        if (writer is null)
                        {
                            WriteObject(targetEntities, true);
                        }
                        else
                        {
                            WriteCsvContent(writer, targetEntities);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "GetPmLicensedGroupError", ErrorCategory.InvalidOperation, group));
                    }
                }
            }
            //else if (ExpandUserBundleLicenses.IsPresent)
            //{

            //}
            else
            {
                WriteObject(groups, true);
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
