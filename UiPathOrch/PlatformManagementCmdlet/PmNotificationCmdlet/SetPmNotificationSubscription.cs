using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Subscribes/unsubscribes the connected user to a notification topic for a given mode
// (InApp / Email). Self-only. Each pipeline row is one (topic, mode); rows for the same
// drive are accumulated and sent as a single request. -Topic accepts the topic name
// (e.g. "Apps.Shared") or its GUID; names are resolved per drive via the topic list.
[Cmdlet(VerbsCommon.Set, "PmNotificationSubscription", SupportsShouldProcess = true)]
[OutputType(typeof(PmNotificationSubscription))]
public class SetPmNotificationSubscriptionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmNotificationTopicCompleter))]
    public string? Topic { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmNotificationModeCompleter))]
    public string? Mode { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("IsSubscribed")]
    public bool Subscribed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private sealed record Row(string Topic, string Mode, bool Subscribed);

    // drive -> accumulated (topic, mode, subscribed), in input order.
    private Dictionary<OrchDriveInfo, List<Row>>? _pending;

    protected override void ProcessRecord()
    {
        if (string.IsNullOrEmpty(Topic) || string.IsNullOrEmpty(Mode)) return;

        _pending ??= [];

        foreach (var drive in SessionState.EnumPmDrives(Path))
        {
            if (!_pending.TryGetValue(drive, out var list))
            {
                list = [];
                _pending[drive] = list;
            }
            list.Add(new Row(Topic, Mode, Subscribed));
        }
    }

    protected override void EndProcessing()
    {
        if (_pending is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, rows) in _pending)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            string? partitionGlobalId;
            try
            {
                partitionGlobalId = drive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (string.IsNullOrEmpty(partitionGlobalId)) continue;

            // One topic-list read per drive, to resolve names -> GUIDs.
            PmNotificationSubscriptionResponse? resp;
            try
            {
                resp = drive.OrchAPISession.GetUserSubscriptions(partitionGlobalId);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmNotificationSubscriptionError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            var topicPairs = (resp?.publishers ?? [])
                .Where(p => p?.topics is not null)
                .SelectMany(p => p!.topics!
                    .Where(t => t is not null && !string.IsNullOrEmpty(t.id))
                    .Select(t => (pub: p, topic: t!)))
                .ToList();

            var items = new List<UserSubscriptionItem>();
            var confirmations = new List<PmNotificationSubscription>();

            foreach (var row in rows)
            {
                string target = $"{drive.NameColonSeparator} {row.Topic} ({row.Mode}) = {row.Subscribed}";

                var match = Guid.TryParse(row.Topic, out _)
                    ? topicPairs.FirstOrDefault(p => string.Equals(p.topic.id, row.Topic, StringComparison.OrdinalIgnoreCase))
                    : topicPairs.FirstOrDefault(p => string.Equals(p.topic.name, row.Topic, StringComparison.OrdinalIgnoreCase));
                if (match.topic is null && !Guid.TryParse(row.Topic, out _))
                {
                    match = topicPairs.FirstOrDefault(p => string.Equals(p.topic.displayName, row.Topic, StringComparison.OrdinalIgnoreCase));
                }

                var topic = match.topic;
                if (topic is null)
                {
                    WriteError(new ErrorRecord(new OrchException(target, $"Notification topic '{row.Topic}' was not found in '{drive.NameColonSeparator}'."), "PmNotificationTopicNotFound", ErrorCategory.ObjectNotFound, target));
                    continue;
                }

                string? publisher = match.pub?.displayName ?? match.pub?.name;
                items.Add(new UserSubscriptionItem { topicId = topic.id, isSubscribed = row.Subscribed, notificationMode = row.Mode });
                confirmations.Add(new PmNotificationSubscription
                {
                    Path = drive.NameColonSeparator,
                    PathPublisher = drive.NameColonSeparator + publisher,
                    Publisher = publisher,
                    Group = topic.group,
                    Topic = topic.name,
                    DisplayName = topic.displayName,
                    Category = topic.category,
                    Mode = row.Mode,
                    IsSubscribed = row.Subscribed,
                    IsMandatory = topic.isMandatory,
                    TopicId = topic.id,
                });
            }

            if (items.Count == 0) continue;

            if (ShouldProcess(drive.NameColonSeparator, "Set PmNotificationSubscription"))
            {
                try
                {
                    drive.OrchAPISession.UpdateUserSubscriptions(partitionGlobalId, new UpdateUserSubscriptionPayload { userSubscriptions = items });
                    foreach (var c in confirmations) WriteObject(c);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SetPmNotificationSubscriptionError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
