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
        var wpName = Name.ConvertToWildcardPatternList();
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
                .FilterByWildcards(b => b?.Name, wpName)
                .OrderBy(b => b.Name).WithCancellation(cancelHandler.Token))
            {
                string target = bucket.GetPSPath();

                // Deep copy the existing entity, then layer the user's overrides on top
                // so the PUT payload preserves all server-known fields. Password is
                // write-only on the server and never returned by GET, so the deep copy
                // has Password = null and only the user-supplied value is sent.
                Bucket newBucket = OrchCollectionExtensions.DeepCopy(bucket);

                // Resolve the credential-store name -> id up front (the only API round-trip in this
                // block), then hand the resolved value to the pure, API-free change-detection core so
                // the "only write when something changes" decision is unit-testable in isolation.
                bool credentialStoreResolved = false;
                long? resolvedCredentialStoreId = null;
                if (!string.IsNullOrEmpty(CredentialStore))
                {
                    var found = FindCredentialStoreId(target, drive, wpCredentialStore);
                    if (found is not null)
                    {
                        credentialStoreResolved = true;
                        resolvedCredentialStoreId = found.Id;
                    }
                }

                var inputs = new BucketUpdateInputs
                {
                    NewName = NewName,
                    Description = Description,
                    StorageProvider = StorageProvider,
                    StorageParameters = StorageParameters,
                    StorageContainer = StorageContainer,
                    ExternalName = ExternalName,
                    Password = Password,
                    Options = Options,
                    Tags = Tags,
                    CredentialStoreResolved = credentialStoreResolved,
                    ResolvedCredentialStoreId = resolvedCredentialStoreId,
                };

                bool dirty = ComputeBucketUpdate(newBucket, bucket, inputs);

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

    /// <summary>
    /// Pure inputs for <see cref="ComputeBucketUpdate"/>. The only API round-trip (credential-store
    /// name -> id) is resolved by the cmdlet first and passed in via
    /// <see cref="CredentialStoreResolved"/> / <see cref="ResolvedCredentialStoreId"/>, so change
    /// detection is fully testable without a live Orchestrator.
    /// </summary>
    internal sealed class BucketUpdateInputs
    {
        public string? NewName { get; init; }
        public string? Description { get; init; }
        public string? StorageProvider { get; init; }
        public string? StorageParameters { get; init; }
        public string? StorageContainer { get; init; }
        public string? ExternalName { get; init; }
        public string? Password { get; init; }
        public string[]? Options { get; init; }
        public string[]? Tags { get; init; }
        /// <summary>True when -CredentialStore resolved to exactly one store (mirrors the former "found is not null" guard).</summary>
        public bool CredentialStoreResolved { get; init; }
        /// <summary>The resolved credential-store id (may be null); only applied when <see cref="CredentialStoreResolved"/> and it differs from the current value.</summary>
        public long? ResolvedCredentialStoreId { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a deep copy of
    /// <paramref name="source"/>) and returns whether anything actually changed versus
    /// <paramref name="source"/> (the current bucket). Only a real difference flips the result true,
    /// so the caller can skip the PUT when the request is a no-op. Password is write-only (never
    /// returned by GET) so a supplied one always writes. No API access — unit-testable in isolation.
    /// </summary>
    internal static bool ComputeBucketUpdate(Bucket payload, Bucket source, BucketUpdateInputs input)
    {
        bool dirty = false;

        dirty |= payload.AssignStringIfNotNull(input.NewName, source, b => b.Name, (b, v) => b.Name = v);
        dirty |= payload.AssignStringIfNotNull(input.Description, source, b => b.Description, (b, v) => b.Description = v);
        dirty |= payload.AssignStringIfNotNull(input.StorageProvider, source, b => b.StorageProvider, (b, v) => b.StorageProvider = v);
        dirty |= payload.AssignStringIfNotNull(input.StorageParameters, source, b => b.StorageParameters, (b, v) => b.StorageParameters = v);
        dirty |= payload.AssignStringIfNotNull(input.StorageContainer, source, b => b.StorageContainer, (b, v) => b.StorageContainer = v);
        dirty |= payload.AssignStringIfNotNull(input.ExternalName, source, b => b.ExternalName, (b, v) => b.ExternalName = v);

        if (!string.IsNullOrEmpty(input.Password))
        {
            payload.Password = input.Password;
            dirty = true;
        }

        if (input.Options is not null)
        {
            string joined = string.Join(",", input.Options);
            if (joined != (source.Options ?? ""))
            {
                payload.Options = joined;
                dirty = true;
            }
        }

        if (input.CredentialStoreResolved && input.ResolvedCredentialStoreId != source.CredentialStoreId)
        {
            payload.CredentialStoreId = input.ResolvedCredentialStoreId;
            dirty = true;
        }

        var effectiveTags = input.Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
        if (effectiveTags is not null && effectiveTags.Length != 0)
        {
            // Only write when the tag set actually differs from the current one.
            dirty |= payload.AssignTags(effectiveTags, source, b => b.Tags, (b, v) => b.Tags = v);
        }

        return dirty;
    }
}
