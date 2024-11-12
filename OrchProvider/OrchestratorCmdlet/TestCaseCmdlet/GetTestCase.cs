using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchTestCase")]
    [OutputType(typeof(Entities.TestCaseDefinition))]
    public class GetTestCaseCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TestCaseNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        //[Parameter(Position = 1)]
        //[ArgumentCompleter(typeof(PackageIdentifierCompleter))]
        //[SupportsWildcards]
        //public string[]? PackageIdentifier { get; set; }

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

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.TestCases.Get(df.folder)
            );

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities
                        .FilterByWildcards(tc => tc?.Name, wpName)
                        .OrderBy(tc => tc.PackageIdentifier)
                        .ThenBy(tc => tc.Name),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
