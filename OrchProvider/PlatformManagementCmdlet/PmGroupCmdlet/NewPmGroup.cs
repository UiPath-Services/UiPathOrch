using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName;

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
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                var partitionGlobalId = drive.GetPartitionGlobalId();
                foreach (var groupName in GroupName!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = System.IO.Path.Combine(drive.NameColonSeparator, groupName);
                    if (ShouldProcess(target, "Add PmGroup"))
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
                                drive._dicPmDirectoryUsers = null;
                                drive._dicSearchForUsersAndGroups = null;
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
