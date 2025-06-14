using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

// 残念ながら、これは OAuth app からは動かないようだ。
//[Cmdlet(VerbsCommon.Get, "OrchConnection")]
//[OutputType(typeof(Bucket))]
class GetConnectionCommand : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(BucketNameCompleter<TPositional, False>))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    //[Parameter]
    //public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        //var drivesFolders = OrchDriveInfo.EnumFolders(Path, false, 0, true);

        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    drive.OrchAPISession.GetConnections(folder.Id is null ? 0 : folder.Id.Value);
        //}
    }
}
