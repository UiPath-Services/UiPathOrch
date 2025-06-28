using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCredentialStore", SupportsShouldProcess = true)]
public class RemoveCredentialStoreCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var credentialStores = drive.CredentialStores.Get();

                foreach (var cs in credentialStores.FilterByWildcards(c => c?.Name, wpName).OrderBy(c => c.Name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (ShouldProcess(cs.GetPSPath(), "Remove Credential Store"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveCredentialStore(cs.Id ?? 0);
                            drive.CredentialStores.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(cs.GetPSPath(), ex), "RemoveCredentialStoreError", ErrorCategory.InvalidOperation, cs);
                            WriteError(errorRecord);
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
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCredentialStoreError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    // cap はドライブ毎にするから、マルチスレッドのままでも良い気もするが。。
    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetCredentialStores());

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var drive = result.Source;

    //            foreach (var cs in entities.FilterByWildcards(c => c.Name!, wpName).OrderBy(c => c.Name))
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                string target = System.IO.Path.Combine(drive.NameColon, cs.Name!);
    //                if (ShouldProcess(target, "Remove Credential Store"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.RemoveCredentialStore(cs.Id ?? 0);
    //                        drive._dicCredentialStores = null;
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        var errorRecord = new ErrorRecord(new OrchException(target, ex), "RemoveCredentialStoreError", ErrorCategory.InvalidOperation, cs);
    //                        WriteError(errorRecord);
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetCredentialStoreError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
