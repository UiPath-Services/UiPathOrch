using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name_Email;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicensedUser")]
[OutputType(typeof(Entities.NuLicensedUser))]
public class GetUserLicenseUser: OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EmailCompleter))]
    public string[]? Email { get; set; }

    //[Parameter]
    //public SwitchParameter ExpandAllocation { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    //[Parameter]
    //public string? ExportCsv { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(EncodingCompleter))]
    //[EncodingArgumentTransformation]
    //public Encoding? CsvEncoding { get; set; }

    //private static readonly string DefaultCsvName = "ExportedPmUserLicenseGroups.csv";
    //private static readonly string[] CsvHeaders = ["Path", "GroupName", "UserName", "DisplayName"];

    //private static void WriteCsvContent(StreamWriter writer, IEnumerable<NuLicensedGroupMember> output)
    //{
    //    foreach (var member in output)
    //    {
    //        var line = new StringBuilder();
    //        line.Append($"{EscapeCsvValue(member.Path, true)},");
    //        line.Append($"{EscapeCsvValue(member.GroupName, true)},");
    //        line.Append($"{EscapeCsvValue(member.name)},");
    //        line.Append($"{EscapeCsvValue(member.displayName)}");
    //        writer.WriteLine(line.ToString());
    //    }
    //}

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);
            var wpEmail = CreateWPListFromOtherParameters(commandAst, "Email", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PmLicensedUsers.Get());

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
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
            var wpEmail = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PmLicensedUsers.Get());

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

                WriteObject(targetEntities, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicensedUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
