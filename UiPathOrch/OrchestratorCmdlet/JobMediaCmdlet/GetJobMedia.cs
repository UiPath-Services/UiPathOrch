using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchJobMedia")]
[OutputType(typeof(Entities.ExecutionMedia))]
public class GetJobMediaCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.JobsHavingExecutionMedia.Fetch(df.folder, skip: skip, first: first));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var recordings = result.GetResult(cancelHandler.Token);
                if (recordings is null) continue;

                WriteObject(recordings, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetJobMediaError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
