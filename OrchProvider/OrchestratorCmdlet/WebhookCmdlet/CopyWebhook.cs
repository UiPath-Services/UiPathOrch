using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchWebhook", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Webhook))]
public class CopyWebhookCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = OrchDriveInfo.GetOrchDrive(Path!);
        if (srcDrive is null)
            throw new Exception("Path is not OrchDrive.");

        var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();
        //var wpName = Name.ConvertToWildcardPatternList();

        srcDrive.Webhooks.ClearCache();

        // この実装はこれで良い。
        ICollection<Webhook>? srcWebhooks = null;
        try
        {
            srcWebhooks = srcDrive.Webhooks.Get();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetWebhookError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (srcWebhooks is null) return;


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
                        if (createdWebhook is not null)
                        {
                            dstDrive.Webhooks.ClearCache();
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
