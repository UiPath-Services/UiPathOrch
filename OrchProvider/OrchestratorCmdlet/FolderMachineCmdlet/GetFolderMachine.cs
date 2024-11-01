using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using Positional = UiPath.PowerShell.Positional.Name;
using System.Text;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchFolderMachine")]
    [OutputType(typeof(Entities.MachineFolder))]
    public class GetFolderMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(NameCompleter))]
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

        private static readonly string DefaultCsvName = "ExportedFolderMachines.csv";

        private static readonly string[] CsvHeaders = ["Path", "Name", "PropagateToSubFolders"];

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

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetMachinesAssignedToFolder(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(m => wp.IsMatch(m.Name))
                        .ExcludeByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, TipHelp(e));
                    }
                }
            }
        }

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<MachineFolder> output)
        {
            // 各フォルダーマシンに対してデータ行を書き込む
            foreach (var m in output.Where(m => m.IsAssignedToFolder.GetValueOrDefault()))
            {
                string[] line = [
                    EscapeCsvValue(m.Path, true),
                    EscapeCsvValue(m.Name, true),
                    EscapeCsvValue(m.PropagateToSubFolders)
                ];

                WriteCsvLine(writer, line);
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetMachinesAssignedToFolder(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    var machines = result.GetResult(cancelHandler.Token);
                    if (machines == null) continue;

                    var targetMachines = machines
                        .FilterByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name);

                    if (writer != null)
                        WriteCsvContent(writer, targetMachines);
                    else
                        WriteObject(targetMachines, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
