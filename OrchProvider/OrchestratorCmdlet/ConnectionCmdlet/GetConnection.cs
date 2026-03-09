using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Unfortunately, this does not seem to work from an OAuth app.
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
        //var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    drive.OrchAPISession.GetConnections(folder.Id is null ? 0 : folder.Id.Value);
        //}
    }
}
