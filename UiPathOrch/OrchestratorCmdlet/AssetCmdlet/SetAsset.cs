using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

class SetAssetCommandParameter
{
    public string[]? Name { set; get; }
    public string? Description { set; get; }
    public string? ValueType { set; get; }
    public string? Value { set; get; }
    public string[]? UserName { set; get; }
    public string[]? MachineName { set; get; }
    public string[]? Path { set; get; }
}

[Cmdlet(VerbsCommon.Set, "OrchAsset", DefaultParameterSetName = Default, SupportsShouldProcess = true)]
[OutputType(typeof(Entities.Asset))]
public class SetAssetCmdlet : OrchestratorPSCmdlet
{
    private readonly List<SetAssetCommandParameter> parameters = [];

    // See OrchExtensions.MergeNonEmptyValue for the merge spec.
    private readonly Dictionary<(string name, string path), string> _resolvedDescriptions = [];

    private void MergeDescription(string name, string folderPath, string? rowDescription)
        => _resolvedDescriptions.MergeNonEmptyValue((name, folderPath), rowDescription);

    private readonly Dictionary<(string name, string path), Asset> pendingAssets = [];

    private const string Default = "DefaultParameterSet";

    // Single source of truth shared with the -ValueType tab-completer
    // (StaticTextsCompleter<AssetTypeItems>) so validation and completion can't drift.
    public static readonly string[] ValidValueTypes = AssetTypeItems.Items;

