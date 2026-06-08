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
