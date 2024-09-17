using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchWebhook", SupportsShouldProcess = true)]
    public class RemoveWebhookCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(WebhookNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var entities = drive.GetWebhooks();

                    foreach (var webhook in entities
                        .FilterByWildcards(wh => wh?.Name, wpName)
                        .OrderBy(wh => wh.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(webhook.GetPSPath(), "Remove Webhook"))
                        {
                            try
                            {
                                drive.OrchAPISession.RemoveWebhooks(webhook.Id ?? 0);
                                drive._dicWebhooks = null;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "RemoveWebhookError", ErrorCategory.InvalidOperation, webhook));
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
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
        //    var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        //    using var results = OrchThreadPool.RunForEach(drives,
        //        drive => drive.NameColonSeparator,
        //        drive => drive,
        //        drive => drive.GetWebhooks());

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var drive = result.Source!;

        //            foreach (var webhook in entities
        //                .FilterByWildcards(wh => wh.Name!, wpName)
        //                .OrderBy(wh => wh.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(webhook.GetPSPath(), "Remove Webhook"))
        //                {
        //                    try
        //                    {
        //                        drive.OrchAPISession.RemoveWebhooks(webhook.Id ?? 0);
        //                        drive._dicWebhooks = null;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "RemoveWebhookError", ErrorCategory.InvalidOperation, webhook));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetWebhookError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
