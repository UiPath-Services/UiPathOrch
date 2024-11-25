using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
//using TPositional = UiPath.PowerShell.Positional.IdentityName;
using TPositional = UiPath.PowerShell.Positional.Empty;

namespace UiPath.PowerShell.Commands
{
    // 後で非公開にしておかないと。
    [Cmdlet(VerbsCommon.Search, "OrchPmDirectoryUser")]
    [OutputType(typeof(Entities.TmDirectoryUser))]
    class SearchPmDirectoryUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        //[ArgumentCompleter(typeof(DrivePathCompleter<TPositional>))]
        public string? IdentityName { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                try
                {
                    var partitionGlobalId = drive.GetPartitionGlobalId();
                    var du = drive.OrchAPISession.SearchPmDirectoryUsers(partitionGlobalId!, IdentityName!);
                    WriteObject(du, true);
                    //drive.OrchAPISession.GetIdSetting();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchPmDirectoryUserError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
