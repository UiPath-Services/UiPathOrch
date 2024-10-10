using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchPmLicensedGroup", SupportsShouldProcess = true)]
    public class RemoveUserLicenseGroup: OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter<GroupName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<GroupName>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpGroupName = GroupName
                .Split1stValueByUnescapedCommas()
                .ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                IEnumerable<NuLicensedGroup> groups = null;
                try
                {
                    groups = drive.GetPmLicensedGroups()
                        .FilterByWildcards(g => g?.name, wpGroupName)
                        .OrderBy(g => g?.name);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }

                foreach (var group in groups)
                {
                    if (ShouldProcess(group.GetPSPath(), "Remove PmLicensedGroup"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemovePmLicensedGroup(group.id);
                            drive._dicPmLicensedGroups = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "RemovePmLicenseGroupError", ErrorCategory.InvalidOperation, group));
                        }
                    }
                }
            }
        }
    }
}