    [Parameter(ParameterSetName = Default, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<AssetTypeItems>))]
    public string? ValueType { get; set; }

    [Parameter(ParameterSetName = Default, Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ParameterSetName = Default, Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ValueCompleter))]
    public string? Value { get; set; }

    [Parameter(ParameterSetName = Default, Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(ParameterSetName = Default, Position = 4, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? MachineName { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DescriptionCompleter))]
    public string? Description { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Cannot be shared because non-existent assets are displayed as "New asset name here"
    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Extract the path from the parameters. If not specified, target the current directory
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Only target the ValueType selected by the parameter
            var wpValueType = GetFakeBoundParameters(fakeBoundParameters, "ValueType").ConvertToWildcardPatternList();

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => wp.IsMatch(a.ValueType))
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .ExcludeByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name))
                {
                    string tiphelp = asset.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, tiphelp);
                }
            }
            if (wordToComplete == "")
            {
                string newAssetName = "'New asset name here'";
                yield return new CompletionResult(newAssetName, newAssetName, CompletionResultType.Text, newAssetName);
            }
        }
    }

    private class DescriptionCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpValueType = GetFakeBoundParameters(fakeBoundParameters, "ValueType").ConvertToWildcardPatternList();
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpDescription = CreateSelfExclusionList(commandAst, "Description", wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool isEmpty = true;
            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType != "Credential" && a.ValueType != "Secret")
                    .Where(a => wp.IsMatch(a.Description))
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(a => a?.Name, wpName)
                    .ExcludeByWildcards(a => a?.Description, wpDescription))
                {
                    if (!string.IsNullOrEmpty(asset.Description))
                    {
                        isEmpty = false;
                        string tooltip = $" (current description of '{asset.Name}')";
                        yield return new CompletionResult(PathTools.EscapeNonWildcardText(asset.Description), asset.Description, CompletionResultType.Text, tooltip);
                    }
                }
            }
            if (isEmpty)
            {
                yield return new CompletionResult("'Description here'");
            }
        }
    }

    private class ValueCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpValueType = GetFakeBoundParameters(fakeBoundParameters, "ValueType").ConvertToWildcardPatternList();
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();
            var wpMachineName = GetFakeBoundParameters(fakeBoundParameters, "MachineName").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool bValueExists = false;
            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType != "Credential" && a.ValueType != "Secret")
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(a => a?.Name, wpName))
                {
                    if ((wpUserName is not null && wpUserName.Count != 0) || (wpMachineName is not null && wpMachineName.Count != 0))
                    {
                        var userValues = asset.UserValues?
                            .FilterByWildcards(uv => uv?.UserName, wpUserName)
                            .FilterByWildcards(uv => uv?.MachineName, wpMachineName) ?? [];
                        foreach (var userValue in userValues)
                        {
                            bValueExists = true;
                            string tiphelp = TipHelp(asset);
                            yield return new CompletionResult(PathTools.EscapePSText(userValue.Value), userValue.Value, CompletionResultType.Text, tiphelp);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(asset.Value))
                        {
                            bValueExists = true;
                            string tiphelp = TipHelp(asset);
                            yield return new CompletionResult(PathTools.EscapePSText(asset.Value), asset.Value, CompletionResultType.Text, tiphelp);
                        }
                    }
                }
            }

            if (!bValueExists)
            {
                string value = "New value here";
                yield return new CompletionResult(PathTools.EscapePSText(value), value, CompletionResultType.Text, value);
            }
        }
    }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude UserNames already selected by the parameter from the candidates
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);

            //// Only target Names already selected by the parameter
            //var wpName = CreateWPListFromOtherParameters(commandAst, "Name", positionalParams);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetFolderUsersUnion(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(u => u.UserEntity!.Type != "DirectoryGroup")
                    .Where(ur => wp.IsMatch(ur.UserEntity!.UserName))
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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude MachineNames already selected by the parameter from the candidates
            var wpMachineName = CreateSelfExclusionList(commandAst, "MachineName", wordToComplete);

            // TODO: Exclude existing user name and machine name combinations from the candidates
            // It's complicated, so let's skip it for now..

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var machine in result
                    .Where(m => wp.IsMatch(m.Name))
                    .ExcludeByWildcards(m => m?.Name, wpMachineName)
                    .OrderBy(m => m.Name))
                {
                    string tiphelp = machine.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        // check ValueType
        if (ValueType == "Credential" || ValueType == "Secret")
            return;

        SetAssetCommandParameter parameter = new()
        {
            Name = Name,
            Description = Description,
            ValueType = ValueType,
            Value = Value,
            UserName = UserName,
            MachineName = MachineName,
            Path = EffectivePath(Path, LiteralPath)
        };
        parameters.Add(parameter);
    }

    protected void RetrieveAllAssets()
    {
        // Retrieve Assets for the target folders asynchronously in bulk
        ParallelResults.GroupBy(parameters, param =>
        {
            var drivesFolders = SessionState.EnumFolders(param.Path);

            // Since the path is already resolved, only one folder should be expanded, but iterate just in case
            return ParallelResults.GroupBy(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                return drive.Assets.Get(folder);
            }).SelectMany(g => g);
        });
    }

    /// <summary>
    /// Assign a typed value to StringValue/BoolValue/IntValue properties.
    /// Returns true if the value was changed.
    /// </summary>
    private static bool AssignTypedValue(
        string? valueType, string? textValue, bool boolValue, int intValue,
        Func<string?> getStr, Action<string?> setStr,
        Func<bool?> getBool, Action<bool?> setBool,
        Func<int?> getInt, Action<int?> setInt)
    {
        switch (valueType)
        {
            case "Text":
                if (getStr() != textValue) { setStr(textValue); return true; }
                break;
            case "Bool":
                if (getBool() != boolValue) { setBool(boolValue); return true; }
                break;
            case "Integer":
                if (getInt() != intValue) { setInt(intValue); return true; }
                break;
        }
        return false;
    }

    private Asset? UpdateAssetInMemory(OrchDriveInfo drive, Folder folder, string name, SetAssetCommandParameter param,
        IEnumerable<User>? specifiedUsers,
        IEnumerable<ExtendedMachine?> specifiedMachines)
    {
        string target = System.IO.Path.Combine(folder.GetPSPath(), param.Name![0]);
        bool isDirty = false;

        pendingAssets.TryGetValue((name, folder.GetPSPath()), out var asset);
        if (asset is null)
        {
            var assets = drive.Assets.Get(folder);
            asset = assets.FirstOrDefault(a => a.Name == name);
            if (asset is null)
            {
                if (string.IsNullOrEmpty(param.Value))
                    return null;

                isDirty = true;
                // Description is intentionally omitted here; it's resolved across all input
                // rows via MergeDescription and applied in EndProcessing before POST.
                asset = new Asset
                {
                    Name = name,
                    ValueType = param.ValueType,
                    CanBeDeleted = true,
                    HasDefaultValue = false,
                    Path = folder.GetPSPath(),
                    ValueScope = "Global",
                };
                if (param.ValueType is null)
                    asset.ValueType = "Text";
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

        // respect existing asset valuetype
        if (string.IsNullOrEmpty(asset.ValueType) && asset.ValueType != param.ValueType)
        {
            isDirty = true;
            asset.ValueType = param.ValueType;
        }

        // When Value is not specified, do not update Value
        // (When Value is set to '', continue subsequent processing to delete the asset)
        if (param.Value is null)
        {
            if (isDirty)
                return asset;
            else
                return null;
        }

        bool boolValue = false;
        int intValue = 0;
        if (!string.IsNullOrEmpty(param.Value))
        {
            if (asset.ValueType == "Bool")
            {
                if (!bool.TryParse(param.Value!, out boolValue))
                {
                    var errorRecord = new ErrorRecord(new OrchException(target, $"Value {param.Value} cannot be parsed as bool."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                    return null;
                }
            }
            else if (asset.ValueType == "Integer")
            {
                if (!int.TryParse(param.Value!, out intValue))
                {
                    var errorRecord = new ErrorRecord(new OrchException(target, $"Value {param.Value} cannot be parsed as integer."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                    return null;
                }
            }
        }

        if (specifiedUsers is null)
            isDirty |= UpdateGlobalValue(asset, param, target, boolValue, intValue, specifiedMachines);
        else
            isDirty |= UpdatePerRobotValues(asset, param, target, boolValue, intValue, specifiedUsers, specifiedMachines!);

        return isDirty ? asset : null;
    }

    private bool UpdateGlobalValue(Asset asset, SetAssetCommandParameter param, string target,
        bool boolValue, int intValue, IEnumerable<ExtendedMachine?> specifiedMachines)
    {
        if (specifiedMachines is not null && specifiedMachines.Any(m => m is not null))
        {
            string strMachineNames = string.Join(", ", param.MachineName!);
            var errorRecord = new ErrorRecord(new OrchException(target, $"UserName was not specified. MachineName '{strMachineNames}' ignored."), "SetAssetError", ErrorCategory.InvalidOperation, target);
            WriteError(errorRecord);
        }

        if (param.Value == "" && !string.IsNullOrEmpty(asset.Value))
        {
            asset.ValueScope = "PerRobot";
            asset.HasDefaultValue = false;
            asset.StringValue = null;
            asset.BoolValue = null;
            asset.IntValue = null;
            return true;
        }

        if (param.Value is not null)
        {
            if (string.IsNullOrEmpty(asset.ValueType))
            {
                WriteWarning($"\"{target}\": ValueType was not specified. It will be assumed as 'Text'.");
                asset.ValueType = "Text";
            }

            if (AssignTypedValue(asset.ValueType, param.Value, boolValue, intValue,
                () => asset.StringValue, v => asset.StringValue = v,
                () => asset.BoolValue, v => asset.BoolValue = v,
                () => asset.IntValue, v => asset.IntValue = v))
            {
                asset.HasDefaultValue = true;
                return true;
            }
        }
        return false;
    }

    private bool UpdatePerRobotValues(Asset asset, SetAssetCommandParameter param, string target,
        bool boolValue, int intValue,
        IEnumerable<User> specifiedUsers, IEnumerable<ExtendedMachine?> specifiedMachines)
    {
        bool isDirty = false;

        // Use Dictionary for O(1) lookup by (UserId, MachineId)
        var uvDict = (asset.UserValues ?? []).ToDictionary(uv => (uv.UserId, uv.MachineId));

        foreach (var user in specifiedUsers)
        {
            foreach (var machine in specifiedMachines)
            {
                var key = (user.Id, machine?.Id);
                uvDict.TryGetValue(key, out var userValue);

                if (param.Value == "")
                {
                    if (userValue is not null)
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
                        UserId = user.Id,
                        UserName = user.UserName,
                        MachineId = machine?.Id,
                        MachineName = machine?.Name
                    };
                    uvDict[key] = userValue;
                }

                if ((userValue.ValueType ?? "") != asset.ValueType)
                {
                    isDirty = true;
                    userValue!.ValueType = asset.ValueType;
                }
                if (string.IsNullOrEmpty(asset.ValueType))
                    asset.ValueType = userValue?.ValueType;

                if (string.IsNullOrEmpty(asset.ValueType))
                {
                    WriteWarning($"\"{target}\": ValueType was not specified. It will be assumed as 'Text'.");
                    asset.ValueType = "Text";
                    userValue!.ValueType = "Text";
                }

                if (AssignTypedValue(asset.ValueType, param.Value, boolValue, intValue,
                    () => userValue!.StringValue, v => userValue!.StringValue = v,
                    () => userValue!.BoolValue, v => userValue!.BoolValue = v,
                    () => userValue!.IntValue, v => userValue!.IntValue = v))
                {
                    isDirty = true;
                    asset.ValueScope = "PerRobot";
                }
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

        return isDirty;
    }

    protected void BuildAssetDataFromParameterSets()
    {
        // For performance, retrieve all Assets for the target folders in advance
        RetrieveAllAssets();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var param in parameters)
        {
            if (!string.IsNullOrEmpty(param.ValueType) && !ValidValueTypes.Contains(param.ValueType))
            {
                string target;
                if (param.Path is not null && param.Path.Any())
                {
                    target = System.IO.Path.Combine(param.Path![0], param.Name?[0] ?? "");
                }
                else
                {
                    target = $"{param.Name?[0]}";
                }
                Exception e = new Exception(
                    $"ValueType '{param.ValueType}' is invalid (expected: {string.Join(", ", ValidValueTypes)}). "
                    + "Set-OrchAsset takes ValueType first, then Name (Set-OrchAsset <ValueType> <Name>); "
                    + "pass -Name explicitly if you meant an asset name.");
                var errorRecord = new ErrorRecord(new OrchException(target, e), "SetAssetError", ErrorCategory.InvalidOperation, target);
                WriteError(errorRecord);
                continue;
            }

            // expand UserName and MachineName
            List<WildcardPattern> wpUserName = null;
            List<WildcardPattern> wpMachineName = null;

            if (param.UserName is not null && param.UserName.Any(un => !string.IsNullOrEmpty(un)))
                wpUserName = param.UserName.ConvertToWildcardPatternList();

            if (param.MachineName is not null && param.MachineName.Any(mn => !string.IsNullOrEmpty(mn)))
                wpMachineName = param.MachineName.ConvertToWildcardPatternList();

            var drivesFolders = SessionState.EnumFolders(param.Path);
            foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
            {
                string targetFolder = $"{folder.GetPSPath()}";

                IEnumerable<User> specifiedUsers = null;
                IEnumerable<ExtendedMachine?> specifiedMachines = null;

                // expand UserName
                if (wpUserName is not null)
                {
                    // Folder-scope authorization is DELEGATED TO THE ORCHESTRATOR API here:
                    // resolve -UserName against the whole tenant user list, not a folder-assigned
                    // subset. Rationale (direct raw-PUT probes on cloud Orch1 and on-prem 22.10.1,
                    // 2026-07-09): the server accepts a per-User UserValue for ANY existing tenant
                    // user regardless of folder assignment — incl. group-/tenant-role-reachable
                    // users, and even a real DirectoryUser with zero access to the target folder
                    // (PUT -> 200, value persisted, Global Value intact). A non-existent user id is
                    // rejected with an atomic 400 that leaves the asset UNCHANGED. The old
                    // GetFolderUsersUnion filter (once kept for R9 / R13) was pure OVER-rejection:
                    // it blocked group-reachable users the server would accept, guarding an
                    // R13-era silent drop + Global-wipe that neither tested server exhibits.
                    // See PR #20. (The MachineName filter below is a SEPARATE axis and stays.)
                    //
                    // NB a directory user not yet materialized in the tenant has no integer UserId
                    // to reference, so they don't resolve here; add them first with
                    // Add-OrchFolderUser (materialization is that cmdlet's job — like copying a
                    // package before a process), then retry.
                    var tenantUsers = drive.Users.Get()
                        .Where(u => u.Type != "DirectoryGroup" && u.Id is not null);
                    // Match both UserName (tenant form) and EmailAddress (canonical)
                    // so Azure AD B2B guests resolve regardless of which form the
                    // caller supplies — see FilterByWildcards multi-selector overload.
                    specifiedUsers = tenantUsers.FilterByWildcardsAny(
                        [u => u?.UserName, u => u?.EmailAddress],
                        wpUserName);
                    if (!specifiedUsers.Any())
                    {
                        string strUserNames = string.Join(", ", param.UserName!);
                        Exception e = new Exception($"UserName '{strUserNames}' was not found among the tenant users of '{drive.NameColon}'. If this is a directory user not yet in the tenant, add them to the destination folder first (e.g.: Add-OrchFolderUser), then retry.");
                        var errorRecord = new ErrorRecord(new OrchException(targetFolder, e), "SetAssetError", ErrorCategory.InvalidOperation, targetFolder);
                        WriteError(errorRecord);
                        continue;
                    }
                }

                // expand MachineName
                if (wpMachineName is not null)
                {
                    // Restrict the candidate set to machines actually assigned to this folder
                    // so that asset per-machine values cannot reference machines outside the
                    // folder's scope.
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

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType != "Credential" && a.ValueType != "Secret");

                foreach (var name in param.Name!.WithCancellation(cancelHandler.Token))
                {
                    // Match against this name only: deciding with the all-names pattern
                    // list would send every name down the update branch as soon as any
                    // one of them matched an existing asset.
                    var wpName = new[] { name }.ConvertToWildcardPatternList();
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        // Update existing assets
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines);
                            if (asset is not null)
                                pendingAssets.TryAdd((asset.Name!, asset.Path!), asset);
                        }
                    }
                    else if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        WriteWarning($"\"{System.IO.Path.Combine(targetFolder, name)}\": no existing asset matched, and a new asset cannot be created with a name that contains wildcard characters. "
                            + $"To create an asset with this literal name, escape the wildcard characters with a backtick: -Name '{WildcardPattern.Escape(name)}'.");
                        continue;
                    }
                    else
                    {
                        // Create a new asset
                        var asset = UpdateAssetInMemory(drive, folder, WildcardPattern.Unescape(name), param, specifiedUsers!, specifiedMachines);
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

        using var reporter = new ProgressReporter(this, 1, pendingAssets.Count, "Updating Assets");

        // Process the grouped parameter sets
        try
        {
            int index = 0;
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var asset in pendingAssets.Values.WithCancellation(cancelHandler.Token))
            {
                var drivesFolders = SessionState.EnumFolders([WildcardPattern.Escape(asset.Path!)]);
                var (drive, folder) = drivesFolders[0];

                var target = System.IO.Path.Combine(folder.GetPSPath(), asset.Name ?? "");

                reporter.WriteProgress(++index, asset.Name);

                var existingAssets = drive.Assets.Get(folder);
                var existingAsset = existingAssets.FirstOrDefault(a => a.Name == asset.Name);
                if (existingAsset is not null && !ValidValueTypes.Contains(existingAsset.ValueType))
                {
                    Exception e = new($"ValueType '{existingAsset.ValueType}' is not supported.");
                    var errorRecord = new ErrorRecord(new OrchException(target, e), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                    continue;
                }

                try
                {
                    if (existingAsset is null) // Add a new asset
                    {
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            continue;
                        }
                        if (ShouldProcess(target, "Add Asset"))
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
                            if (ShouldProcess(target, "Remove Asset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else // Update the asset
                        {
                            if (ShouldProcess(target, "Update Asset"))
                            {
                                drive.OrchAPISession.PutAsset(folder.Id ?? 0, asset);

                                // If the asset does not contain UserValues, check the linked folders and clear the cache
                                try
                                {
                                    if (asset.UserValues is null || asset.UserValues.Count() == 0)
                                    {
                                        var sharedFolders = drive.OrchAPISession.GetFoldersForAsset(folder.Id ?? 0, asset.Id ?? 0);
                                        if (sharedFolders is not null && sharedFolders.AccessibleFolders is not null)
                                        {
                                            foreach (var AccessibleFolder in sharedFolders.AccessibleFolders)
                                            {
                                                drive.Assets.ClearCache(AccessibleFolder.Id!.Value);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Cache stays warm for shared folders; the next read may show stale data.
                                    System.Diagnostics.Debug.WriteLine($"AssetLink-driven cache clear failed: {ex.Message}");
                                }
                            }
                        }
                        folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddAssetError", ErrorCategory.InvalidOperation, target));
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
