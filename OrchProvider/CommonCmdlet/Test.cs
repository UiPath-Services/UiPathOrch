using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTest")]
[OutputType(typeof(Bucket))]
class GetTestCommand : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(BucketNameCompleter<TPositional>))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
//           var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        foreach (var drive in drives)
        {
            var feeds = drive.LibraryFeeds.Get();
            WriteObject(feeds, true);
        }
    }
}
