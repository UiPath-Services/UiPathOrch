using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchBucket", SupportsShouldProcess = true)]
public class UpdateBucketCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<BucketStorageProviderItems>))]
    public string? StorageProvider { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StorageParameters { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StorageContainer { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string? CredentialStore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Password { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<BucketOptionsItems>))]
    public string[]? Options { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ExternalName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        WildcardPattern? wpCredentialStore = CredentialStore.ConvertToWildcardPattern();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<Bucket>? buckets = null;
            try
            {
                buckets = drive.Buckets.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetBucketError", ErrorCategory.InvalidOperation, folder));
            }
            if (buckets is null) continue;

            foreach (var bucket in buckets
                .FilterByNames(b => b?.Name, Name)
                .OrderBy(b => b.Name).WithCancellation(cancelHandler.Token))
            {
                string target = bucket.GetPSPath();

                // Deep copy the existing entity, then layer the user's overrides on top
                // so the PUT payload preserves all server-known fields. Password is
                // write-only on the server and never returned by GET, so the deep copy
                // has Password = null and only the user-supplied value is sent.
                Bucket newBucket = OrchCollectionExtensions.DeepCopy(bucket);
                bool dirty = false;

                dirty |= newBucket.AssignStringIfNotNull(NewName, bucket, b => b.Name, (b, v) => b.Name = v);
                dirty |= newBucket.AssignStringIfNotNull(Description, bucket, b => b.Description, (b, v) => b.Description = v);
                dirty |= newBucket.AssignStringIfNotNull(StorageProvider, bucket, b => b.StorageProvider, (b, v) => b.StorageProvider = v);
                dirty |= newBucket.AssignStringIfNotNull(StorageParameters, bucket, b => b.StorageParameters, (b, v) => b.StorageParameters = v);
                dirty |= newBucket.AssignStringIfNotNull(StorageContainer, bucket, b => b.StorageContainer, (b, v) => b.StorageContainer = v);
                dirty |= newBucket.AssignStringIfNotNull(ExternalName, bucket, b => b.ExternalName, (b, v) => b.ExternalName = v);

                if (!string.IsNullOrEmpty(Password))
                {
                    newBucket.Password = Password;
                    dirty = true;
                }

                if (Options is not null)
                {
                    string joined = string.Join(",", Options);
                    if (joined != (bucket.Options ?? ""))
                    {
                        newBucket.Options = joined;
                        dirty = true;
                    }
                }

                if (!string.IsNullOrEmpty(CredentialStore))
                {
                    var found = FindCredentialStoreId(target, drive, wpCredentialStore);
                    if (found is not null && found.Id != bucket.CredentialStoreId)
                    {
                        newBucket.CredentialStoreId = found.Id;
                        dirty = true;
                    }
                }

                var effectiveTags = Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                if (effectiveTags is not null && effectiveTags.Length != 0)
                {
                    newBucket.AssignTags(effectiveTags, (b, v) => b.Tags = v);
                    dirty = true;
                }

                if (!dirty) continue;

                if (ShouldProcess(target, "Update Bucket"))
                {
                    try
                    {
                        drive.OrchAPISession.PutBucket(folder.Id ?? 0, newBucket);
                        drive.Buckets.ClearCache(folder);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateBucketError", ErrorCategory.InvalidOperation, bucket));
                    }
                }
            }
        }
    }
}
