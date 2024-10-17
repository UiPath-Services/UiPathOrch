using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    public class EnableWebhookCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
    {
        public virtual string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        // これは無効な Webhook だけを列挙するので、共通化できない。
        internal class NameCompleter : OrchArgumentCompleter
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
                        .Where(e => Enable.Value
                            ? !e.Enabled.GetValueOrDefault()
                            : e.Enabled.GetValueOrDefault())
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
                        .Where(e => Enable.Value
                            ? !e.Enabled.GetValueOrDefault()
                            : e.Enabled.GetValueOrDefault())
                        .FilterByWildcards(e => e?.Name, wpName)
                        .OrderBy(e => e.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string action = $"{(Enable.Value ? "Enable" : "Disable")} Webhook";

                        if (ShouldProcess(webhook.GetPSPath(), action))
                        {
                            try
                            {
                                var newWebhook = new Webhook()
                                {
                                    Enabled = Enable.Value
                                };
                                drive!.OrchAPISession.PatchWebhook(webhook.Id ?? 0, newWebhook);
                                webhook.Enabled = Enable.Value;
                            }
                            catch (Exception ex)
                            {
                                string errorId = $"{(Enable.Value ? "Enable" : "Disable")}WebhookError";
                                WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, webhook));
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
        //                .Where(e => !e.Enabled.GetValueOrDefault())
        //                .FilterByWildcards(e => e.Name!, wpName)
        //                .OrderBy(e => e.Name))
        //            {
        //                cancelHandler.Token.ThrowIfCancellationRequested();

        //                if (ShouldProcess(webhook.GetPSPath(), "Enable Webhook"))
        //                {
        //                    try
        //                    {
        //                        var newWebhook = new Webhook()
        //                        {
        //                            Enabled = true
        //                        };
        //                        drive!.OrchAPISession.PatchWebhook(webhook.Id ?? 0, newWebhook);
        //                        webhook.Enabled = true;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(webhook.GetPSPath(), ex), "EnableWebhookError", ErrorCategory.InvalidOperation, webhook));
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
