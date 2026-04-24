using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchSecretAsset")]
[OutputType(typeof(Asset))]
public class GetSecretAssetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter ExpandUserValues { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedSecretAssets.csv";
    // SecretValue is always returned masked by the API. The column is kept so users can fill
    // values locally and re-import via Set-OrchSecretAsset without reshaping the CSV.
    private static readonly string[] CsvHeaders = ["Path", "Name", "Description", "CredentialStore", "UserName", "MachineName", "SecretValue", "ExternalName"];

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
            var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var asset in result
                    .Where(a => a.ValueType == "Secret")
                    .Where(a => wp.IsMatch(a.Name))
                    .ExcludeByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name))
                {
                    string tip = asset.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, tip);
                }
            }
        }
    }

    private void WriteCsvContent(StreamWriter writer, IEnumerable<Asset> output, OrchDriveInfo drive)
    {
        var credentialStores = drive.CredentialStores.Get();

        foreach (var asset in output)
        {
            string credentialStore = credentialStores.FirstOrDefault(cs => cs.Id == asset.CredentialStoreId)?.Name ?? "";
            bool isDescriptionOut = false;

            // Secret never populates Value (always null by API). Use HasDefaultValue to detect a
            // Global-scope secret; without this, Global secrets would only appear as a header row.
            if (asset.HasDefaultValue.GetValueOrDefault())
            {
                isDescriptionOut = true;
                var line = new StringBuilder();
                line.Append($"{EscapeCsvValue(asset.Path, true)},");
                line.Append($"{EscapeCsvValue(asset.Name, true)},");
                line.Append($"{EscapeCsvValue(asset.Description)},");
                line.Append($"{EscapeCsvValue(credentialStore, true)},,,,"); // UserName, MachineName, SecretValue (always masked)
                line.Append($"{EscapeCsvValue(asset.ExternalName)}");
                writer.WriteLine(line.ToString());
            }

            if (asset.UserValues is not null)
            {
                foreach (var userValue in asset.UserValues)
                {
                    string uvStore = credentialStores.FirstOrDefault(cs => cs.Id == userValue.CredentialStoreId)?.Name ?? "";

                    var line = new StringBuilder();
                    line.Append($"{EscapeCsvValue(userValue.Path, true)},");
                    line.Append($"{EscapeCsvValue(asset.Name, true)},");
                    if (isDescriptionOut)
                    {
                        line.Append(",");
                    }
                    else
                    {
                        isDescriptionOut = true;
                        line.Append($"{EscapeCsvValue(asset.Description)},");
                    }
                    line.Append($"{EscapeCsvValue(uvStore, true)},");
                    line.Append($"{EscapeCsvValue(userValue.UserName, true)},");
                    line.Append($"{EscapeCsvValue(userValue.MachineName, true)},,"); // SecretValue (always empty)
                    line.Append($"{EscapeCsvValue(userValue.ExternalName)}");
                    writer.WriteLine(line.ToString());
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Assets.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var assets = result.GetResult(cancelHandler.Token);
                if (assets is null) continue;

                var output = assets
                    .Where(a => a.ValueType == "Secret")
                    .FilterByWildcards(m => m?.Name, wpName)
                    .OrderBy(m => m.Name)
                    .ToList();

                if (writer is not null)
                {
                    WriteCsvContent(writer, output, result.Source.drive);
                }
                else if (!ExpandUserValues.IsPresent)
                {
                    WriteObject(output, true);
                }
                else
                {
                    foreach (var asset in output)
                    {
                        // Secret.Value is always null from the API (masked). Use HasDefaultValue
                        // to detect a configured Global-scope secret; without this, -ExpandUserValues
                        // would drop the Global row entirely for Secret assets.
                        if (asset.HasDefaultValue.GetValueOrDefault())
                        {
                            AssetUserValue globalValue = new()
                            {
                                Name = asset.Name,
                                ValueType = asset.ValueType,
                                SecretValue = asset.SecretValue,
                                CredentialStoreId = asset.CredentialStoreId,
                                ExternalName = asset.ExternalName,
                                Id = asset.Id,
                                Path = asset.Path,
                                PathName = asset.GetPSPath()
                            };
                            WriteObject(globalValue);
                        }

                        if (asset.UserValues is not null && asset.UserValues.Count != 0)
                        {
                            WriteObject(asset.UserValues, true);
                        }
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetOrchSecretAssetError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
