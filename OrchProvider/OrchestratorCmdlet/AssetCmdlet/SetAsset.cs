using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.ValueType_Name_Value_UserName_MachineName;

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
public class SetAssetCommand : OrchestratorPSCmdlet
{
    private readonly List<SetAssetCommandParameter> parameters = [];

    // 現在の実装では、大量の行を含む CSV を処理するのが遅い。
    // Dictionary に変更したいけど、ちと面倒。。
    private readonly List<Asset> parameterSets = [];
    
    private const string Default = "DefaultParameterSet";
    //private const string GenerateTemplateCsv = "GenerateTemplateCsvParameterSet";

    public static readonly string[] ValidValueTypes = ["Text", "Integer", "Bool"];

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

    //[Parameter(ParameterSetName = GenerateTemplateCsv, Mandatory = true)]
    //public SwitchParameter GenerateTemplateCsv { get; set; }

    //[Parameter(ParameterSetName = GenerateTemplateCsv, Position = 0)]
    //[ArgumentCompleter(typeof(EncodingCompleter))]
    //[EncodingArgumentTransformation]
    //public Encoding? CsvEncoding { get; set; }

    //[Parameter(ParameterSetName = GenerateTemplateCsv, Position = 1)]
    //public string? TemplateCsvPath { get; set; }

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
            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択された ValueType のみ対象とする
            var wpValueType = CreateWPListFromOtherParameters(commandAst, "ValueType", TPositional.Parameters);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var asset in result.Result
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
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpValueType = CreateWPListFromOtherParameters(commandAst, "ValueType", TPositional.Parameters);
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
            var wpDescription = CreateWPListFromParameter(commandAst, "Description", TPositional.Parameters, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool isEmpty = true;
            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var asset in result.Result
                    .Where(a => a.ValueType != "Credential")
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
        private static IEnumerable<CompletionResult> GetCompletionResultsForValueType(string? valueType, bool exists)
        {
            if (string.IsNullOrEmpty(valueType))
            {
                valueType = "text";
            }

            switch (valueType.ToLower())
            {
                case "text":
                    var newTextValue = "Text value here";
                    yield return new CompletionResult(PathTools.EscapePSText(newTextValue), newTextValue, CompletionResultType.Text, newTextValue);
                    if (exists) yield return new CompletionResult("''", "''", CompletionResultType.Text, "Specify '' to remove the existing value.");
                    break;
                case "bool":
                    yield return new CompletionResult("False");
                    yield return new CompletionResult("True");
                    if (exists) yield return new CompletionResult("''", "''", CompletionResultType.Text, "Specify '' to remove the existing value.");
                    break;
                case "integer":
                    yield return new CompletionResult("0");
                    if (exists) yield return new CompletionResult("''", "''", CompletionResultType.Text, "Specify '' to remove the existing value.");
                    break;
            }
        }

        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            var wpValueType   = CreateWPListFromOtherParameters(commandAst, "ValueType",   TPositional.Parameters);
            var wpName        = CreateWPListFromOtherParameters(commandAst, "Name",        TPositional.Parameters);
            var wpUserName    = CreateWPListFromOtherParameters(commandAst, "UserName",    TPositional.Parameters);
            var wpMachineName = CreateWPListFromOtherParameters(commandAst, "MachineName", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

            bool bValueExists = false;
            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var asset in result.Result
                    .Where(a => a.ValueType != "Credential")
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(a => a?.Name, wpName))
                {
                    if ((wpUserName is not null && wpUserName.Count != 0) || (wpMachineName is not null && wpMachineName.Count != 0))
                    {
                        var userValues = asset.UserValues?
                            .FilterByWildcards(uv => uv?.UserName, wpUserName)
                            .FilterByWildcards(uv => uv?.MachineName, wpMachineName);
                        foreach (var userValue in userValues!)
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
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの UserName は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            //// パラメータで選択済みの Name のみ対象とする
            //var wpName = CreateWPListFromOtherParameters(commandAst, "Name", positionalParams);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var userRole in result.Result
                    .Where(u => u.UserEntity!.Type != "DirectoryGroup")
                    .Where(ur => wp.IsMatch(ur.UserEntity!.UserName))
                    .ExcludeByWildcards(ur => ur?.UserEntity?.UserName, wpUserName)
                    .OrderBy(ur => ur.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRole);
                    yield return new CompletionResult(PathTools.EscapePSText(userRole.UserEntity!.UserName), userRole.UserEntity!.UserName, CompletionResultType.ParameterValue, tiphelp);
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

            // パラメータで選択済みの MachineName は、候補から除外する
            var wpMachineName = CreateWPListFromParameter(commandAst, "MachineName", TPositional.Parameters, wordToComplete);

            // TODO: 既存のユーザー名とマシン名の組み合わせは、候補に表示しないようにする
            // ややこしいから、いいか。。

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var machine in result.Result
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
        if (ValueType == "Credential")
            return;

        SetAssetCommandParameter parameter = new()
        {
            Name = Name,
            Description = Description,
            ValueType = ValueType,
            Value = Value,
            UserName = UserName,
            MachineName = MachineName,
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

    private Asset? UpdateAssetInMemory(OrchDriveInfo drive, Folder folder, string name, SetAssetCommandParameter param,
        IEnumerable<User>? specifiedUsers,
        IEnumerable<ExtendedMachine?> specifiedMachines)
    {
        string target = System.IO.Path.Combine(folder.GetPSPath(), param.Name![0]);
        bool isDirty = false;

        var asset = parameterSets.FirstOrDefault(asset => asset.Name == name && asset.Path == folder.GetPSPath());
        if (asset is null)
        {
            var assets = drive.Assets.Get(folder);
            asset = assets.FirstOrDefault(a => a.Name == name);
            if (asset is null)
            {
                if (string.IsNullOrEmpty(param.Value))
                    return null;

                isDirty = true;
                asset = new Asset
                {
                    Name = name,
                    Description = param.Description,
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
                Asset newAsset = new()
                {
                    Key = asset.Key,
                    Name = asset.Name,
                    Description = asset.Description,
                    ValueType = asset.ValueType,
                    CanBeDeleted = asset.CanBeDeleted,
                    HasDefaultValue = asset.HasDefaultValue,
                    Path = folder.GetPSPath(),
                    ValueScope = asset.ValueScope,
                    Value = asset.Value,
                    StringValue = asset.StringValue,
                    BoolValue = asset.BoolValue,
                    IntValue = asset.IntValue,
                    Id = asset.Id,
                    Tags = asset.Tags,
                };
                if (asset.UserValues is not null && asset.UserValues.Any())
                {
                    newAsset.UserValues = new List<AssetUserValue>();
                    foreach (var uv in asset.UserValues)
                    {
                        AssetUserValue auv = new()
                        {
                            UserId = uv.UserId,
                            UserName = uv.UserName,
                            MachineId = uv.MachineId,
                            //MachineName { get; set; }
                            ValueType = uv.ValueType,
                            StringValue = uv.StringValue,
                            BoolValue = uv.BoolValue,
                            IntValue = uv.IntValue,
                            Value = uv.Value,
                            CredentialUsername = uv.CredentialUsername,
                            //public string? CredentialPassword { get; set; }
                            CredentialStoreId = uv.CredentialStoreId,
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

        if (!string.IsNullOrEmpty(param.Description) && asset.Description != param.Description)
        {
            isDirty = true;
            asset.Description = param.Description;
        }

        // respect existing asset valuetype
        if (string.IsNullOrEmpty(asset.ValueType) && asset.ValueType != param.ValueType)
        {
            isDirty = true;
            asset.ValueType = param.ValueType;
        }

        // Value が指定されなかったときは、Value を更新しない
        // (Value に '' が指定されたときは、アセットを削除するために後続の処理を続行する)
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
                    var errorRecord = new ErrorRecord(new OrchException(target, $"Value {param.Value} cannot be parsed as bool."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                    return null;
                }
            }
        }

        // Global 値を更新
        if (specifiedUsers is null)
        {
            if (specifiedMachines is not null && specifiedMachines.Any(m => m is not null))
            {
                // マシンを無視する旨の警告
                string strMachineNames = string.Join(", ", param.MachineName!);
                var errorRecord = new ErrorRecord(new OrchException(target, $"UserName was not specified. MachineName '{strMachineNames}' ignored."), "SetAssetError", ErrorCategory.InvalidOperation, target);
                WriteError(errorRecord);
            }

            if (param.Value == "" && !string.IsNullOrEmpty(asset.Value)) // "" が指定された場合は、Global 値を削除する
            {
                isDirty = true;
                asset.ValueScope = "PerRobot";
                asset.HasDefaultValue = false;
                asset.StringValue = null;
                asset.BoolValue = null;
                asset.IntValue = null;
            }
            else if (param.Value is not null)
            {
                if (string.IsNullOrEmpty(asset.ValueType))
                {
                    Exception e = new Exception($"ValueType was not specified. It will be assumed as 'Text'.");
                    var errorRecord = new ErrorRecord(new OrchException(target, e), "SetAssetError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                    asset.ValueType = "Text";
                }

                switch (asset.ValueType)
                {
                    case "Text":
                        if (asset.StringValue != param.Value)
                        {
                            isDirty = true;
                            asset.HasDefaultValue = true;
                            asset.StringValue = param.Value;
                        }
                        break;
                    case "Bool":
                        if (asset.BoolValue != boolValue)
                        {
                            isDirty = true;
                            asset.HasDefaultValue = true;
                            asset.BoolValue = boolValue; break;
                        }
                        break;
                    case "Integer":
                        if (asset.IntValue != intValue)
                        {
                            isDirty = true;
                            asset.HasDefaultValue = true;
                            asset.IntValue = intValue;
                        }
                        break;
                }
            }
        }
        else // PerRobot 値を更新
        {
            foreach (var user in specifiedUsers)
            {
                foreach (var machine in specifiedMachines!)
                {
                    if (asset.UserValues is null)
                    {
                        asset.UserValues = new List<AssetUserValue>();
                    }

                    AssetUserValue userValue = asset.UserValues.FirstOrDefault(uv => uv.UserId == user.Id && uv.MachineId == machine?.Id);
                    if (param.Value == "")
                    {
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
                            UserId = user.Id,
                            UserName = user.UserName,
                            MachineId = machine?.Id,
                            MachineName = machine?.Name
                        };
                        asset.UserValues.Add(userValue);
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
                        Exception e = new($"ValueType was not specified. It will be assumed as 'Text'.");
                        var errorRecord = new ErrorRecord(new OrchException(target, e), "SetAssetError", ErrorCategory.InvalidOperation, target);
                        WriteError(errorRecord);
                        asset.ValueType = "Text";
                        userValue!.ValueType = "Text";
                    }

                    switch (asset.ValueType)
                    {
                        case "Text":
                            if (userValue!.StringValue != param.Value)
                            {
                                isDirty = true;
                                asset.ValueScope = "PerRobot";
                                userValue.StringValue = param.Value;
                            }
                            break;
                        case "Bool":
                            if (userValue!.BoolValue != boolValue)
                            {
                                isDirty = true;
                                asset.ValueScope = "PerRobot";
                                userValue.BoolValue = boolValue;
                            }
                            break;
                        case "Integer":
                            if (userValue!.IntValue != intValue)
                            {
                                isDirty = true;
                                asset.ValueScope = "PerRobot";
                                userValue.IntValue = intValue;
                            }
                            break;
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
                Exception e = new Exception($"ValueType '{param.ValueType}' is invalid.");
                var errorRecord = new ErrorRecord(new OrchException(target, e), "SetAssetError", ErrorCategory.InvalidOperation, target);
                WriteError(errorRecord);
                continue;
            }

            // expand Asset Name
            List<WildcardPattern> wpName = param.Name!.ConvertToWildcardPatternList();

            // expand UserName and MachineName
            List<WildcardPattern> wpUserName = null;
            List<WildcardPattern> wpMachineName = null;

            if (param.UserName is not null && param.UserName.Any(un => !string.IsNullOrEmpty(un)))
                wpUserName = param.UserName.ConvertToWildcardPatternList();

            if (param.MachineName is not null && param.MachineName.Any(mn => !string.IsNullOrEmpty(mn)))
                wpMachineName = param.MachineName.ConvertToWildcardPatternList();

            var drivesFolders = OrchDriveInfo.EnumFolders(param.Path);
            foreach (var (drive, folder) in drivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string targetFolder = $"{folder.GetPSPath()}";

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
                    var tenantMachines = drive.Machines.Get(); // ★TODO: ここは FolderMachinesAssigned.Get(folder) に変えておかねば。
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
                    specifiedMachines = [null];
                }

                var existingAssets = drive.Assets.Get(folder).Where(a => a.ValueType != "Credential");

                foreach (var name in param.Name!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    var matchingAssets = existingAssets.FilterByWildcards(n => n?.Name, wpName);
                    if (matchingAssets.Any())
                    {
                        // 既存のアセットを更新
                        foreach (var matchingAsset in matchingAssets)
                        {
                            var asset = UpdateAssetInMemory(drive, folder, matchingAsset.Name!, param, specifiedUsers!, specifiedMachines);
                            if (asset is not null && !parameterSets.Contains(asset))
                                parameterSets.Add(asset);
                        }
                    }
                    else if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        continue;
                    }
                    else
                    {
                        // 新規アセットを作成
                        var asset = UpdateAssetInMemory(drive, folder, WildcardPattern.Unescape(name), param, specifiedUsers!, specifiedMachines);
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

        string msg = "Updating Assets";
        using var reporter = new ProgressReporter(this, 1, parameterSets.Count, msg, msg);

        // グループ化したパラメータセットを処理する
        try
        {
            int index = 0;
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var asset in parameterSets)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                var drivesFolders = OrchDriveInfo.EnumFolders([WildcardPattern.Escape(asset.Path!)]);
                var (drive, folder) = drivesFolders[0];

                var target = System.IO.Path.Combine(folder.GetPSPath(), asset.Name ?? "");

                reporter.WriteProgress(++index, $"{index:D}/{parameterSets.Count}");

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
                    if (existingAsset is null) // アセットを新規追加
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
                            //drive._dicAssets?.TryRemove(folder.Id ?? 0, out List<Asset>? _);
                        }
                    }
                    else
                    {
                        // アセットを削除
                        if (!asset.HasDefaultValue.GetValueOrDefault() && (asset.UserValues is null || !asset.UserValues.Any()))
                        {
                            if (ShouldProcess(target, "Remove Asset"))
                            {
                                drive.OrchAPISession.RemoveAsset(folder.Id ?? 0, asset.Id ?? 0);
                            }
                        }
                        else // アセットを更新
                        {
                            if (ShouldProcess(target, "Update Asset"))
                            {
                                drive.OrchAPISession.PutAsset(folder.Id ?? 0, asset);

                                // もし UserValues を含まないアセットであれば、リンク先フォルダーを確認してキャッシュをクリアする
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
                                catch { }
                            }
                        }
                        folderIdsThatShouldRemoveCache.Add((drive, folder.Id ?? 0));
                        //drive._dicAssets?.TryRemove(folder.Id ?? 0, out List<Asset>? _);
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
