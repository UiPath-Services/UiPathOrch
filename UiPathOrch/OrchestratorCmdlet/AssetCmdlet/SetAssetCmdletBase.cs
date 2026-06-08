using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shared base for the Set-Orch{Asset,CredentialAsset,SecretAsset} cmdlets.
// Holds the pieces that are byte-identical across all three and independent of
// the per-cmdlet parameter-row type: the description-merge accumulator and the
// in-memory pending-asset map keyed by (name, folder PSPath). The richer
// Credential/Secret-specific scaffolding lives one level down (those two are
// near-twins); plain Set-OrchAsset only needs this base.
public abstract class SetAssetCmdletBase : OrchestratorPSCmdlet
{
    // Description resolved across all input rows for a given (name, folder),
    // applied to the asset in EndProcessing. See OrchExtensions.MergeNonEmptyValue
    // for the merge spec (non-empty > "" > null), which preserves the common
    // CSV-export shape where Description is written on the first row only and
    // empty cells follow.
    protected readonly Dictionary<(string name, string path), string> _resolvedDescriptions = [];

    protected void MergeDescription(string name, string folderPath, string? rowDescription)
        => _resolvedDescriptions.MergeNonEmptyValue((name, folderPath), rowDescription);

    // Assets built or updated in memory during this invocation, keyed by
    // (name, folder PSPath), flushed (POST / PUT / DELETE) in EndProcessing.
    protected readonly Dictionary<(string name, string path), Asset> pendingAssets = [];
}

// Shared row shape for the Credential/Secret twins. The generic base below owns the
// per-invocation parameter list and the bulk asset prefetch through this interface,
// without needing to know the value-field shape — Credential adds
// CredentialUsername/CredentialPassword, Secret adds SecretValue on top.
public interface ISetAssetRow
{
    string[]? Name { get; set; }
    string? Description { get; set; }
    string? CredentialStore { get; set; }
    string[]? UserName { get; set; }
    string[]? MachineName { get; set; }
    string? ExternalName { get; set; }
    string[]? Path { get; set; }
}

// Base for the two near-twin secure-asset cmdlets (Set-OrchCredentialAsset /
// Set-OrchSecretAsset). Plain Set-OrchAsset is the outlier and stays on
// SetAssetCmdletBase directly. This layer holds the pieces that are byte-identical
// across the twins and depend only on the shared row shape: the parameter-row list
// and the bulk asset prefetch. The value-specific logic (UpdateAssetInMemory /
// BuildAssetDataFromParameterSets / EndProcessing) stays in the derived cmdlets for
// now and is folded down in later slices.
public abstract class SetCredentialLikeAssetCmdletBase<TParam> : SetAssetCmdletBase
    where TParam : ISetAssetRow
{
    // Parameter rows accumulated in ProcessRecord, consumed in EndProcessing.
    protected readonly List<TParam> parameters = [];

    // Prefetch every target folder's assets in bulk so the per-row resolution in
    // BuildAssetDataFromParameterSets runs against warm caches.
    protected void RetrieveAllAssets()
    {
        ParallelResults.GroupBy(parameters, param =>
        {
            var drivesFolders = SessionState.EnumFolders(param.Path);

            // Since the path is already resolved, only one folder should be expanded, but iterate just in case
            return ParallelResults.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                return drive.Assets.Get(folder);
            }).SelectMany(g => g);
        }).ToList();
    }
}
