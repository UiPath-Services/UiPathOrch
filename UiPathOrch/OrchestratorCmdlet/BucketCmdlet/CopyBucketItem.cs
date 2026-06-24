using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Copies the FILES inside storage buckets directly from one drive to another, the bucket-file
// analogue of Copy-OrchQueueItem. `copy -Recurse` / Copy-OrchBucket copy the bucket *definition*
// only; this carries the contents. The transfer streams source -> destination through pre-signed
// blob URIs (see OrchAPISession.OpenBucketItemRead / WriteBucketItemFromStream) with NO local
// staging — unlike the Export-OrchBucketItem -> disk -> Import-OrchBucketItem round-trip, which
// remains the right tool for local backup / inspection / editing.
//
// The destination bucket must already exist (create it first with Copy-OrchBucket); a missing
// destination bucket is a warning + skip, never an implicit create. By default files land in the
// same-named bucket in the structurally-corresponding destination folder; -DestinationBucket
// retargets a single source bucket to a differently-named one.
[Cmdlet(VerbsCommon.Copy, "OrchBucketItem", SupportsShouldProcess = true)]
[OutputType(typeof(BlobFile))]
public class CopyBucketItemCmdlet : OrchestratorPSCmdlet
{
    // Name / FullPath / Destination are all mandatory and positional (0 / 1 / 2), matching the
    // Export-OrchBucketItem argument order. As a mutating cmdlet it follows the module convention
    // of an explicit selector — there is no silent "copy everything" default; pass * to mean all.
    // Making all three mandatory also removes any positional ambiguity (a 2-argument call can't
    // bind the destination into the FullPath slot).
    // Alias "Bucket" so a piped BlobFile binds -Name from its Bucket property:
    // Get-OrchBucketItem ... | Copy-OrchBucketItem -Destination <dst>.
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("Bucket")]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketFullPathCompleter))]
    [SupportsWildcards]
    public string[]? FullPath { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Destination { get; set; }

    [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationBucketNameCompleter))]
    public string? DestinationBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        // -DestinationBucket renames the target, which only makes sense for a single source
        // bucket. Across a -Recurse walk the rename target is ambiguous (which folder's bucket
        // would it apply to?), so reject the combination up front — mirrors the guard in
        // Import-OrchBucketItem ("-Recurse and -Name").
        if (!string.IsNullOrEmpty(DestinationBucket) && Recurse.IsPresent)
        {
            WriteError(new ErrorRecord(
                new PSArgumentException("-DestinationBucket cannot be combined with -Recurse: a single rename target is ambiguous across multiple folders. Copy one bucket at a time."),
                "DestinationBucketWithRecurse", ErrorCategory.InvalidArgument, DestinationBucket));
            return;
        }

        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(EffectivePath(Path, LiteralPath));
        var srcDrivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        var (dstDrive, dstRootFolder) = SessionState.ResolveToSingleFolder(Destination);
        var dstFolderCache = new Dictionary<string, Folder?>();

        // NOTE: no "same root folder -> bail" guard here (unlike Copy-OrchBucket). Because
        // -DestinationBucket can retarget files to a DIFFERENT bucket in the SAME folder, a
        // same-folder copy is legitimate. The only true no-op is a bucket copied onto itself,
        // which is detected per-bucket below (same drive + folder + bucket id).

        var wpName = Name.ConvertToWildcardPatternList();
        var wpFullPath = FullPath.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1000, int.MaxValue, "Copying bucket files...");

        foreach (var (_, srcFolder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
        {
            // Fetch source buckets FIRST: skip folders with nothing to copy before resolving the
            // destination, so empty source subfolders don't trigger a spurious "folder does not
            // exist" error from GetRelativeDstFolder. Mirrors Copy-OrchBucket's ordering.
            List<Bucket> srcBuckets;
            try
            {
                srcBuckets = srcDrive.Buckets.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetBucketError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }
            if (srcBuckets.Count == 0) continue;

            if (!string.IsNullOrEmpty(DestinationBucket) && srcBuckets.Count > 1)
            {
                WriteError(new ErrorRecord(
                    new PSArgumentException($"-DestinationBucket requires the source to resolve to a single bucket, but -Name matched {srcBuckets.Count} buckets in '{srcFolder.GetPSPath()}'. Narrow -Name."),
                    "DestinationBucketAmbiguous", ErrorCategory.InvalidArgument, DestinationBucket));
                return;
            }

            // The destination folders/buckets are expected to already exist (Copy-OrchBucket /
            // copy -Recurse lays them down first), so createIfMissing: false — we never create an
            // empty folder just to host files. A folder that has source buckets but no destination
            // counterpart surfaces GetRelativeDstFolder's "does not exist" error, which correctly
            // tells the user to copy the bucket definitions first.
            Folder? dstFolder = this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, createIfMissing: false, createCache: dstFolderCache);
            if (dstFolder is null) continue;

            List<Bucket> dstBuckets;
            try
            {
                dstBuckets = dstDrive.Buckets.Get(dstFolder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), ex), "GetBucketError", ErrorCategory.InvalidOperation, dstFolder));
                continue;
            }

            foreach (var srcBucket in srcBuckets.OrderBy(b => b.Name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string dstBucketName = string.IsNullOrEmpty(DestinationBucket) ? srcBucket.Name! : DestinationBucket!;
                var dstBucket = dstBuckets.FirstOrDefault(b => string.Equals(b.Name, dstBucketName, StringComparison.OrdinalIgnoreCase));
                if (dstBucket is null)
                {
                    WriteWarning($"'{srcBucket.GetPSPath()}': A bucket named '{dstBucketName}' doesn't exist in {dstFolder.GetPSPath()}. Create it first (e.g. Copy-OrchBucket).");
                    continue;
                }

                // Copying a bucket onto itself is a no-op: the read and write URIs resolve to the
                // same blob. Skip only this exact identity (same folder reference + same bucket id),
                // so a same-folder copy to a DIFFERENT bucket via -DestinationBucket still proceeds.
                if (srcFolder == dstFolder && srcBucket.Id == dstBucket.Id)
                {
                    WriteWarning($"'{srcBucket.GetPSPath()}': source and destination are the same bucket; nothing to copy.");
                    continue;
                }

                List<BlobFile> files;
                try
                {
                    files = srcDrive.BucketFiles.Get(srcFolder, srcBucket)
                        .Where(f => !string.IsNullOrEmpty(f?.FullPath))
                        .FilterByWildcards(f => f!.FullPath, wpFullPath)
                        .OrderBy(f => f.FullPath)
                        .ToList();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(srcBucket.GetPSPath(), ex), "GetBucketFileError", ErrorCategory.InvalidOperation, srcBucket));
                    continue;
                }
                if (files.Count == 0) continue;

                reporter.TotalNum = files.Count;
                reporter.Activity = $"Copying files to {dstBucket.GetPSPath()}";

                // When source and destination resolve to the SAME external storage object, copying a
                // file would stream it onto itself. Warn once per bucket (the check below is per-file).
                bool warnedSameTarget = false;

                int index = 0;
                foreach (var file in files.WithCancellation(cancelHandler.Token))
                {
                    reporter.WriteProgress(++index, file.FullPath!);

                    string target = $"Item: '{srcBucket.GetPSPath()}/{file.FullPath}' Destination: '{dstBucket.GetPSPath()}/{file.FullPath}'";
                    if (!ShouldProcess(target, "Copy BucketItem")) continue;

                    try
                    {
                        // Open the read on the SOURCE session and the write on the DESTINATION
                        // session so each side's proxy / SSL config applies; the body streams
                        // straight across with no local staging. The full in-bucket path is
                        // preserved on both ends (GetWriteUri keeps file.FullPath), so nested
                        // bucket folders survive — something Import-OrchBucketItem flattens.
                        var readAccess = srcDrive.OrchAPISession.GetBucketReadUri(srcFolder.Id!.Value, srcBucket.Id!.Value, file.FullPath!);
                        var writeAccess = dstDrive.OrchAPISession.GetBucketWriteUri(dstFolder.Id!.Value, dstBucket.Id!.Value, file.FullPath!);
                        if (writeAccess is null)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, new InvalidOperationException("The destination did not return a writable URI.")), "CopyBucketItemError", ErrorCategory.InvalidOperation, dstBucket));
                            continue;
                        }

                        // Two different Orchestrator buckets (even across tenants) can be backed by the
                        // same external storage; when the resolved read and write URIs point to the same
                        // physical object, copying would stream the file onto itself (pointless, and a
                        // concurrent read+write of one object). Skip it.
                        if (BlobUriHelper.SamePhysicalObject(readAccess?.Uri, writeAccess.Uri))
                        {
                            if (!warnedSameTarget)
                            {
                                WriteWarning($"'{srcBucket.GetPSPath()}': source and destination resolve to the same external storage; nothing to copy.");
                                warnedSameTarget = true;
                            }
                            continue;
                        }

                        using var readStream = srcDrive.OrchAPISession.OpenBucketItemRead(readAccess, cancelHandler.Token);
                        if (readStream is null)
                        {
                            WriteWarning($"'{srcBucket.GetPSPath()}/{file.FullPath}': the source returned no readable content. Skipping.");
                            continue;
                        }

                        dstDrive.OrchAPISession.WriteBucketItemFromStream(writeAccess, readStream.Stream, readStream.Length, readStream.ContentType, file.FullPath!, cancelHandler.Token);

                        dstDrive.BucketFiles.ClearCache(dstFolder, dstBucket.Id.Value);

                        // Emit the copied source file so the run can be inspected and, if desired,
                        // piped into Remove-OrchBucketItem for a copy-then-delete move.
                        WriteObject(file);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyBucketItemError", ErrorCategory.InvalidOperation, srcBucket));
                    }
                }
            }
        }
    }
}
