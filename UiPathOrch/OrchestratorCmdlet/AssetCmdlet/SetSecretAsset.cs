using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

class SetSecretAssetCommandParameter
{
    public string[]? Name { set; get; }
    public string? Description { set; get; }
    public string? CredentialStore { set; get; }
    public string[]? UserName { set; get; }
    public string[]? MachineName { set; get; }
    public string? SecretValue { set; get; }
    public string? ExternalName { set; get; }
    public string[]? Path { set; get; }
}

[Cmdlet(VerbsCommon.Set, "OrchSecretAsset", DefaultParameterSetName = Default, SupportsShouldProcess = true)]
[OutputType(typeof(Asset))]
public class SetSecretAssetCommand : OrchestratorPSCmdlet
{
    private readonly List<SetSecretAssetCommandParameter> parameters = [];
    private readonly Dictionary<(string name, string path), Asset> pendingAssets = [];

    // See OrchExtensions.MergeNonEmptyValue for the merge spec.
    private readonly Dictionary<(string name, string path), string> _resolvedDescriptions = [];

    private void MergeDescription(string name, string folderPath, string? rowDescription)
        => _resolvedDescriptions.MergeNonEmptyValue((name, folderPath), rowDescription);

    private const string Default = "DefaultParameterSet";
    private const string Plain = "SpecifyPlainSecretParameterSet";

