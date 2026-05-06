using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchQueueLink", SupportsShouldProcess = true)]
public class AddQueueLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Link { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var drivesLinks = SessionState.EnumFolders(Link).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        // Parallel prefetch warms the per-folder Queues cache.
        Parallel.ForEach(drivesFolders, df =>
        {
            try { df.drive.Queues.Get(df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AddQueueLink prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            ICollection<QueueDefinition> queues;
            try
            {
                queues = drive.Queues.Get(folder);
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "AddQueueLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            // Cross-drive linking is not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var queue in queues
                .FilterByWildcards(q => q?.Name, wpName)
                .OrderBy(q => q.Name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                // Batch all targets for this (folder, queue) into a single API call.
                var toAddIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)
                    .Distinct()
                    .ToList();
                if (toAddIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, queue.Name!);
                string action = $"Add QueueLink → {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    drive.OrchAPISession.ShareQueuesToFolders(
                        folder.Id ?? 0,
                        new List<Int64> { queue.Id ?? 0 },
                        toAddIds,
                        new List<Int64>());
                    drive._dicQueueLinks = null;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddQueueLinkError",
                        ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
