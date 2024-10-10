using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName_UserName;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmLicensedGroup")]
    [OutputType(typeof(Entities.NuLicensedGroup))]
    [OutputType(typeof(Entities.NuLicensedGroupMember))]
    public class GetUserLicenseGroup: OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter<GroupName_UserName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(ParameterSetName = "ExpandAllocation")]
        public SwitchParameter ExpandAllocation { get; set; }

        // TODO: これ実装する必要がある。この CSV を、Add-OrchPmLicenseToLicenseGroup cmdlet でインポートできるようにしたい。
        //[Parameter(ParameterSetName = "License")]
        //public SwitchParameter License { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_UserName>))]
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

        // この CSV は、Remove-OrchPmAllocationFromPmUserLicenseGroup にインポートすることを意図したものなので
        // これで良い。
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
                WriteCsvLine(writer, line);
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
                var drives = ResolveDrives(fakeBoundParameters);

                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", Positional.GroupName_UserName.Parameters);
                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, Positional.GroupName_UserName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmLicensedGroups());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var e in entities!
                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name))
                    {
                        var users = drive.GetPmLicensedGroupAllocations(e);
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
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpGroupName = GroupName.ConvertToWildcardPatternList();
            var wpUserName = UserName.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                IEnumerable<NuLicensedGroup> groups = null;
                try
                {
                    groups = drive.GetPmLicensedGroups()
                        .FilterByWildcards(g => g?.name, wpGroupName)
                        .OrderBy(g => g?.name);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmNamedUserLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }

                if (ExpandAllocation.IsPresent || writer != null)
                {
                        using var results = OrchThreadPool.RunForEach(groups,
                            group => group.GetPSPath(),
                            group => group,
                            group => drive.GetPmLicensedGroupAllocations(group));

                    foreach (var result in results)
                    {
                        try
                        {
                            var entities = result.GetResult(cancelHandler.Token);
                            if (entities == null) continue;

                            var targetEntities = entities
                                .FilterByWildcards(u => u?.name, wpUserName)
                                .OrderBy(u => u?.name);

                            if (writer == null)
                            {
                                WriteObject(targetEntities, true);
                            }
                            else
                            {
                                WriteCsvContent(writer, targetEntities);
                            }
                        }
                        catch (OrchException ex)
                        {
                            WriteError(new ErrorRecord(ex, "GetPmLicensedGroupError", ErrorCategory.InvalidOperation, ex.Target));
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

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
