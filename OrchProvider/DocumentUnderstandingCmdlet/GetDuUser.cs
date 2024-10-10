using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "DuUser")]
    [OutputType(typeof(Entities.DuClassifier))]
    public class GetDuUserCommand : OrchestratorPSCmdlet
    {
        //[Parameter(Position = 0)]
        //[ArgumentCompleter(typeof(ClassifierNameCompleter))]
        //[SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessRecord()
        {
            var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
            //var wpClassifierName = Name.ConvertToWildcardPatternList();

            foreach (var (drive, project) in drivesProjects)
            {
                string partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                drive.OrchAPISession.GetDuUsers(partitionGlobalId, project.id);
            }

            //// 非同期バージョン
            //using var results = OrchThreadPool.RunForEach(drivesProjects,
            //    dp => dp.project.GetPSPath(),
            //    dp => dp.project,
            //    dp => dp.drive.GetDuClassifiers(dp.project));

            //using var cancelHandler = new ConsoleCancelHandler();
            //foreach (var result in results)
            //{
            //    try
            //    {
            //        var entities = result.GetResult(cancelHandler.Token);
            //        if (entities == null) continue;

            //        WriteObject(entities
            //            .FilterByWildcards(u => u?.name, wpClassifierName)
            //            .OrderBy(e => e.name),
            //            true);
            //    }
            //    catch (OrchException ex)
            //    {
            //        WriteError(new ErrorRecord(ex, "GetDuClassifierError", ErrorCategory.InvalidOperation, ex.Target));
            //    }
            //}
        }
    }
}
