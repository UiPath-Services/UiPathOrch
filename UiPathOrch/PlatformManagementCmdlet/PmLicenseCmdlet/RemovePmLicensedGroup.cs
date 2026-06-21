using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmLicensedGroup", SupportsShouldProcess = true)]
public class RemoveUserLicenseGroup : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        var wpGroupName = GroupName
            .ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
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

            foreach (var group in groups
                .WithProgressBar(this, $"Removing licensed groups in {drive.NameColonSeparator}", g => g.name)
                .WithCancellation(cancelHandler.Token))
            {
                if (ShouldProcess(group.GetPSPath(drive.NameColonSeparator), "Remove PmLicensedGroup"))
                {
                    try
                    {
                        drive.OrchAPISession.RemovePmLicensedGroup(group.id);
                        drive.PmLicensedGroups.ClearCache();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(group.GetPSPath(drive.NameColonSeparator), ex), "RemovePmLicenseGroupError", ErrorCategory.InvalidOperation, group));
                    }
                }
            }
        }
    }
}
