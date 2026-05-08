using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchBucketLink", SupportsShouldProcess = true)]
public class AddBucketLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
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

        // Parallel prefetch warms the per-folder Buckets cache.
        Parallel.ForEach(drivesFolders, df =>
        {
            try { df.drive.Buckets.Get(df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AddBucketLink prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            ICollection<Bucket> buckets;
            try
            {
                buckets = drive.Buckets.Get(folder);
            }
            catch (Exception ex)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "AddBucketLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            // Cross-drive linking is not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var bucket in buckets
                .FilterByWildcards(b => b?.Name, wpName)
                .OrderBy(b => b.Name).WithCancellation(cancelHandler.Token))
            {
                // Batch all targets for this (folder, bucket) into a single API call.
                var toAddIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)
                    .Distinct()
                    .ToList();
                if (toAddIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, bucket.Name!);
                string action = $"Add BucketLink → {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    drive.OrchAPISession.ShareBucketsToFolders(
                        folder.Id ?? 0,
                        new List<Int64> { bucket.Id ?? 0 },
                        toAddIds,
                        new List<Int64>());
                    drive.ClearBucketLinkCache(bucket.Id ?? 0);
                    foreach (var dl in sameDriveLinks.Where(dl => toAddIds.Contains(dl.folder.Id ?? 0)))
                    {
                        drive.Buckets.ClearCache(dl.folder);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddBucketLinkError",
                        ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
