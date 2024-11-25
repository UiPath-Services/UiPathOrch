using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchMachine", SupportsShouldProcess = true)]
    public class RemoveMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        // TODO: これはマルチスレッド化できる。
        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    string targetFolder = drive.NameColonSeparator;
                    foreach (var machine in drive.Machines.Get().FilterByWildcards(m => m?.Name, wpName).OrderBy(m => m.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(machine.GetPSPath(), "Remove Machine"))
                        {
                            try
                            {
                                drive.OrchAPISession.RemoveMachine(machine.Id ?? 0);
                                drive.Machines.ClearCache();
                                drive.FolderMachinesAssignable.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                var errorRecord = new ErrorRecord(new OrchException(machine.GetPSPath(), ex), "RemoveMachineError", ErrorCategory.InvalidOperation, machine);
                                WriteError(errorRecord);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachineError", ErrorCategory.InvalidOperation, drive);
                    WriteError(errorRecord);
                }
            }
        }
    }
}
