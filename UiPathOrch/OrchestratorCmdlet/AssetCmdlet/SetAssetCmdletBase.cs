using System.Management.Automation;
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

    // Build or update one asset in memory from a single parameter row, returning the asset
    // when something actually changed (dirty) or null otherwise. The control flow is shared
    // across the Credential/Secret twins; the value-field differences are isolated in the
    // abstract hooks below.
    protected Asset? UpdateAssetInMemory(OrchDriveInfo drive, Folder folder, string name, TParam param,
        IEnumerable<User>? specifiedUsers,
        IEnumerable<ExtendedMachine?> specifiedMachines,
        Int64 credentialStoreId)
    {
        string target = System.IO.Path.Combine(folder.GetPSPath(), param.Name?[0] ?? "");
        bool isDirty = false;

        pendingAssets.TryGetValue((name, folder.GetPSPath()), out var asset);
        if (asset is null)
        {
            var assets = drive.Assets.Get(folder);
            asset = assets.FirstOrDefault(a => a.Name == name);
            if (asset is null)
            {
                if (!HasCreateValue(param))
                    return null;

                isDirty = true;
                // Create a new asset in memory. Description is intentionally omitted here;
                // it's resolved across all input rows via MergeDescription and applied in
                // EndProcessing before POST.
                asset = new Asset
                {
                    Name = name,
                    ValueScope = "Global",
                    ValueType = ValueType,
                    CanBeDeleted = true,
                    HasDefaultValue = false,
                    Path = folder.GetPSPath(),
                };
                InitializeNewAsset(asset);
            }
            else
            {
                asset = OrchCollectionExtensions.DeepCopy(asset);
                asset.Path = folder.GetPSPath();
            }
        }

        // Description is merged across all input rows in MergeDescription (priority:
        // non-empty > "" > null) and applied to the asset in EndProcessing. We mark dirty
        // whenever the user supplied a Description on this row, so the asset reaches POST
        // even when Description is the only change.
        MergeDescription(name, folder.GetPSPath(), param.Description);
        if (param.Description is not null) isDirty = true;

        if (asset.CredentialStoreId != credentialStoreId && credentialStoreId != 0)
        {
            isDirty = true;
            asset.CredentialStoreId = credentialStoreId;
            if (asset.UserValues is not null)
            {
                foreach (var userValue in asset.UserValues)
                {
                    userValue.CredentialStoreId = credentialStoreId;
                }
            }
        }

        // Update Global value
        if (specifiedUsers is null)
        {
            if (specifiedMachines is not null && specifiedMachines.Any(m => m is not null))
            {
                // Warning that machines will be ignored
                string strMachineNames = string.Join(", ", param.MachineName!);
                var errorRecord = new ErrorRecord(new OrchException(target, $"UserName is not specified. MachineName '{strMachineNames}' ignored."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                WriteError(errorRecord);
            }

            if (ApplyGlobalValue(asset, param)) isDirty = true;
        }
        else // Update PerRobot value
        {
            // If CredentialStore is specified, update the CredentialStoreId for all UserValues
            if (credentialStoreId != 0 && asset.UserValues is not null)
            {
                foreach (var uv in asset.UserValues)
                {
                    if (uv.CredentialStoreId != credentialStoreId)
                    {
                        isDirty = true;
                        uv.CredentialStoreId = credentialStoreId;
                    }
                }
            }

            // Use Dictionary for O(1) lookup by (UserId, MachineId)
            var uvDict = (asset.UserValues ?? []).ToDictionary(uv => (uv.UserId, uv.MachineId));

            foreach (var user in specifiedUsers)
            {
                foreach (var machine in specifiedMachines!)
                {
                    var key = (user.Id, machine?.Id);
                    uvDict.TryGetValue(key, out var userValue);

                    if (IsPerRobotValueEmpty(param))
                    {
                        // No per-robot value on this row. Credential clears an existing entry;
                        // Secret leaves it untouched (use Remove-OrchAsset to drop a Secret UserValue).
                        if (AllowPerRobotRemoval && userValue is not null)
                        {
                            isDirty = true;
                            uvDict.Remove(key);
                        }
                        continue;
                    }
                    if (userValue is null)
                    {
                        isDirty = true;
                        userValue = new AssetUserValue
                        {
                            ValueType = ValueType,
                            UserId = user.Id,
                            UserName = user.UserName,
                            MachineId = machine?.Id,
                            MachineName = machine?.Name,
                            CredentialStoreId = asset.CredentialStoreId
                        };
                        uvDict[key] = userValue;
                        asset.ValueScope = "PerRobot";
                    }

                    if (ApplyPerRobotValue(userValue, param)) isDirty = true;
                }
            }

            // Write back to asset.UserValues
            if (uvDict.Count > 0)
                asset.UserValues = uvDict.Values.ToList();
            else
            {
                asset.ValueScope = "Global";
                asset.UserValues = null;
            }
        }

        return isDirty ? asset : null;
    }

    protected void BuildAssetDataFromParameterSets()
    {
        // For performance, retrieve all Assets for the target folders in advance
        RetrieveAllAssets();

        foreach (var param in parameters)
        {
            ApplyDefaultParameterSetValues(param);

            // expand Asset Name
            List<WildcardPattern> wpName = param.Name!.ConvertToWildcardPatternList();

            // expand UserName and MachineName
            List<WildcardPattern> wpUserName = null;
            List<WildcardPattern> wpMachineName = null;
            WildcardPattern wpCredentialStore = null;

            if (param.UserName is not null && param.UserName.Any(un => !string.IsNullOrEmpty(un)))
                wpUserName = param.UserName.ConvertToWildcardPatternList();

            if (param.MachineName is not null && param.MachineName.Any(mn => !string.IsNullOrEmpty(mn)))
                wpMachineName = param.MachineName.ConvertToWildcardPatternList();

            if (!string.IsNullOrEmpty(param.CredentialStore))
                wpCredentialStore = new WildcardPattern(param.CredentialStore, WildcardOptions.IgnoreCase);

            var drivesFolders = SessionState.EnumFolders(param.Path);
            foreach (var (drive, folder) in drivesFolders)
            {
                string targetFolder = $"{folder.GetPSPath()}";

                long credentialStoreId = FindCredentialStoreId(targetFolder, drive, wpCredentialStore)?.Id ?? 0;

                IEnumerable<User> specifiedUsers = null;
                IEnumerable<ExtendedMachine?> specifiedMachines = null;

                // expand UserName
                if (wpUserName is not null)
                {
                    // See SetAsset.cs UserName expansion — the same scope-tightening fix.
                    var assignedUserIds = drive.FolderUsersWithInherited.Get(folder)
                        .Where(ur => ur?.UserEntity?.Id is not null)
                        .Select(ur => ur.UserEntity!.Id!.Value)
                        .ToHashSet();
                    var tenantUsers = drive.Users.Get()
                        .Where(u => u.Type != "DirectoryGroup" && u.Id is not null && assignedUserIds.Contains(u.Id!.Value));
                    // Match both UserName and EmailAddress; see SetAsset.cs.
                    specifiedUsers = tenantUsers.FilterByWildcardsAny(
                        [u => u?.UserName, u => u?.EmailAddress],
                        wpUserName);
                    if (!specifiedUsers.Any())
                    {
                        string strUserNames = string.Join(", ", param.UserName!);
                        Exception e = new Exception($"UserName '{strUserNames}' is not assigned to the folder '{folder.GetPSPath()}'.");
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, e), "SetAssetError", ErrorCategory.InvalidOperation, targetFolder);
                        WriteError(errorRecord);
                        continue;
                    }
                }

                // expand MachineName
                if (wpMachineName is not null)
                {
                    // See SetAsset.cs MachineName expansion — the same scope-tightening fix.
                    var assignedIds = drive.FolderMachinesAssigned.Get(folder)
                        .Where(m => m?.Id is not null)
                        .Select(m => m.Id!.Value)
                        .ToHashSet();
                    var tenantMachines = drive.Machines.Get()
                        .Where(m => m?.Id is not null && assignedIds.Contains(m.Id!.Value));
                    specifiedMachines = tenantMachines.FilterByWildcards(m => m?.Name, wpMachineName);
                    if (!specifiedMachines.Any())
                    {
                        string strMachineNames = string.Join(", ", param.MachineName!);
                        Exception e = new Exception($"MachineName '{strMachineNames}' is not assigned to the folder '{folder.GetPSPath()}'.");
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, e), "SetAssetError", ErrorCategory.InvalidOperation, targetFolder);
                        WriteError(errorRecord);
                        continue;
                    }
                }
                if (specifiedMachines is null || !specifiedMachines.Any())
                {
                    // For processing convenience, insert a single null element
                    specifiedMachines = [null];
                }

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType == ValueType);

                foreach (var name in param.Name!)
                {
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        // Update existing assets
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                            if (asset is not null)
                                pendingAssets.TryAdd((asset.Name!, asset.Path!), asset);
                        }
                    }
                    else
                    {
                        // Create a new asset
                        var asset = UpdateAssetInMemory(drive, folder, name, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                        if (asset is not null)
                            pendingAssets.TryAdd((asset.Name!, asset.Path!), asset);
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        BuildAssetDataFromParameterSets();

        // Apply the merged Description (resolved across all input rows) to each pending asset.
        // See MergeDescription / _resolvedDescriptions for the priority rule.
        foreach (var asset in pendingAssets.Values)
        {
            if (_resolvedDescriptions.TryGetValue((asset.Name!, asset.Path!), out var resolved)
                && asset.Description != resolved)
            {
                asset.Description = resolved;
            }
        }

        List<(OrchDriveInfo drive, Int64 id)> folderIdsThatShouldRemoveCache = [];

        using var reporter = new ProgressReporter(this, 1, pendingAssets.Count, ProgressActivity);

        // Process the grouped parameter sets
        try
        {
            int index = 0;
            foreach (var asset in pendingAssets.Values)
            {
                NormalizeBeforeFlush(asset);

                var drivesFolders = SessionState.EnumFolders([WildcardPattern.Escape(asset.Path!)]);
                var (drive, folder) = drivesFolders[0];

                var target = asset.GetPSPath();

                reporter.WriteProgress(++index);

                var existingAssets = drive.Assets.Get(folder);
                var existingAsset = existingAssets.FirstOrDefault(a => a.Name == asset.Name);

                try
                {
                    if (existingAsset is null) // Add a new asset
                    {
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            continue;
                        }
                        if (ShouldProcess(target, $"Add {ValueType}Asset"))
                        {
                            Asset createdAsset = drive.OrchAPISession.AddAsset(folder.Id ?? 0, asset);
                            createdAsset!.Path = folder.GetPSPath();
                            WriteObject(createdAsset);

                            folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                        }
                    }
                    else
                    {
                        // Delete the asset
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            if (ShouldProcess(target, $"Remove {ValueType}Asset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else // Update the asset
                        {
                            if (ShouldProcess(target, $"Update {ValueType}Asset"))
                            {
                                drive.OrchAPISession.PutAsset(folder.Id ?? 0, asset);
                            }
                        }

                        folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                    }
                }
                catch (Exception ex)
                {
                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                }
            }
        }
        finally
        {
            foreach (var cache in folderIdsThatShouldRemoveCache)
            {
                cache.drive.Assets.ClearCache(cache.id);
            }
        }
    }

    // ---- value-field hooks (Credential vs Secret) ----

    // "Credential" or "Secret"; stamped on freshly created Asset / AssetUserValue instances.
    protected abstract string ValueType { get; }

    // True when the row carries enough to justify creating a brand-new asset (a value or an
    // ExternalName); otherwise an unknown asset name is silently skipped.
    protected abstract bool HasCreateValue(TParam param);

    // Per-type seeding of a freshly created asset (Credential sets an empty CredentialUsername;
    // Secret needs nothing).
    protected abstract void InitializeNewAsset(Asset asset);

    // Apply the row's value to the asset's Global value. Returns true if it changed anything.
    protected abstract bool ApplyGlobalValue(Asset asset, TParam param);

    // True when the row has no per-robot value to apply (the delete/skip trigger).
    protected abstract bool IsPerRobotValueEmpty(TParam param);

    // Whether an empty per-robot row removes an existing UserValue (Credential) or leaves it
    // in place (Secret).
    protected abstract bool AllowPerRobotRemoval { get; }

    // Apply the row's value to a single per-robot UserValue. Returns true if it changed anything.
    protected abstract bool ApplyPerRobotValue(AssetUserValue userValue, TParam param);

    // Extract the secure value from the Default parameter set (PSCredential for Credential,
    // SecureString for Secret) into the row before expansion.
    protected abstract void ApplyDefaultParameterSetValues(TParam param);

    // Progress-bar activity text shown while flushing ("Updating credential assets").
    protected abstract string ProgressActivity { get; }

    // Per-type normalization applied to each pending asset just before the API flush
    // (Credential nulls an empty username and drops the store link when the password is blank;
    // Secret deliberately preserves the store link copied from the existing asset).
    protected abstract void NormalizeBeforeFlush(Asset asset);
}
