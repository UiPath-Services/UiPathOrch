using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Reads the connected user's notification subscriptions, flattened to one row per
// (topic, mode). Self-only: the notification service uses the token's user, so there
// is no -UserName. Output groups by publisher in the default view.
[Cmdlet(VerbsCommon.Get, "PmNotificationSubscription")]
[OutputType(typeof(PmNotificationSubscription))]
public class GetPmNotificationSubscriptionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmNotificationPublisherCompleter))]
    [SupportsWildcards]
    public string[]? Publisher { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmNotificationModeCompleter))]
    public string[]? Mode { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter IncludeHidden { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpPublisher = Publisher.ConvertToWildcardPatternList();
        var modeFilter = (Mode is { Length: > 0 })
            ? new HashSet<string>(Mode, StringComparer.OrdinalIgnoreCase)
            : null;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
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
            if (resp?.publishers is null) continue;

            foreach (var pub in resp.publishers
                .Where(p => p is not null)
                .FilterByWildcards(p => p!.displayName ?? p.name ?? "", wpPublisher)
                .OrderBy(p => p!.displayName ?? p.name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                if (pub!.topics is null) continue;

                foreach (var topic in pub.topics.Where(t => t is not null && (IncludeHidden || t.isVisible != false)))
                {
                    foreach (var m in topic!.modes ?? [])
                    {
                        if (modeFilter is not null && (m?.name is null || !modeFilter.Contains(m.name))) continue;

                        WriteObject(new PmNotificationSubscription
                        {
                            Path = drive.NameColonSeparator,
                            PathPublisher = drive.NameColonSeparator + (pub.displayName ?? pub.name),
                            Publisher = pub.displayName ?? pub.name,
                            Group = topic.group,
                            Topic = topic.name,
                            DisplayName = topic.displayName,
                            Category = topic.category,
                            Mode = m!.name,
                            IsSubscribed = m.isSubscribed,
                            IsMandatory = topic.isMandatory,
                            TopicId = topic.id,
                        });
                    }
                }
            }
        }
    }
}
