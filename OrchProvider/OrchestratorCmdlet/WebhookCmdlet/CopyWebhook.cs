using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchWebhook", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Webhook))]
    public class CopyWebhookCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(WebhookNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        [SupportsWildcards]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string? Path { get; set; }

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.Name_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.Name_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!);
            if (srcDrive == null)
                throw new Exception("Path is not OrchDrive.");

            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            srcDrive._dicWebhooks = null;
            srcDrive._dicWebhooks_Exceptions.ClearCache();

            // この実装はこれで良い。
            ICollection<Webhook>? srcWebhooks = null;
            try
            {
                srcWebhooks = srcDrive.GetWebhooks();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }
            if (srcWebhooks == null) return;


            foreach (var dstDrive in dstDrives)
            {
                foreach (var srcWebhook in srcWebhooks
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    string item = srcWebhook.GetPSPath();
                    string destination = dstDrive.NameColonSeparator;
                    if (ShouldProcess($"Item: {item} Destination: {destination}", "Copy Webhook"))
                    {
                        try
                        {
                            var newWebhook = OrchCollectionExtensions.DeepCopy(srcWebhook);
                            newWebhook.Key = null;
                            newWebhook.Id = null;
                            // newWebhook.Path = null; // JsonIgnore 属性がついているので不要
                            var createdWebhook = dstDrive.OrchAPISession.CreateWebhook(newWebhook);
                            if (createdWebhook != null)
                            {
                                dstDrive._dicWebhooks = null;
                                createdWebhook.Path = dstDrive.NameColonSeparator;
                                //WriteObject(createdWebhook);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(item, ex), "CreateCalendarError", ErrorCategory.InvalidOperation, destination));
                        }
                    }
                }
            }
        }
    }
}
