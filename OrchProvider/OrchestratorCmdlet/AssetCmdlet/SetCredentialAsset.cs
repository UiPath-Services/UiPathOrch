using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.Metadata;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_UserName_MachineName_CredentialUsername_CredentialPassword;

namespace UiPath.PowerShell.Commands;

class SetCredentialAssetCommandParameter
{
    public string[]? Name { set; get; }
    public string? Description { set; get; }
    public string? CredentialStore { set; get; }
    public string[]? UserName { set; get; }
    public string[]? MachineName { set; get; }
    public string? CredentialUsername { set; get; }
    public string? CredentialPassword { set; get; }
    public string? ExternalName { set; get; }
    public string[]? Path { set; get; }
}

[Cmdlet(VerbsCommon.Set, "OrchCredentialAsset", DefaultParameterSetName = Default, SupportsShouldProcess = true)]
[OutputType(typeof(UiPath.PowerShell.Entities.Asset))]
public class SetCredentialAssetCommand : OrchestratorPSCmdlet
{
    private readonly List<SetCredentialAssetCommandParameter> parameters = [];
    private readonly List<Asset> parameterSets = [];

    private const string Default = "DefaultParameterSet";
    private const string Plain = "SpecifyPlainPasswordParameterSet";
    //private const string Export = "ExportTemplateParameterSet";

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
    public PSCredential? Credential { get; set; }

