using System.Data;
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Unfortunately, it seems this API cannot be called with an external app.
//[Cmdlet(VerbsCommon.Get, "OrchAllRolesForUser")]
//[OutputType(typeof(Entities.Role))]
class GetAllRolesForUserCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    //[SupportsWildcards]
    //[ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    //public string[]? Type { get; set; }

    //[Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    //[SupportsWildcards]
    //[ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    //public string[]? UserName { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(DriveCompleter))]
    //public string[]? Path { get; set; }

    ////[Parameter]
    ////[ValidateSet("Tenant", "Folder", "Mixed")]
    ////public string? Type { get; set; }

    //[Parameter]
    //public SwitchParameter ExpandPermission { get; set; }

    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpUserName = UserName.ConvertToWildcardPatternList();

    //    foreach (var drive in drives) {
    //        var users = drive.GetUsers();

    //        foreach (var user in users
    //            .FilterByWildcards(u => u?.UserName, wpUserName)
    //            //.FilterByWildcards(u => u?.Type, wpType)
    //            .OrderBy(u => u.UserName))
    //        {
    //            string s = drive.OrchAPISession.GetAllRolesForUser(user.UserName!);
    //            WriteObject(s);
    //        }
    //    }
    //}
}
