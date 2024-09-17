using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands
{
    // このコマンドレットからは、メンバーを追加する機能は外す。
    // 空っぽのグループを追加するだけのコマンドレットでないと、ShouldProcess がうまいことサポートできないため。

    [Cmdlet(VerbsCommon.Add, "OrchPmGroup", SupportsShouldProcess = true)]
    public class AddPmGroupCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                var partitionGlobalId = drive.GetPartitionGlobalId();
                foreach (var groupName in GroupName!)
                {
                    string target = System.IO.Path.Combine(drive.NameColonSeparator, groupName);
                    if (ShouldProcess(target, "Add Group"))
                    {
                        CreateGroupCommand createGroupCommand = new()
                        {
                            partitionGlobalId = partitionGlobalId,
                            id = Guid.NewGuid().ToString(),
                            name = WildcardPattern.Unescape(groupName),
                            directoryUserMemberIDs = []
                        };

                        try
                        {
                            var newGroup = drive.OrchAPISession.CreatePmGroup(createGroupCommand);
                            if (newGroup  != null)
                            {
                                newGroup.Path = drive.NameColonSeparator;
                                WriteObject(newGroup);
                                drive._dicPmGroups = null;
                                drive._dicPmGroups_Exception.ClearCache();
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddPmGroupError", ErrorCategory.InvalidOperation, createGroupCommand));
                        }
                    }
                }
            }
        }
    }
}