    [Parameter(ParameterSetName = Plain, Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialUsernameCompleter))]
    public string? CredentialUsername { get; set; }

    [Parameter(ParameterSetName = Plain, Position = 4, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialPasswordCompleter))]
    public string? CredentialPassword { get; set; }

    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialPasswordCompleter))]
    public string? ExternalName { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DescriptionCompleter))]
    public string? Description { get; set; }

    [Parameter(ParameterSetName = Default)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string? CredentialStore { get; set; }

    [Parameter(ParameterSetName = Default)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    // Cannot be shared because non-existent assets are displayed as "New asset name here"
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

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType == "Credential")
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

            // Only target Names already selected by the parameter
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // Exclude UserNames already selected by the parameter from the candidates
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));

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

            // Exclude MachineNames already selected by the parameter from the candidates
            var wpMachineName = CreateWPListFromParameter(commandAst, "MachineName", TPositional.Parameters, wordToComplete);

            // TODO: Exclude existing user name and machine name combinations from the candidates
            // It's complicated, so let's skip it for now..

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

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

    // Can be rewritten using StaticTextCompleter
    private class CredentialUsernameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("'CredentialUsername here'");
        }
    }

    private class CredentialPasswordCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            yield return new CompletionResult("'CredentialPassword here'");
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

            // Only target Names already selected by the parameter
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            //var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool bEmpty = true;
            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType == "Credential")
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

    //long FindCredentialStoreId(string target, OrchDriveInfo drive, WildcardPattern? wpCredentialStore)
    //{
    //    var credentialStores = drive.GetCredentialStores();
    //    CredentialStore cs = null;
    //    if (wpCredentialStore is not null)
    //    {
    //        var matchingCredentialStores = credentialStores.Where(cs => wpCredentialStore.IsMatch(cs.Name));
    //        if (!matchingCredentialStores.Any())
    //        {
    //            Exception e = new Exception($"CredentialStore '{CredentialStore}' does not exist.");
    //            WriteError(new ErrorRecord(new OrchException(target, e), "SetCredentialAssetError", ErrorCategory.InvalidOperation, target));
    //            return 0;
    //        }
    //        if (matchingCredentialStores.Take(2).Count() == 2)
    //        {
    //            Exception e = new Exception($"CredentialStore '{CredentialStore}' resolved to multiple credential stores. Ignored.");
    //            WriteError(new ErrorRecord(new OrchException(target, e), "SetCredentialAssetError", ErrorCategory.InvalidOperation, target));
    //            return 0;
    //        }
    //        // assert(matchingCredentialStores.Couint() == 1)
    //        cs = matchingCredentialStores.First();
    //    }
    //    else
    //    {
    //        //cs = credentialStores.Where(cs => string.Equals(cs.Name, "Orchestrator Database", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    //        //cs ??= credentialStores.First();
    //    }
    //    return cs?.Id ?? 0;
    //}

    protected override void ProcessRecord()
    {
        SetCredentialAssetCommandParameter parameter = new()
        {
            Name = Name,
            Description = Description,
            CredentialStore = CredentialStore,
            UserName = UserName,
            MachineName = MachineName,
            CredentialUsername = CredentialUsername,
            CredentialPassword = CredentialPassword,
            ExternalName = ExternalName,
            Path = Path
        };
        parameters.Add(parameter);
    }

    protected void RetrieveAllAssets()
    {
        // Retrieve Assets for the target folders asynchronously in bulk
        var _ = ParallelResults.ForEach(parameters, param =>
        {
            var drivesFolders = SessionState.EnumFolders(param.Path);

            // Since the path is already resolved, only one folder should be expanded, but iterate just in case
            return ParallelResults.ForEach(drivesFolders, driveFolder =>
            {
                var (drive, folder) = driveFolder;
                return drive.Assets.Get(folder);
            });
        });
    }

    private Asset? UpdateAssetInMemory(OrchDriveInfo drive, Folder folder, string name, SetCredentialAssetCommandParameter param,
        IEnumerable<User>? specifiedUsers,
        IEnumerable<ExtendedMachine?> specifiedMachines,
        Int64 credentialStoreId)
    {
        string target = System.IO.Path.Combine(folder.GetPSPath(), param.Name?[0] ?? "");
        //string target = System.IO.Path.Combine(folder.GetPSPath(), param.Name!);
        bool isDirty = false;

        var asset = parameterSets.FirstOrDefault(asset => asset.Name == name && asset.Path == folder.GetPSPath());
        if (asset is null)
        {
            var assets = drive.Assets.Get(folder);
            asset = assets.FirstOrDefault(a => a.Name == name);
            if (asset is null)
            {
                if (string.IsNullOrEmpty(param.CredentialPassword) && string.IsNullOrEmpty(param.ExternalName))
                    return null;

                isDirty = true;
                // Create a new asset in memory
                asset = new Asset
                {
                    Name = name,
                    Description = param.Description,
                    ValueScope = "Global",
                    ValueType = "Credential",
                    CredentialUsername = "",
                    CanBeDeleted = true,
                    HasDefaultValue = false,
                    Path = folder.GetPSPath(),
                };
            }
            else
            {
                // Copy the existing asset and create an asset in memory
                Asset newAsset = new()
                {
                    Key = asset.Key,
                    Name = asset.Name,
                    Description = asset.Description,
                    ValueType = "Credential",
                    CredentialStoreId = asset.CredentialStoreId,
                    CredentialUsername = asset.CredentialUsername,
                    CanBeDeleted = asset.CanBeDeleted,
                    HasDefaultValue = asset.HasDefaultValue,
                    Path = folder.GetPSPath(),
                    ValueScope = asset.ValueScope,
                    Id = asset.Id,
                    Tags = asset.Tags,
                };

                // Copy the existing asset's UserValues into memory
                if (asset.UserValues is not null && asset.UserValues.Count != 0)
                {
                    newAsset.UserValues = new List<AssetUserValue>();
                    foreach (var uv in asset.UserValues)
                    {
                        AssetUserValue auv = new()
                        {
                            UserId = uv.UserId,
                            UserName = uv.UserName,
                            MachineId = uv.MachineId,
                            ValueType = uv.ValueType,
                            CredentialUsername = uv.CredentialUsername,
                            CredentialStoreId = newAsset.CredentialStoreId,
                            ExternalName = uv.ExternalName,
                            KeyValueList = uv.KeyValueList,
                            Id = uv.Id,
                        };
                        newAsset.UserValues.Add(auv);
                    }
                }
                asset = newAsset;
            }
            // If isDirty is true at the end, call parameterSets.Add(asset). Don't do it here yet
            //parameterSets.Add(asset);
        }

        if (asset.Description != param.Description && !string.IsNullOrEmpty(param.Description))
        {
            isDirty = true;
            asset.Description = param.Description;
        }

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

        // At this point, Description and CredentialStoreId have been updated in memory
        // However, CredentialStoreId for UserValues has not been updated yet

        // When CredentialPassword is not specified, do not update the Credential
        // (When CredentialPassword is set to '', continue subsequent processing to delete the asset)

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

            if (!string.IsNullOrEmpty(param.ExternalName))
            {
                isDirty = true;
                asset.ExternalName = param.ExternalName;
                asset.CredentialUsername = null;
                asset.CredentialPassword = null;
                asset.HasDefaultValue = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(param.CredentialUsername) && asset.CredentialUsername != param.CredentialUsername)
                {
                    isDirty = true;
                    asset.CredentialUsername = param.CredentialUsername;
                }

                if ((asset.CredentialUsername != param.CredentialUsername || string.IsNullOrEmpty(param.CredentialUsername)) && param.CredentialPassword == "") // If "" is specified, delete the Global value
                {
                    isDirty = true;
                    asset.ValueScope = "PerRobot";
                    asset.HasDefaultValue = false;
                    asset.CredentialUsername = "";
                    asset.CredentialPassword = null;
                    asset.CredentialStoreId = null;
                }
                else if (!string.IsNullOrEmpty(param.CredentialPassword)) // Only update this when CredentialPassword has a value specified
                {
                    isDirty = true;
                    asset.CredentialPassword = param.CredentialPassword;
                    asset.HasDefaultValue = true;
                }
            }
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

            // Find and update the corresponding UserValue
            foreach (var user in specifiedUsers)
            {
                foreach (var machine in specifiedMachines!)
                {
                    asset.UserValues ??= [];

                    AssetUserValue userValue = asset.UserValues.FirstOrDefault(uv => uv.UserId == user.Id && uv.MachineId == machine?.Id);
                    if (string.IsNullOrEmpty(param.CredentialUsername) && (param.CredentialPassword == "" || param.ExternalName == ""))
                    {
                        // If CredentialPassword is set to "" or ExternalName is set to "",
                        // delete this asset value
                        if (userValue is not null)
                        {
                            isDirty = true;
                            asset.UserValues.Remove(userValue);
                            if (!asset.UserValues.Any())
                            {
                                asset.ValueScope = "Global";
                                asset.UserValues = null;
                            }
                        }
                        continue;
                    }
                    if (userValue is null)
                    {
                        isDirty = true;
                        userValue = new AssetUserValue
                        {
                            ValueType = "Credential",
                            UserId = user.Id,
                            UserName = user.UserName,
                            MachineId = machine?.Id,
                            MachineName = machine?.Name,
                            CredentialStoreId = asset.CredentialStoreId
                        };

                        asset.ValueScope = "PerRobot";
                        asset.UserValues.Add(userValue);
                    }

                    if (!string.IsNullOrEmpty(param.ExternalName))
                    {
                        isDirty = true;
                        userValue.ExternalName = param.ExternalName;
                        userValue.CredentialUsername = null;
                        userValue.CredentialPassword = null;
                    }
                    else
                    {
                        if (userValue.CredentialUsername != param.CredentialUsername && !string.IsNullOrEmpty(param.CredentialUsername))
                        {
                            isDirty = true;
                            userValue.CredentialUsername = param.CredentialUsername;
                        }
                        if (!string.IsNullOrEmpty(param.CredentialPassword))
                        {
                            isDirty = true;
                            userValue.CredentialPassword = param.CredentialPassword;
                        }
                    }
                }
            }
        }
        if (isDirty)
            return asset;
        else
            return null;
    }

    protected void BuildAssetDataFromParameterSets()
    {
        // For performance, retrieve all Assets for the target folders in advance
        RetrieveAllAssets();

        foreach (var param in parameters)
        {
            if (ParameterSetName == Default)
            {
                param.CredentialUsername = Credential!.UserName;
                param.CredentialPassword = ConvertToUnsecureString(Credential!.Password);
            }

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
                    var tenantUsers = drive.GetUsers().Where(u => u.Type != "DirectoryGroup");
                    specifiedUsers = tenantUsers.FilterByWildcards(u => u?.UserName, wpUserName);
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
                    var tenantMachines = drive.Machines.Get();
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
                    //specifiedMachines = new ExtendedMachine?[] { null };
                    specifiedMachines = [null];
                }

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType == "Credential");

                foreach (var name in param.Name!)
                {
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        // Update existing assets
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                            if (asset is not null && !parameterSets.Contains(asset))
                                parameterSets.Add(asset);
                        }
                    }
                    else
                    {
                        // Create a new asset
                        var asset = UpdateAssetInMemory(drive, folder, name, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                        if (asset is not null && !parameterSets.Contains(asset))
                            parameterSets.Add(asset);
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        BuildAssetDataFromParameterSets();

        List<(OrchDriveInfo drive, Int64 id)> folderIdsThatShouldRemoveCache = [];

        using var reporter = new ProgressReporter(this, 1, parameterSets.Count, "Updating credential assets");

        // Process the grouped parameter sets
        try
        {
            int index = 0;
            foreach (var asset in parameterSets)
            {
                if (asset.CredentialUsername == "")
                {
                    asset.CredentialUsername = null;
                }
                if (string.IsNullOrEmpty(asset.CredentialPassword))
                {
                    asset.CredentialStoreId = null;
                }

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
                        if (ShouldProcess(target, "Add CredentialAsset"))
                        {
                            Asset createdAsset = drive.OrchAPISession.AddAsset(folder.Id ?? 0, asset);
                            createdAsset!.Path = folder.GetPSPath();
                            WriteObject(createdAsset);

                            folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                            //drive._dicAssets?.TryRemove(folder.Id ?? 0, out List<Asset>? _);
                        }
                    }
                    else
                    {
                        // Delete the asset
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            if (ShouldProcess(target, "Remove CredentialAsset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else // Update the asset
                        {
                            if (ShouldProcess(target, "Update CredentialAsset"))
                            {
                                drive.OrchAPISession.PutAsset(folder.Id ?? 0, asset);
                            }
                        }

                        folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                        //drive._dicAssets?.TryRemove(folder.Id ?? 0, out List<Asset>? _);
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
