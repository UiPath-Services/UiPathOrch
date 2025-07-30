using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchFolderMachine")]
[OutputType(typeof(Entities.MachineFolder))]
public class GetFolderMachineCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    // これ足すの大変だな。。
    //[Parameter]
    //public SwitchParameter IncludeInherited { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
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

            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.FolderMachinesAssigned.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var machines = result.GetResult(cancelHandler.Token);
                if (machines is null) continue;

                var targetMachines = machines
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name);

                if (writer is not null)
                    WriteCsvContent(writer, targetMachines);
                else
                    WriteObject(targetMachines, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetFolderMachineError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
