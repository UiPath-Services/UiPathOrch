namespace UiPath.OrchAPI;

// Central table of Orchestrator API version floors ("capabilities"). Each constant is the
// API version at which a behavior / DTO field / endpoint shape became available, replacing
// the magic-number comparisons that were scattered across OrchAPISession's per-endpoint
// methods. Thresholds are empirical (OData $metadata + POST probes per OC build); the call
// sites carry the verification notes. Centralizing them gives one documented place to adjust
// when a version is sunset, and names the intent at each branch instead of a bare number.
//
// Inclusion criterion (deliberately narrow): a floor earns a name here only when it is EITHER
//   (a) one capability checked in MORE THAN ONE place (a single source of truth — e.g. the
//       StaleRetention fields stripped by three queue methods and the release strip), OR
//   (b) an endpoint-shape ROUTING decision (which URL/action to POST to at a given version).
// A per-entity field strip that happens at one site is left as an inline `ApiVersion < N` with
// its empirical `// Fields added in vN` comment: those are NOT shared capabilities, and many
// distinct strips merely share a version number by coincidence (several different fields each
// "added in v15"). Folding them under a `V15`-style constant would be a magic number with a
// new name — misleading (it implies they are the same capability) and a worse read than the
// local comment. EnsureVersionSupport(N) guards and the per-cmdlet checks are likewise left
// inline. This table grows only when a genuinely shared/ routing floor appears.
internal static class OrchApiFloor
{
    // --- Queues ---
    public const double QueueV14Fields = 14;           // QueueDefinitionDto Tags / Encrypted / IsProcessInCurrentFolder / FoldersCount (absent v11-v13; live-confirmed Tags rejected on 21.10.4 / v13)
    public const double QueueCreateAction = 16;        // POST .../OData.CreateQueue (vs legacy POST /odata/QueueDefinitions)
    public const double QueueGetAction = 19;           // GET  .../OData.GetQueue(id=). The action itself answers from 16 (measured 200 on 23.4.0 and 25.10.2), but v16's QueueGetModel is not field-complete (CreationTime came back zeroed on a just-created queue), so the floor stays at the field-parity-verified value — do not lower on route existence alone.
    public const double QueueRetentionMerge = 16;      // merge QueueRetention into the legacy GET result
    public const double QueueRetryAbandonedItems = 18; // QueueDefinitionDto.RetryAbandonedItems field
    public const double QueueStaleRetention = 19;      // QueueDefinitionDto.StaleRetention* fields

    // --- Alerts ---
    public const double AlertsRemoved = 18;            // /odata/Alerts deleted from the API

    // --- Packages / Processes ---
    public const double PackageEntryPointMetadata = 12; // GetPackageMainEntryPoint / GetPackageEntryPoints return data (Not Found below); checked in both

    // --- Releases / Processes ---
    public const double ReleaseGetAction = 19;         // GET  .../OData.GetRelease
    public const double ReleaseCreateAction = 17;      // POST .../OData.CreateRelease (vs legacy POST /odata/Releases)
    public const double ReleaseCloudRetentionDefault = 19; // on Automation Cloud, default RetentionAction "Delete"/30d
    public const double ReleaseV19Fields = 19;         // EnvironmentVariables, MinRequiredRobotVersion, FolderKey, StaleRetention*, ProcessSettings.AutopilotForRobots
    public const double ReleaseV17Fields = 17;         // HiddenForAttendedUser, EntryPointPath
    public const double ReleaseV16Fields = 16;         // RemoteControlAccess, VideoRecordingSettings, AutomationHubIdeaUrl, RobotSize
    public const double ReleaseEntryPointId = 15;      // ReleaseDto.EntryPointId
    public const double ReleaseSpecificPriority = 14;  // SpecificPriorityValue accepted (else mapped to a JobPriority bucket)
    public const double ReleaseRetentionReadable = 17; // GET /odata/ReleaseRetention

    // Null-safe predicates.
    //
    // IMPORTANT: an UNKNOWN version (apiVersion == null) yields false for BOTH Supports and
    // Below, exactly matching the historic inline `ApiVersion >= N` and `ApiVersion < N`
    // comparisons (a nullable comparison is false when either operand is null). The two are
    // deliberately NOT negations of each other in the null case, so do not "simplify"
    // Below(v, f) to !Supports(v, f) — that would flip the unknown-version branch.
    public static bool Supports(double? apiVersion, double floor) => apiVersion >= floor;

    public static bool Below(double? apiVersion, double floor) => apiVersion < floor;
}
