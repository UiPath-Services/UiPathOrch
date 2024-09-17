using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    // WIP
    [Cmdlet(VerbsLifecycle.Enable, "OrchClassicRobot")]
    [OutputType(typeof(Entities.Session))]
    class EnableClassicRobotCommand : OrchestratorPSCmdlet
    {
        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Path>))]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            ulong skip = Skip ?? 0;
            ulong first = First ?? ulong.MaxValue;

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

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
}
