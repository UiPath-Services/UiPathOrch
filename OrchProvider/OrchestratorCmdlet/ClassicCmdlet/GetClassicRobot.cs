using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchClassicRobot")]
    [OutputType(typeof(Entities.Session))]
    public class GetClassicRobotCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

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

        private static readonly string DefaultCsvName = "ExportedClassicRobots.csv";
        private static readonly string[] CsvHeaders = [
            "Path",
            "MachineName",
            "Name",
            "Description",
            "Type",
            //"CredentialStoreName",
            "Username",
            "RobotEnvironments"
        ];

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.Session> output)
        {
            foreach (var r in output)
            {
                #region CredentialStoreId を Name に変換
                //string credentialStoreName = null;
                //if (r?.Robot?.CredentialStoreId != null)
                //{
                //    OrchDriveInfo drive = null;
                //    Folder folder = null;
                //    try
                //    {
                //        (drive, folder) = OrchDriveInfo.EnumFolders([r.Path]).FirstOrDefault();
                //    }
                //    catch
                //    {
                //        WriteWarning($"Path '{r.GetPSPath()}' cannot be resolved.");
                //    }

                //    if (drive != null)
                //    {
                //        var credentialStores = drive.GetCredentialStores();
                //        var credentialStore = credentialStores.FirstOrDefault(cs => cs.Id == r.Robot.CredentialStoreId);
                //        if (credentialStore != null)
                //        {
                //            credentialStoreName = credentialStore.Name;
                //        }
                //        else
                //        {
                //            WriteWarning($"{r.GetPSPath()}: CredentialStoreId {r.Robot.CredentialStoreId.ToString()} cannot be resolved.");
                //        }
                //    }
                //}
                #endregion

                var line = new StringBuilder();

                line.Append($"{EscapeCsvValue(r.Path, true)},");
                line.Append($"{EscapeCsvValue(r.Robot?.MachineName)},");
                line.Append($"{EscapeCsvValue(r.Robot?.Name)},");
                line.Append($"{EscapeCsvValue(r.Robot?.Description)},");
                line.Append($"{EscapeCsvValue(r.Robot?.Type)},");
                //line.Append($"{EscapeCsvValue(credentialStoreName)},");
                line.Append($"{EscapeCsvValue(r.Robot?.Username)},");
                line.Append($"{EscapeCsvValue(r.Robot?.RobotEnvironments)}");

                writer.WriteLine(line.ToString());
            }
        }

        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);
                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetSessions(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var s in entities!
                        .Where(s => wp.IsMatch(s.Robot?.Name))
                        .ExcludeByWildcards(s => s?.Robot?.Name, wpName)
                        .OrderBy(s => s.Robot?.Name))
                    {
                        string tiphelp = TipHelp(s);
                        yield return new CompletionResult(PathTools.EscapePSText(s.Robot?.Name), s.Robot?.Name, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private void Output(StreamWriter? writer, IEnumerable<Session> sessions)
        {
            if (writer != null)
            {
                WriteCsvContent(writer, sessions);
            }
            else
            {
                WriteObject(sessions, true);
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name?.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(
                drivesFolders.Where(df => df.folder.ProvisionType == "Manual"),
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetSessions(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var sessions = result.GetResult(cancelHandler.Token);
                    if (sessions == null) continue;

                    Output(writer, sessions
                        .FilterByWildcards(s => s?.Robot?.Name, wpName)
                        .OrderBy(s => s.Robot?.Name));
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetClassicRobotError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
