using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "TmTestCase")]
    [OutputType(typeof(Entities.TmTestCase))]
    public class GetTmTestCaseCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TmTestCaseNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessRecord()
        {
            var drivesProjects = OrchTmDriveInfo.EnumFolders(Path, Recurse.IsPresent);
            var wpName = Name.ConvertToWildcardPatternList();

            //foreach (var driveProject in drivesProjects)
            //{
            //    var (drive, project) = driveProject;
            //    try
            //    {
            //        WriteObject(drive.GetTmTestCases(project), true);
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new OrchException(project.GetPSPath(), ex);
            //    }
            //}

            using var results = OrchThreadPool.RunForEach(drivesProjects,
                dp => dp.project.GetPSPath(),
                dp => dp.project,
                dp => dp.drive.GetTmTestCases(dp.project));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entity = result.GetResult(cancelHandler.Token);
                    if (entity == null) continue;

                    WriteObject(entity
                        .FilterByWildcards(e => e?.name, wpName)
                        .OrderBy(e => e.objKey!, ObjKeyComparer.Instance),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetTmTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
