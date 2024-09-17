using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchWebhook", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Webhook))]
    public class DisableWebhookCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        // これは有効な Webhook だけを列挙するので、共通化できない。
        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetWebhooks());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(e => e.Enabled.GetValueOrDefault())
                        .Where(e => wp.IsMatch(e.Name))
                        .ExcludeByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name!))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var webhooks = drive.GetWebhooks();

                    foreach (var webhook in webhooks
                        .Where(e => e.Enabled.GetValueOrDefault())
                        .FilterByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(webhook.GetPSPath(), "Disable Webhook"))
                        {
                            try
                            {
                                var newWebhook = new Webhook()
                                {
                                    Enabled = false
                                };
                                drive!.OrchAPISession.PatchWebhook(webhook.Id ?? 0, newWebhook);
                                webhook.Enabled = false;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "DisableWebhookError", ErrorCategory.InvalidOperation, webhook));
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
        //            var webhooks = result.GetResult(cancelHandler.Token);
        //            if (webhooks == null) continue;

        //            var drive = result.Source;

        //            foreach (var webhook in webhooks
        //                .Where(e => e.Enabled.GetValueOrDefault())
        //                .FilterByWildcards(e => e.Name!, wpName)
        //                .OrderBy(e => e.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(webhook.GetPSPath(), "Disable Webhook"))
        //                {
        //                    try
        //                    {
        //                        var newWebhook = new Webhook()
        //                        {
        //                            Enabled = false
        //                        };
        //                        drive!.OrchAPISession.PatchWebhook(webhook.Id ?? 0, newWebhook);
        //                        webhook.Enabled = false;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "DisableWebhookError", ErrorCategory.InvalidOperation, webhook));
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
