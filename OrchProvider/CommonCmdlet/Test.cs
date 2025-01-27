using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
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
#if false
            TestBase tb = new()
            {
                Path = "hoge:\\",
                Text = "I am Base."
            };

            //WriteObject(tb);

            TestBase tb2 = new TestSub()
            {
                Path = "hoge:\\",
                Text = "I am Sub."
            };


            var psObject = new PSObject(tb2);
            psObject.TypeNames.Clear();
            psObject.TypeNames.Add("UiPath.PowerShell.Entities.TmProjectPermission");
            //psObject.TypeNames.Add("UiPath.PowerShell.Entities.DirectoryApplication");

            WriteObject(tb2);

            //var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            //var drives = OrchDriveInfo.EnumOrchDrives(Path);

            //foreach (var drive in drives)
            //{
            //    drive.OrchAPISession.GetAllRolesForUser();
            //}
#endif
        }
    }
}
