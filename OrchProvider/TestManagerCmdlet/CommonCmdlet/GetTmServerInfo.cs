using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "TmServerInfo")]
    [OutputType(typeof(Entities.TmServerInfo))]
    public class GetTmServerInfoCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TmDriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchTmDriveInfo.EnumOrchTmDrives(Path);

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetTmServerInfo());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entity = result.GetResult(cancelHandler.Token);
                    if (entity == null) continue;

                    WriteObject(entity);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetTmServerInfoError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
