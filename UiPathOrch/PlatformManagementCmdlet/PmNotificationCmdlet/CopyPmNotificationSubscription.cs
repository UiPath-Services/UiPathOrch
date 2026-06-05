using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Copies the connected user's notification subscriptions from the source organization
// to the same person in each destination organization. Topics are matched by name
// (their GUIDs differ per organization); a topic absent from the destination, a
// mandatory destination topic, and a mode the destination topic doesn't offer are all
// skipped. Self-only: the service uses each drive's token user.
[Cmdlet(VerbsCommon.Copy, "PmNotificationSubscription", SupportsShouldProcess = true)]
[OutputType(typeof(PmNotificationSubscription))]
public class CopyPmNotificationSubscriptionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    private sealed record SrcState(string TopicName, string Mode, bool IsSubscribed);

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(EffectivePath(Path, LiteralPath)!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        string? srcPartition;
        try
        {
            srcPartition = srcDrive.GetPartitionGlobalId();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (string.IsNullOrEmpty(srcPartition)) return;

        PmNotificationSubscriptionResponse? srcResp;
        try
        {
            srcResp = srcDrive.OrchAPISession.GetUserSubscriptions(srcPartition);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmNotificationSubscriptionError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        // Flatten source to (topicName, mode, isSubscribed). Mandatory topics can't be
        // changed at the destination, so there's no point copying them.
        var srcStates = (srcResp?.publishers ?? [])
            .Where(p => p?.topics is not null)
            .SelectMany(p => p!.topics!)
            .Where(t => t is not null && !string.IsNullOrEmpty(t.name) && t.isMandatory != true)
            .SelectMany(t => (t!.modes ?? [])
                .Where(m => m?.name is not null && m.isSubscribed is not null)
                .Select(m => new SrcState(t.name!, m!.name!, m.isSubscribed!.Value)))
            .ToList();
        if (srcStates.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var dstDrive in dstDrives.WithCancellation(cancelHandler.Token))
        {
            string? dstPartition;
            try
            {
                dstPartition = dstDrive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, dstDrive));
                continue;
            }
            if (string.IsNullOrEmpty(dstPartition) || srcPartition == dstPartition) continue;

            PmNotificationSubscriptionResponse? dstResp;
            try
            {
                dstResp = dstDrive.OrchAPISession.GetUserSubscriptions(dstPartition);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "GetPmNotificationSubscriptionError", ErrorCategory.InvalidOperation, dstDrive));
                continue;
            }

            // Destination topics keyed by name; the topic GUIDs differ across orgs.
            var dstByName = (dstResp?.publishers ?? [])
                .Where(p => p?.topics is not null)
                .SelectMany(p => p!.topics!.Where(t => t is not null && !string.IsNullOrEmpty(t.name)).Select(t => (pub: p, topic: t!)))
                .GroupBy(x => x.topic.name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var items = new List<UserSubscriptionItem>();
            var confirmations = new List<PmNotificationSubscription>();

            foreach (var s in srcStates)
            {
                if (!dstByName.TryGetValue(s.TopicName, out var match)) continue;   // topic absent in dest
                var dstTopic = match.topic;
                if (dstTopic.isMandatory == true) continue;                         // can't change a mandatory topic
                if (dstTopic.modes is null || !dstTopic.modes.Any(m => string.Equals(m?.name, s.Mode, StringComparison.OrdinalIgnoreCase)))
                    continue;                                                       // mode not offered by dest topic

                string? publisher = match.pub?.displayName ?? match.pub?.name;
                items.Add(new UserSubscriptionItem { topicId = dstTopic.id, isSubscribed = s.IsSubscribed, notificationMode = s.Mode });
                confirmations.Add(new PmNotificationSubscription
                {
                    Path = dstDrive.NameColonSeparator,
                    PathPublisher = dstDrive.NameColonSeparator + publisher,
                    Publisher = publisher,
                    Group = dstTopic.group,
                    Topic = dstTopic.name,
                    DisplayName = dstTopic.displayName,
                    Category = dstTopic.category,
                    Mode = s.Mode,
                    IsSubscribed = s.IsSubscribed,
                    IsMandatory = dstTopic.isMandatory,
                    TopicId = dstTopic.id,
                });
            }

            if (items.Count == 0) continue;

            string target = $"Source: {srcDrive.NameColon} Destination: {dstDrive.NameColonSeparator}";
            if (ShouldProcess(target, "Copy PmNotificationSubscription"))
            {
                try
                {
                    dstDrive.OrchAPISession.UpdateUserSubscriptions(dstPartition, new UpdateUserSubscriptionPayload { userSubscriptions = items });
                    foreach (var c in confirmations) WriteObject(c);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyPmNotificationSubscriptionError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
