using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmLicensedGroup", SupportsShouldProcess = true)]
public class RemoveUserLicenseGroup: OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        var wpGroupName = GroupName
            .Split1stValueByUnescapedCommas()
            .ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            IEnumerable<NuLicensedGroup> groups = null;
            try
            {
                groups = drive.PmLicensedGroups.Get()
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
                        drive.PmLicensedGroups.ClearCache();
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
