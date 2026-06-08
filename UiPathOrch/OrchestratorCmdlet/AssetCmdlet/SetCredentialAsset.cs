using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

public class SetCredentialAssetCommandParameter : ISetAssetRow
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
public class SetCredentialAssetCmdlet : SetCredentialLikeAssetCmdletBase<SetCredentialAssetCommandParameter>
{
    // parameters / RetrieveAllAssets are inherited from SetCredentialLikeAssetCmdletBase;
    // _resolvedDescriptions / MergeDescription / pendingAssets from SetAssetCmdletBase.

    private const string Default = "DefaultParameterSet";
    private const string Plain = "SpecifyPlainPasswordParameterSet";
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
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string? CredentialStore { get; set; }

    [Parameter(ParameterSetName = Default)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ParameterSetName = Default, ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = Plain, ValueFromPipelineByPropertyName = true)]
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
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Only target Names already selected by the parameter
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude UserNames already selected by the parameter from the candidates
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
                    string tiphelp = TipHelp(machine);
                    yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class CredentialUsernameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
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
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Only target Names already selected by the parameter
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

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
            Path = EffectivePath(Path, LiteralPath)
        };
        parameters.Add(parameter);
    }

    protected override string ValueType => "Credential";

    protected override bool HasCreateValue(SetCredentialAssetCommandParameter param)
        => !string.IsNullOrEmpty(param.CredentialPassword) || !string.IsNullOrEmpty(param.ExternalName);

    protected override void InitializeNewAsset(Asset asset)
        => asset.CredentialUsername = "";

    protected override bool ApplyGlobalValue(Asset asset, SetCredentialAssetCommandParameter param)
    {
        if (!string.IsNullOrEmpty(param.ExternalName))
        {
            asset.ExternalName = param.ExternalName;
            asset.CredentialUsername = null;
            asset.CredentialPassword = null;
            asset.HasDefaultValue = true;
            return true;
        }

        bool isDirty = false;
        if (!string.IsNullOrEmpty(param.CredentialUsername) && asset.CredentialUsername != param.CredentialUsername)
        {
            asset.CredentialUsername = param.CredentialUsername;
            isDirty = true;
        }

        // Only update password when a non-empty value is explicitly specified.
        // Empty string "" means "not specified" (e.g., from CSV export where passwords are masked).
        if (!string.IsNullOrEmpty(param.CredentialPassword))
        {
            asset.CredentialPassword = param.CredentialPassword;
            asset.HasDefaultValue = true;
            isDirty = true;
        }
        return isDirty;
    }

    // Empty-row trigger: no username and password/externalName explicitly blanked.
    protected override bool IsPerRobotValueEmpty(SetCredentialAssetCommandParameter param)
        => string.IsNullOrEmpty(param.CredentialUsername) && (param.CredentialPassword == "" || param.ExternalName == "");

    // Credential clears the per-robot entry on an empty row.
    protected override bool AllowPerRobotRemoval => true;

    protected override bool ApplyPerRobotValue(AssetUserValue userValue, SetCredentialAssetCommandParameter param)
    {
        if (!string.IsNullOrEmpty(param.ExternalName))
        {
            userValue.ExternalName = param.ExternalName;
            userValue.CredentialUsername = null;
            userValue.CredentialPassword = null;
            return true;
        }

        bool isDirty = false;
        if (userValue.CredentialUsername != param.CredentialUsername && !string.IsNullOrEmpty(param.CredentialUsername))
        {
            userValue.CredentialUsername = param.CredentialUsername;
            isDirty = true;
        }
        if (!string.IsNullOrEmpty(param.CredentialPassword))
        {
            userValue.CredentialPassword = param.CredentialPassword;
            isDirty = true;
        }
        return isDirty;
    }

    protected override void ApplyDefaultParameterSetValues(SetCredentialAssetCommandParameter param)
    {
        if (ParameterSetName == Default)
        {
            param.CredentialUsername = Credential!.UserName;
            param.CredentialPassword = ConvertToUnsecureString(Credential!.Password);
        }
    }

    protected override string ProgressActivity => "Updating credential assets";

    protected override void NormalizeBeforeFlush(Asset asset)
    {
        if (asset.CredentialUsername == "")
        {
            asset.CredentialUsername = null;
        }
        if (string.IsNullOrEmpty(asset.CredentialPassword))
        {
            asset.CredentialStoreId = null;
        }
    }
}
