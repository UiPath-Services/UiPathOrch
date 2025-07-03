using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
//using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

// たぶん動いているんだけど、空っぽしか返ってこないので動作を確認できないな。
[Cmdlet(VerbsCommon.Get, "PmDirectoryScope")]
[OutputType(typeof(PmDirectoryEntityInfo))]
class SearchPmDirectoryScopeCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            var directoryScopes = drive.OrchAPISession.GetPmDirectoryScope(drive.GetPartitionGlobalId()!);
            foreach (var directoryScope in directoryScopes ?? [])
            {
                directoryScope.Path = drive.NameColonSeparator;
            }
            WriteObject(directoryScopes, true);
        }
    }
}