    [Parameter(ParameterSetName = Default, Position = 0, Mandatory = true)]
    [Parameter(ParameterSetName = Plain, Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ParameterSetName = Default, Position = 1)]
    [Parameter(ParameterSetName = Plain, Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(ParameterSetName = Default, Position = 2)]
    [Parameter(ParameterSetName = Plain, Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? MachineName { get; set; }

    [Parameter(ParameterSetName = Default, Mandatory = true, DontShow = true)]
    public SecureString? Secret { get; set; }

    [Parameter(ParameterSetName = Plain, Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(SecretValueCompleter))]
    public string? SecretValue { get; set; }

    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExternalNameCompleter))]
    public string? ExternalName { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DescriptionCompleter))]
    public string? Description { get; set; }

    [Parameter(ParameterSetName = Default)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string? CredentialStore { get; set; }

    [Parameter(ParameterSetName = Default)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType == "Secret")
                    .Where(a => wp.IsMatch(a.Name))
                    .ExcludeByWildcards(a => a?.Name, wpName))
                {
                    string tiphelp = TipHelp(asset);
                    yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, tiphelp);
                }

                var newAssetName = "New asset name here";
                if (wp.IsMatch(newAssetName))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(newAssetName), newAssetName, CompletionResultType.Text, newAssetName);
                }
            }
        }
    }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(u => u.UserEntity!.Type != "DirectoryGroup")
                    .Where(ur => wp.IsMatch(ur.UserEntity?.UserName))
                    .ExcludeByWildcards(ur => ur?.UserEntity?.UserName, wpUserName)
                    .OrderBy(ur => ur.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity!.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class MachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpMachineName = CreateSelfExclusionList(commandAst, "MachineName", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var machine in result
                    .Where(m => wp.IsMatch(m.Name))
                    .ExcludeByWildcards(m => m?.Name, wpMachineName)
                    .OrderBy(m => m.Name))
                {
                    string tiphelp = TipHelp(machine);
                    yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class SecretValueCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("'SecretValue here'");
        }
    }

    private class ExternalNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("'ExternalName here'");
        }
    }

    private class DescriptionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool bEmpty = true;
            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType == "Secret")
                    .FilterByWildcards(a => a?.Name, wpName))
                {
                    if (!string.IsNullOrEmpty(asset.Description))
                    {
                        bEmpty = false;
                        string tooltip = $" (current description of '{asset.Name}')";
                        yield return new CompletionResult(PathTools.EscapeNonWildcardText(asset.Description), asset.Description, CompletionResultType.Text, tooltip);
                    }
                }
            }

            if (bEmpty)
            {
                yield return new CompletionResult("'Description here'");
            }
        }
    }

    protected override void ProcessRecord()
    {
        SetSecretAssetCommandParameter parameter = new()
        {
            Name = Name,
            Description = Description,
            CredentialStore = CredentialStore,
            UserName = UserName,
            MachineName = MachineName,
            SecretValue = SecretValue,
            ExternalName = ExternalName,
            Path = Path
        };
        parameters.Add(parameter);
    }

    protected void RetrieveAllAssets()
    {
        ParallelResults.GroupBy(parameters, param =>
        {
            var drivesFolders = SessionState.EnumFolders(param.Path);
            return ParallelResults.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                return drive.Assets.Get(folder);
            }).SelectMany(g => g);
        }).ToList();
    }

    private Asset? UpdateAssetInMemory(OrchDriveInfo drive, Folder folder, string name, SetSecretAssetCommandParameter param,
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
                if (string.IsNullOrEmpty(param.SecretValue) && string.IsNullOrEmpty(param.ExternalName))
                    return null;

                isDirty = true;
                // Description is intentionally omitted here; it's resolved across all input
                // rows via MergeDescription and applied in EndProcessing before POST.
                asset = new Asset
                {
                    Name = name,
                    ValueScope = "Global",
                    ValueType = "Secret",
                    CanBeDeleted = true,
                    HasDefaultValue = false,
                    Path = folder.GetPSPath(),
                };
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

        if (specifiedUsers is null)
        {
            if (specifiedMachines is not null && specifiedMachines.Any(m => m is not null))
            {
                string strMachineNames = string.Join(", ", param.MachineName!);
                var errorRecord = new ErrorRecord(new OrchException(target, $"UserName is not specified. MachineName '{strMachineNames}' ignored."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                WriteError(errorRecord);
            }

            if (!string.IsNullOrEmpty(param.ExternalName))
            {
                isDirty = true;
                asset.ExternalName = param.ExternalName;
                asset.SecretValue = null;
                asset.HasDefaultValue = true;
            }
            else if (!string.IsNullOrEmpty(param.SecretValue))
            {
                isDirty = true;
                asset.SecretValue = param.SecretValue;
                asset.HasDefaultValue = true;
            }
        }
        else
        {
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

            var uvDict = (asset.UserValues ?? []).ToDictionary(uv => (uv.UserId, uv.MachineId));

            foreach (var user in specifiedUsers)
            {
                foreach (var machine in specifiedMachines!)
                {
                    var key = (user.Id, machine?.Id);
                    uvDict.TryGetValue(key, out var userValue);

                    // Both empty = "not specified" (e.g., CSV round-trip from Get-OrchSecretAsset).
                    // Skip silently so we do not clobber an existing secret with empty or delete
                    // the UserValue entry. To remove a UserValue, use Remove-OrchAsset.
                    if (string.IsNullOrEmpty(param.SecretValue) && string.IsNullOrEmpty(param.ExternalName))
                    {
                        continue;
                    }
                    if (userValue is null)
                    {
                        isDirty = true;
                        userValue = new AssetUserValue
                        {
                            ValueType = "Secret",
                            UserId = user.Id,
                            UserName = user.UserName,
                            MachineId = machine?.Id,
                            MachineName = machine?.Name,
                            CredentialStoreId = asset.CredentialStoreId
                        };
                        uvDict[key] = userValue;
                        asset.ValueScope = "PerRobot";
                    }

                    if (!string.IsNullOrEmpty(param.ExternalName))
                    {
                        isDirty = true;
                        userValue.ExternalName = param.ExternalName;
                        userValue.SecretValue = null;
                    }
                    else if (!string.IsNullOrEmpty(param.SecretValue))
                    {
                        isDirty = true;
                        userValue.SecretValue = param.SecretValue;
                    }
                }
            }

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
        RetrieveAllAssets();

        foreach (var param in parameters)
        {
            if (ParameterSetName == Default)
            {
                param.SecretValue = ConvertToUnsecureString(Secret!);
            }

            List<WildcardPattern> wpName = param.Name!.ConvertToWildcardPatternList();
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

                if (wpUserName is not null)
                {
                    // See SetAsset.cs UserName expansion — the same scope-tightening fix.
                    var assignedUserIds = drive.FolderUsersWithInherited.Get(folder)
                        .Where(ur => ur?.UserEntity?.Id is not null)
                        .Select(ur => ur.UserEntity!.Id!.Value)
                        .ToHashSet();
                    var tenantUsers = drive.GetUsers()
                        .Where(u => u.Type != "DirectoryGroup" && u.Id is not null && assignedUserIds.Contains(u.Id!.Value));
                    specifiedUsers = tenantUsers.FilterByWildcards(u => u?.UserName, wpUserName);
                    if (!specifiedUsers.Any())
                    {
                        string strUserNames = string.Join(", ", param.UserName!);
                        Exception e = new($"UserName '{strUserNames}' is not assigned to the folder '{folder.GetPSPath()}'.");
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, e), "SetAssetError", ErrorCategory.InvalidOperation, targetFolder);
                        WriteError(errorRecord);
                        continue;
                    }
                }

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
                        Exception e = new($"MachineName '{strMachineNames}' is not assigned to the folder '{folder.GetPSPath()}'.");
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, e), "SetAssetError", ErrorCategory.InvalidOperation, targetFolder);
                        WriteError(errorRecord);
                        continue;
                    }
                }
                if (specifiedMachines is null || !specifiedMachines.Any())
                {
                    specifiedMachines = [null];
                }

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType == "Secret");

                foreach (var name in param.Name!)
                {
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                            if (asset is not null)
                                pendingAssets.TryAdd((asset.Name!, asset.Path!), asset);
                        }
                    }
                    else
                    {
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

        // Apply the merged Description to each pending asset before POST.
        foreach (var asset in pendingAssets.Values)
        {
            if (_resolvedDescriptions.TryGetValue((asset.Name!, asset.Path!), out var resolved)
                && asset.Description != resolved)
            {
                asset.Description = resolved;
            }
        }

        List<(OrchDriveInfo drive, Int64 id)> folderIdsThatShouldRemoveCache = [];
        using var reporter = new ProgressReporter(this, 1, pendingAssets.Count, "Updating secret assets");

        try
        {
            int index = 0;
            foreach (var asset in pendingAssets.Values)
            {
                // Do NOT null CredentialStoreId when SecretValue is empty — the value is always
                // returned empty by the API (masked), so on update we must preserve the store link
                // copied from the existing asset.

                var drivesFolders = SessionState.EnumFolders([WildcardPattern.Escape(asset.Path!)]);
                var (drive, folder) = drivesFolders[0];

                var target = asset.GetPSPath();
                reporter.WriteProgress(++index);

                var existingAssets = drive.Assets.Get(folder);
                var existingAsset = existingAssets.FirstOrDefault(a => a.Name == asset.Name);

                try
                {
                    if (existingAsset is null)
                    {
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            continue;
                        }
                        if (ShouldProcess(target, "Add SecretAsset"))
                        {
                            Asset createdAsset = drive.OrchAPISession.AddAsset(folder.Id ?? 0, asset);
                            createdAsset!.Path = folder.GetPSPath();
                            WriteObject(createdAsset);
                            folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                        }
                    }
                    else
                    {
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            if (ShouldProcess(target, "Remove SecretAsset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else
                        {
                            if (ShouldProcess(target, "Update SecretAsset"))
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
}
