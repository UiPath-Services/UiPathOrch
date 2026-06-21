using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

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
}
