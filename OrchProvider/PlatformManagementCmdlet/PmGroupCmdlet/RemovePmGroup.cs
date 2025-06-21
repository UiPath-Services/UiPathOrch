using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmGroup", SupportsShouldProcess = true)]
public class RemovePmGroupCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var groups = drive.PmGroups.Get();

                var partitionGlobalId = drive.GetPartitionGlobalId();

                foreach (var group in groups
                    .Where(g => g is not null)
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(group!.GetPSPath(), "Remove PmGroup"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemovePmGroup(partitionGlobalId!, group?.id);
                            drive.PmGroups.ClearCache(group?.id);
                            drive._dicSearchPmDirectory = null;
                            drive._dicSearchDirectory = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "RemovePmGroupError", ErrorCategory.InvalidOperation, group));
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

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumPmDrives(Path);
    //    var wpDisplayName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetPmGroups());

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var groups = result.GetResult(cancelHandler.Token);
    //            if (groups is null) continue;

    //            var drive = result.Source;
    //            var partitionGlobalId = drive.GetPartitionGlobalId();

    //            foreach (var group in groups.Values
    //                .Where(g => g is not null)
    //                .FilterByWildcards(g => g?.displayName!, wpDisplayName)
    //                .OrderBy(g => g?.displayName))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                if (ShouldProcess(group!.GetPSPath(), "Remove IdGroup"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveIdGroup(partitionGlobalId!, group!.id!);
    //                        drive._dicPmGroups?.TryRemove(group.id!, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "RemovePmGroupError", ErrorCategory.InvalidOperation, group));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetPmGroupError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
