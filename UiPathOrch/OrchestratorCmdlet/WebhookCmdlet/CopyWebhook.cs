using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchWebhook", SupportsShouldProcess = true)]
public class CopyWebhookCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    internal static void CopyWebhooks(
        IWritableHost _this,
        OrchDriveInfo srcDrive,
        List<WildcardPattern>? wpName,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        srcDrive.Webhooks.ClearCache();

        // This implementation is fine as is.
        ICollection<Webhook>? srcWebhooks = null;
        try
        {
            srcWebhooks = srcDrive.Webhooks.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (srcWebhooks is null) return;

        foreach (var dstDrive in dstDrives)
        {
            foreach (var srcWebhook in srcWebhooks
                .FilterByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name))
            {
                cancelToken.ThrowIfCancellationRequested();

                string item = srcWebhook.GetPSPath();
                string destination = dstDrive.NameColonSeparator;
                if (shouldProcess || _this.ShouldProcess($"Item: {item} Destination: {destination}", "Copy Webhook"))
                {
                    try
                    {
                        var newWebhook = OrchCollectionExtensions.DeepCopy(srcWebhook);
                        newWebhook.Key = null;
                        newWebhook.Id = null;
                        // newWebhook.Path = null; // Not needed since it has the JsonIgnore attribute

                        // The server returns Secret either masked or as-is depending on version. Either
                        // way, POSTing it to the destination would write a broken value (the masked
                        // string becomes the literal secret). Drop it on copy and rely on the warning
                        // below to nudge the operator to re-supply via Update-OrchWebhook.
                        bool bSecretExists = !string.IsNullOrEmpty(newWebhook.Secret);
                        if (bSecretExists)
                        {
                            newWebhook.Secret = null;
                        }

                        // Older Orchestrator versions (< v16) do not have a Name field.
                        // Generate a default name from the URL when copying to v16+.
                        if (dstDrive.OrchAPISession.ApiVersion >= 16
                            && string.IsNullOrEmpty(newWebhook.Name) && !string.IsNullOrEmpty(newWebhook.Url))
                        {
                            try { newWebhook.Name = new Uri(newWebhook.Url).Host; }
                            catch { newWebhook.Name = "webhook"; }
                        }

                        var createdWebhook = dstDrive.OrchAPISession.CreateWebhook(newWebhook);
                        if (createdWebhook is not null)
                        {
                            dstDrive.Webhooks.ClearCache();
                            createdWebhook.Path = dstDrive.NameColonSeparator;
                            //WriteObject(createdWebhook);

                            if (bSecretExists)
                            {
                                _this.WriteWarning($"'{createdWebhook.GetPSPath()}': Please update the webhook Secret with Update-OrchWebhook cmdlet.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(item, ex), "CreateCalendarError", ErrorCategory.InvalidOperation, destination));
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetOrchDrive(Path!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();
        //var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyWebhooks(this, srcDrive, wpName, dstDrives, false, cancelHandler.Token);
    }
}
