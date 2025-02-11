using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchPmUser", SupportsShouldProcess = true)]
[OutputType(typeof(PmUser))]
public class RemovePmUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var users = drive.PmUsers.Get();

                var partitionGlobalId = drive!.GetPartitionGlobalId();

                var targetUsers = users.FilterByWildcards(u => u?.email, wpEmail);

                if (NoMatchWarning.IsPresent && !targetUsers.Any())
                {
                    // ちょっと適当な実装だけど、これでも CSV インポート時にちゃんと動くから十分か。。
                    // ちゃんと実装するには、UserName の配列を先頭から順にひとつずつ処理しないといけない。
                    WriteWarning($"No match found for UserName '{Email![0]}'.");
                    continue;
                }

                foreach (var user in targetUsers.OrderBy(u => u.email))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = user.GetPSPath();
                    if (ShouldProcess(target, "Remove PmUser"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemovePmUser(user.id!);
                            drive.PmUsers.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmUserError", ErrorCategory.InvalidOperation, user));
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpUserName = UserName.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetPmUsers()
    //    );

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var drive = result.Source;

    //            var partitionGlobalId = drive!.GetPartitionGlobalId();

    //            foreach (var user in entities.Values
    //                .FilterByWildcards(u => u.userName!, wpUserName)
    //                .OrderBy(u => u.userName))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                string target = user.GetPSPath();
    //                if (ShouldProcess(target, "Remove PmUser"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemovePmUser(user.id!);
    //                        drive._dicPmUsers?.TryRemove(user.id!, out _);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(target, ex), "RemovePmUserError", ErrorCategory.InvalidOperation, user));
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetPmUserError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
