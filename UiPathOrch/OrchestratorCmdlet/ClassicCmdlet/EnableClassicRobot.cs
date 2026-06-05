using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;


namespace UiPath.PowerShell.Commands;

// WIP — disabled. ProcessRecord's body is an empty try{} (a no-op), so shipping
// this would register Enable-OrchClassicRobot as a silently-succeeding cmdlet.
// Gated out (like ConnectJob) until implemented; re-enable by removing #if false.
#if false
[Cmdlet(VerbsLifecycle.Enable, "OrchClassicRobot")]
[OutputType(typeof(Entities.Session))]
class EnableClassicRobotCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        foreach (var (drive, folder) in drivesFolders)
        {
            if (folder.ProvisionType != "Manual") continue;

            try
            {
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetClassicRobotError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
#endif
