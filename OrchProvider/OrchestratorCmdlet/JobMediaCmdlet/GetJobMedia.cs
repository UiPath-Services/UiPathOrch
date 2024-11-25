using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
//using TPositional = UiPath.PowerShell.Positional.JobId_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchJobMedia")]
    [OutputType(typeof(Entities.ExecutionMedia))]
    public class GetJobMediaCommand : OrchestratorPSCmdlet
    {
        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
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

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetExecutionMedia(df.folder, skip, first));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var recordings = result.GetResult(cancelHandler.Token);
                    if (recordings == null) continue;

                    WriteObject(recordings, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetJobMediaError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
