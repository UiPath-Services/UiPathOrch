using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmGroup", SupportsShouldProcess = true)]
public class RemovePmGroupCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
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
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var groups = drive.PmGroups.Get();

                var partitionGlobalId = drive.GetPartitionGlobalId();

                foreach (var group in groups
                    .Where(g => g is not null)
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name)
                    .WithProgressBar(this, $"Removing PmGroups in {drive.NameColonSeparator}", g => g?.name)
                    .WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(group!.GetPSPath(drive.NameColonSeparator), "Remove PmGroup"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemovePmGroup(partitionGlobalId!, group?.id);
                            drive.PmGroups.ClearCache(group?.id);
                            drive.SearchPmDirectoryCache.ClearCache();
                            drive.SearchDirectoryCache.ClearCache();

                            RemoveLicensingRecord(drive, group!);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(drive.NameColonSeparator), ex), "RemovePmGroupError", ErrorCategory.InvalidOperation, group));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    /// <summary>
    /// Deletes the group's LICENSING record, which deleting the group does not.
    ///
    /// Left alone it comes back from the API with an empty name and orphan=true, still holding
    /// whatever bundles the group had, and the portal will not remove it -- the row shows as
    /// "N.A" with its delete button disabled. So every Remove-PmGroup used to leave a row
    /// behind that only the API could clear. It costs no licence (measured: a bundle held by
    /// an orphan stays out of `allocated`), but it accumulates in Get-PmGroupLicense forever.
    ///
    /// Run AFTER the group, not before. If the group delete failed we would otherwise have
    /// stripped the licences off a group that still exists, which is worse than a leftover
    /// row -- and the row is now removable. The record carries the group's own id, which is
    /// how it is found here and how Remove-PmLicensedGroup -Id reaches one that slips through.
    /// </summary>
    private void RemoveLicensingRecord(OrchDriveInfo drive, PmGroup group)
    {
        if (string.IsNullOrEmpty(group.id)) return;

        // A standalone Orchestrator has no licensing records to leave behind: its licensing
        // endpoints do not exist (the server answers with its HTML page), so looking would only
        // turn every Remove-PmGroup there into a warning about an orphan row that cannot exist.
        // IsCloud covers Automation Cloud and Automation Suite, which both keep the records.
        if (!drive._psDrive.IsCloud) return;

        try
        {
            var licensed = drive.PmLicensedGroups.Get()
                .FirstOrDefault(lg => string.Equals(lg?.id, group.id, StringComparison.OrdinalIgnoreCase));
            if (licensed is null) return;

            drive.OrchAPISession.RemovePmLicensedGroup(licensed.id);
            drive.PmLicensedGroups.ClearCache();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The group itself is already gone, so the operation asked for did succeed. Say
            // what was left behind and how to finish it rather than reporting a failure.
            WriteWarning($"\"{group.name}\" was removed, but its licensing record ({group.id}) was not: "
                + $"{OrchException.ExtractMessage(ex) ?? ex.Message} "
                + $"It will appear in Get-PmGroupLicense as an orphan row; remove it with "
                + $"Remove-PmLicensedGroup -Id {group.id}.");
        }
    }
}
