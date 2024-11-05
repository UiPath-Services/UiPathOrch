using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmUser")]
    [OutputType(typeof(Entities.PmUser))]
    public class GetPmUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmUserEmailCompleter<Positional.Email>))]
        [SupportsWildcards]
        [Alias("UserName")]
        public string[]? Email { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Email>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpEmail = Email.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetPmUsers().Values);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities
                        .FilterByWildcards(u => u?.email, wpEmail)
                        .OrderBy(u => u.email),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
