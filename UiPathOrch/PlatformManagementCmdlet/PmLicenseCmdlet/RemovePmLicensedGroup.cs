using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmLicensedGroup", SupportsShouldProcess = true)]
public class RemovePmLicensedGroupCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    // An ORPHAN row -- a licensing record whose group has been deleted -- comes back with an
    // empty name, and -GroupName cannot reach it: '' matches nothing and '*' matches every
    // live group as well. The portal cannot delete one either; it lists the row as "N.A" with
    // its delete button disabled. Addressing it by id is the only way to remove one without
    // taking the real groups along.
    //
    // ANDed with -GroupName when both are given, so a name pattern can be narrowed by id
    // rather than the two fighting.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

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
        var wpId = Id.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<NuLicensedGroup> groups = null;
            try
            {
                groups = drive.PmLicensedGroups.Get()
                    .FilterByWildcards(g => g?.name, wpGroupName)
                    .FilterByWildcards(g => g?.id, wpId)
                    .OrderBy(g => g?.name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseGroupError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            // Falling back to the id for both the bar and the prompt: an orphan row has no
            // name, and "remove this: yotsudatest:\" is not a question anyone can answer.
            foreach (var group in groups
                .WithProgressBar(this, $"Removing licensed groups in {drive.NameColonSeparator}",
                    g => string.IsNullOrEmpty(g.name) ? g.id : g.name)
                .WithCancellation(cancelHandler.Token))
            {
                string target = string.IsNullOrEmpty(group.name)
                    ? System.IO.Path.Combine(drive.NameColonSeparator, group.id ?? "")
                    : group.GetPSPath(drive.NameColonSeparator);

                if (ShouldProcess(target, "Remove PmLicensedGroup"))
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
                        WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmLicenseGroupError", ErrorCategory.InvalidOperation, group));
                    }
                }
            }
        }
    }
}
