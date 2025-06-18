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

    // 存在しないアセットを "New asset name here" として表示するので、これは共通化できない
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

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var asset in results
                .Select(r => r.Item)
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

            // パラメータで選択済みの UserName は、候補から除外する
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            // パラメータで選択済みの UserName は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));

            foreach (var userRoles in results
                .Select(r => r.Item)
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

            // パラメータで選択済みの MachineName は、候補から除外する
            var wpMachineName = CreateWPListFromParameter(commandAst, "MachineName", TPositional.Parameters, wordToComplete);

            // TODO: 既存のユーザー名とマシン名の組み合わせは、候補に表示しないようにする
            // ややこしいから、いいか。。

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var machine in results
                .Select(r => r.Item)
                .Where(m => wp.IsMatch(m.Name))
                .ExcludeByWildcards(m => m?.Name, wpMachineName)
                .OrderBy(m => m.Name))
            {
                string tiphelp = TipHelp(machine);
                yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    // StaticTextCompleter で書き直せる
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

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            //var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults2.ForEachMany(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool bEmpty = true;
            foreach (var asset in results
                .Select(r => r.Item)
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
        // 対象のフォルダの Asset を、非同期にまとめて取得する
        var _ = ParallelResults.ForEach(parameters, param =>
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(param.Path);

            // 展開済みなので、フォルダはいっこしか展開されないはずだが、いちおう繰り返す
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
                // メモリ内にアセットを新規作成
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
                // 既存アセットをコピーして、メモリ内にアセットを作成
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

                // 既存アセットの UserValues をメモリ内にコピー
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
            // 最後に isDirty が true になっていれば、parameterSets.Add(asset) する。ここではまだしない
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

        // ここまでで、Description と CredentialStoreId をメモリ内に更新完了
        // ただし、UserValues への CredentialStoreId の更新はまだ

        // CredentialPassword が指定されなかったときは、Credential を更新しない
        // (CredentialPassword に '' が指定されたときは、アセットを削除するために後続の処理を続行する)

        // Global 値を更新
        if (specifiedUsers is null)
        {
            if (specifiedMachines is not null && specifiedMachines.Any(m => m is not null))
            {
                // マシンを無視する旨の警告
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

                if ((asset.CredentialUsername != param.CredentialUsername || string.IsNullOrEmpty(param.CredentialUsername)) && param.CredentialPassword == "") // "" が指定された場合は、Global 値を削除する
                {
                    isDirty = true;
                    asset.ValueScope = "PerRobot";
                    asset.HasDefaultValue = false;
                    asset.CredentialUsername = "";
                    asset.CredentialPassword = null;
                    asset.CredentialStoreId = null;
                }
                else if (!string.IsNullOrEmpty(param.CredentialPassword)) // CredentialPassword に何かが指定された場合に限り、これを更新する
                {
                    isDirty = true;
                    asset.CredentialPassword = param.CredentialPassword;
                    asset.HasDefaultValue = true;
                }
            }
        }
        else // PerRobot 値を更新
        {
            // CredentialStore が指定された場合は、全ての UserValues の CredentialStoreId を更新
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

            // 当該の UserValue を探して更新                        
            foreach (var user in specifiedUsers)
            {
                foreach (var machine in specifiedMachines!)
                {
                    asset.UserValues ??= [];

                    AssetUserValue userValue = asset.UserValues.FirstOrDefault(uv => uv.UserId == user.Id && uv.MachineId == machine?.Id);
                    if (string.IsNullOrEmpty(param.CredentialUsername) && (param.CredentialPassword == "" || param.ExternalName == ""))
                    {
                        // CredentialPassword に "" もしくは ExternalName に "" が指定された場合は
                        // このアセット値を削除する
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
        // パフォーマンス向上のため、対象のフォルダの Asset を先にまとめて取得しておく
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

            var drivesFolders = OrchDriveInfo.EnumFolders(param.Path);
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
                    // 処理の便宜上、null の要素をひとつだけ入れておく
                    //specifiedMachines = new ExtendedMachine?[] { null };
                    specifiedMachines = [null];
                }

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType == "Credential");

                foreach (var name in param.Name!)
                {
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        // 既存のアセットを更新
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines, credentialStoreId);
                            if (asset is not null && !parameterSets.Contains(asset))
                                parameterSets.Add(asset);
                        }
                    }
                    else
                    {
                        // 新規アセットを作成
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

        // グループ化したパラメータセットを処理する
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

                var drivesFolders = OrchDriveInfo.EnumFolders([WildcardPattern.Escape(asset.Path!)]);
                var (drive, folder) = drivesFolders[0];

                var target = asset.GetPSPath();

                reporter.WriteProgress(++index);

                var existingAssets = drive.Assets.Get(folder);
                var existingAsset = existingAssets.FirstOrDefault(a => a.Name == asset.Name);

                try
                {
                    if (existingAsset is null) // アセットを新規追加
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
                        // アセットを削除
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            if (ShouldProcess(target, "Remove CredentialAsset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else // アセットを更新
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
