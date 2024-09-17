using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName_UserName;

namespace UiPath.PowerShell.Commands
{
    // WIP
    [Cmdlet(VerbsCommon.Get, "OrchPmLicensedUser")]
    [OutputType(typeof(Entities.NuLicensedGroup))]
    [OutputType(typeof(Entities.NuLicensedGroupMember))]
    class GetUserLicenseUser: OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        //[Parameter]
        //public SwitchParameter ExpandAllocation { get; set; }

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
        private static readonly string[] CsvHeaders = ["Path", "GroupName", "UserName", "DisplayName"];

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<NuLicensedGroupMember> output)
        {
            foreach (var member in output)
            {
                var line = new StringBuilder();
                line.Append($"{EscapeCsvValue(member.Path, true)},");
                line.Append($"{EscapeCsvValue(member.GroupName, true)},");
                line.Append($"{EscapeCsvValue(member.name)},");
                line.Append($"{EscapeCsvValue(member.displayName)}");
                writer.WriteLine(line.ToString());
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

                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, Positional.GroupName_UserName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmUserLicenseGroups());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var e in entities!
//                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name))
                    {
                        var users = drive.GetPmUserLicenseGroupAllocations(e);
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

            var wpUserName = UserName.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                //if (ExpandAllocation.IsPresent || writer != null)
                //{
                //    using var results = OrchThreadPool.RunForEach(groups,
                //        group => group.GetPSPath(),
                //        group => group,
                //        group => drive.GetPmUserLicenseGroupAllocations(group));

                //    foreach (var result in results)
                //    {
                //        try
                //        {
                //            var entities = result.GetResult(cancelHandler.Token);
                //            if (entities == null) continue;

                //            var group = result.Source!;

                //            var targetEntities = entities
                //                .FilterByWildcards(u => u?.name, wpUserName)
                //                .OrderBy(u => u?.name);

                //            if (writer == null)
                //            {
                //                WriteObject(targetEntities, true);
                //            }
                //            else
                //            {
                //                WriteCsvContent(writer, targetEntities);
                //            }
                //        }
                //        catch (OrchException ex)
                //        {
                //            WriteError(new ErrorRecord(ex, "GetNamedUserLicenseGroupAllocationError", ErrorCategory.InvalidOperation, ex.Target));
                //        }
                //    }
                //}
                //else
                //{
                //    WriteObject(groups, true);
                //}
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
