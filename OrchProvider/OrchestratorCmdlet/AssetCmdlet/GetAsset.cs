
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAsset")]
[OutputType(typeof(Entities.Asset))]
public class GetAssetCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetValueTypeCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? ValueType { get; set; }

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
    public string? ExportCredentialCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedAssets.csv";
    private static readonly string DefaultCredentialCsvName = "ExportedCredentialAssets.csv";

    private static readonly string[] CsvHeaders = ["Path", "Name", "Description", "ValueType", "Value", "UserName", "MachineName"];
    private static readonly string[] CsvCredentialHeaders = ["Path", "Name", "Description", "CredentialStore", "UserName", "MachineName", "CredentialUsername", "CredentialPassword", "ExternalName"];

    private static void WriteCsvContent(StreamWriter writer, IEnumerable<Asset> output)
    {
        // 各アセットに対してデータ行を書き込む
        foreach (var asset in output.Where(a => a.ValueType != "Credential"))
        {
            var line = new StringBuilder();
            bool isDescriptionOut = false;

            if (!string.IsNullOrEmpty(asset.Value))
            {
                isDescriptionOut = true;
                line.Append($"{EscapeCsvValue(asset.Path, true)},");
                line.Append($"{EscapeCsvValue(asset.Name, true)},");
                line.Append($"{asset.Description},");
                line.Append($"{asset.ValueType},");
                line.Append($"{asset.Value!},");
                line.Append($"{EscapeCsvValue("")},");
                line.Append($"{EscapeCsvValue("")}");
                writer.WriteLine(line.ToString());
            }

            if (asset.UserValues is not null)
            {
                foreach (var userValue in asset.UserValues)
                {
                    line = new StringBuilder();
                    line.Append($"{EscapeCsvValue(userValue.Path, true)},");
                    line.Append($"{EscapeCsvValue(userValue.Name, true)},");
                    if (isDescriptionOut)
                    {
                        isDescriptionOut = true;
                        line.Append($"{EscapeCsvValue("")},");
                    }
                    else
                    {
                        line.Append($"{asset.Description!},");
                    }
                    line.Append($"{userValue.ValueType!},");
                    if (userValue.ValueType == "Bool")
                        line.Append($"{userValue.Value!.ToUpper()},");
                    else
                        line.Append($"{userValue.Value!},");
                    line.Append($"{EscapeCsvValue(userValue.UserName, true)},");
                    line.Append($"{EscapeCsvValue(userValue.MachineName, true)}");
                    writer.WriteLine(line.ToString());
                }
            }
        }
    }

    private void WriteCredentialCsvContent(StreamWriter writer, IEnumerable<Asset> output)
    {
        // 各アセットに対してデータ行を書き込む
        foreach (var asset in output.Where(a => a.ValueType == "Credential"))
        {
            #region find the name of CredentialStore
            ICollection<CredentialStore> credentialStores = null;

            string credentialStore = "";
            var psPath = SessionState.Path.GetResolvedPSPathFromPSPath(asset.Path).FirstOrDefault();
            if (psPath is not null)
            {
                OrchDriveInfo drive = psPath.Drive as OrchDriveInfo;
                if (drive is not null)
                {
                    credentialStores = drive.CredentialStores.Get();
                    credentialStore = credentialStores.FirstOrDefault(cs => cs.Id == asset.CredentialStoreId)?.Name ?? "";
                }
            }
            #endregion find the name of CredentialStore

            var line = new StringBuilder();
            bool isDescriptionOut = false;

            if (!string.IsNullOrEmpty(asset.Value))
            {
                isDescriptionOut = true;
                line.Append($"{EscapeCsvValue(asset.Path, true)},");
                line.Append($"{EscapeCsvValue(asset.Name, true)},");
                line.Append($"{asset.Description!},");
                line.Append($"{EscapeCsvValue(credentialStore, true)},,,");
                line.Append($"{asset.CredentialUsername},,");
                line.Append($"{asset.ExternalName}");
                writer.WriteLine(line.ToString());
            }

            if (asset.UserValues is not null)
            {
                foreach (var userValue in asset.UserValues)
                {
                    credentialStore = "";
                    if (credentialStores is not null)
                    {
                        credentialStore = credentialStores.FirstOrDefault(cs => cs.Id == userValue.CredentialStoreId)?.Name ?? "";
                    }

                    line = new StringBuilder();
                    line.Append($"{EscapeCsvValue(userValue.Path, true)},");
                    line.Append($"{EscapeCsvValue(asset.Name, true)},");
                    if (isDescriptionOut)
                    {
                        isDescriptionOut = true;
                        line.Append(",");
                    }
                    else
                    {
                        line.Append($"{asset.Description!},");
                    }
                    line.Append($"{EscapeCsvValue(credentialStore, true)},");
                    line.Append($"{EscapeCsvValue(userValue.UserName, true)},");
                    line.Append($"{EscapeCsvValue(userValue.MachineName, true)},");
                    line.Append($"{userValue.CredentialUsername!},,");
                    line.Append($"{userValue.ExternalName}");
                    writer.WriteLine(line.ToString());
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpValueType = ValueType.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        var (physicalCredentialCsvPath, providerCredentialCsvPath) = GenerateCsvFilePath(ExportCredentialCsv, SessionState, DefaultCredentialCsvName);
        using var writerCredential = WriteCsvHeader(physicalCredentialCsvPath, CsvEncoding, CsvCredentialHeaders);

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
                    .FilterByWildcards(a => a?.ValueType, wpValueType)
                    .FilterByWildcards(m => m?.Name, wpName)
                    //.OrderBy(m => m.ValueType)
                    .OrderBy(m => m.Name);

                if (writer is not null || writerCredential is not null)
                {
                    if (writer is not null)
                    {
                        WriteCsvContent(writer, output);
                    }
                    if (writerCredential is not null)
                    {
                        WriteCredentialCsvContent(writerCredential, output);

                    }
                }
                else if (!ExpandUserValues.IsPresent)
                {
                    // global 値のみ出力する
                    WriteObject(output, true);
                }
                else
                {
                    foreach (var asset in output)
                    {
                        // global 値を AssetUserValue 型で出力する
                        if (!string.IsNullOrEmpty(asset.Value))
                        {
                            AssetUserValue globalValue = new()
                            {
                                Name = asset.Name,
                                ValueType = asset.ValueType,
                                StringValue = asset.StringValue,
                                BoolValue = asset.BoolValue,
                                IntValue = asset.IntValue,
                                Value = asset.Value,
                                CredentialUsername = asset.CredentialUsername,
                                CredentialPassword = asset.CredentialPassword,
                                CredentialStoreId = asset.CredentialStoreId,
                                ExternalName = asset.ExternalName,
                                KeyValueList = asset.KeyValueList,
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
                WriteError(new ErrorRecord(ex, "GetAssetError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
        if (!string.IsNullOrEmpty(ExportCredentialCsv))
        {
            WriteCSVExportedMessage(this, providerCredentialCsvPath);
        }
    }
}
