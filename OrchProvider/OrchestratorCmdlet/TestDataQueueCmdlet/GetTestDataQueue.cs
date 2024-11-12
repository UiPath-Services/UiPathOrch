using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchTestDataQueue")]
    [OutputType(typeof(Entities.TestDataQueue))]
    public class GetTestDataQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TestDataQueueNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            //foreach (var (drive, folder) in drivesFolders)
            //{
            //    var results = drive.GetTestDataQueues(folder);
            //    WriteObject(results, true);
            //}

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.TestDataQueues.Get(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities
                        .FilterByWildcards(ts => ts?.Name, wpName)
                        .OrderBy(ts => ts.Name),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetTestDataQueueError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
