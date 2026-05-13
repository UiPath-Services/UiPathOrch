using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Unfortunately, this does not work
//[Cmdlet(VerbsCommon.Get, "OrchCgIndex")]
//[OutputType(typeof(Bucket))]
class GetCgIndexCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(BucketNameCompleter))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[SupportsWildcards]
    //public string[]? Path { get; set; }

    //[Parameter]
    //public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

    //protected override void ProcessRecord()
    //{
    //    var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
    //    //        var wpName = Name.ConvertToWildcardPatternList();


    //    foreach (var (drive, folder) in drivesFolders)
    //    {
    //        string partitionGlobalId = drive.GetPartitionGlobalId();
    //        var (_, tenantKey) = drive.GetTenantId();
    //        string str = drive.OrchAPISession.GetCgIndex(partitionGlobalId!, tenantKey!, folder.Key!);
    //        WriteObject(str);
    //    }
    //}
}
