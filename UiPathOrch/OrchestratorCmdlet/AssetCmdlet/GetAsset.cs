
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAsset")]
[OutputType(typeof(Entities.Asset))]
public class GetAssetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetValueTypeCompleter))]
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
        // Write data rows for each asset. Description is written on the first row of each
        // asset only; subsequent rows leave the column empty. Set-OrchAsset's MergeDescription
        // tolerates this: empty cells lose to the non-empty first row, so CSV roundtrip is
        // lossless.
        foreach (var asset in output.Where(a => a.ValueType != "Credential" && a.ValueType != "Secret"))
        {
            bool isDescriptionOut = false;

            if (!string.IsNullOrEmpty(asset.Value))
            {
                isDescriptionOut = true;
                string?[] line = [
                    EscapeCsvValue(asset.Path, true),
                    EscapeCsvValue(asset.Name, true),
                    EscapeCsvValue(asset.Description),
                    asset.ValueType,
                    EscapeCsvValue(asset.Value),
                    "",
                    ""
                ];
                writer.WriteCsvLine(line);
            }

            if (asset.UserValues is not null)
            {
                foreach (var userValue in asset.UserValues)
                {
                    string? description = isDescriptionOut ? "" : asset.Description;
                    isDescriptionOut = true;
                    string? value = userValue.ValueType == "Bool" ? userValue.Value?.ToUpper() : userValue.Value;

                    string?[] line = [
                        EscapeCsvValue(userValue.Path, true),
                        EscapeCsvValue(userValue.Name, true),
                        EscapeCsvValue(description),
                        userValue.ValueType,
                        EscapeCsvValue(value),
                        EscapeCsvValue(userValue.UserName, true),
                        EscapeCsvValue(userValue.MachineName, true)
                    ];

                    writer.WriteCsvLine(line);
                }
            }
        }
    }

    private void WriteCredentialCsvContent(StreamWriter writer, IEnumerable<Asset> output)
    {
        // Write data rows for each asset
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

            // Description is written on the first row of each asset only; subsequent rows leave
            // the column empty. See SetCredentialAsset.MergeDescription for the importer rule.
            bool isDescriptionOut = false;

            if (!string.IsNullOrEmpty(asset.Value))
            {
                isDescriptionOut = true;
                writer.WriteCsvLine(BuildCredentialCsvRow(
                    asset.Path, asset.Name, asset.Description, credentialStore,
                    null, null, asset.CredentialUsername, asset.ExternalName));
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

                    string? description = isDescriptionOut ? "" : asset.Description;
                    isDescriptionOut = true;

                    writer.WriteCsvLine(BuildCredentialCsvRow(
                        userValue.Path, asset.Name, description, credentialStore,
                        userValue.UserName, userValue.MachineName, userValue.CredentialUsername, userValue.ExternalName));
                }
            }
        }
    }

    // Builds one credential-CSV row in CsvCredentialHeaders column order: Path, Name,
    // Description, CredentialStore, UserName, MachineName, CredentialUsername,
    // CredentialPassword, ExternalName. CredentialPassword is always empty (passwords are
    // never exported). Pure / static so the per-field escaping is unit-testable without a
    // live drive — the credential-store NAME lookup (which needs SessionState) is resolved
    // by the caller and passed in as credentialStore.
    internal static string?[] BuildCredentialCsvRow(
        string? path, string? name, string? description, string? credentialStore,
        string? userName, string? machineName, string? credentialUsername, string? externalName)
        => [
            EscapeCsvValue(path, true),
            EscapeCsvValue(name, true),
            EscapeCsvValue(description),
            EscapeCsvValue(credentialStore, true),
            EscapeCsvValue(userName, true),
            EscapeCsvValue(machineName, true),
            EscapeCsvValue(credentialUsername),
            "",
            EscapeCsvValue(externalName),
        ];

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
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
                    // Output only global values
                    WriteObject(output, true);
                }
                else
                {
                    foreach (var asset in output)
                    {
                        // Output the global value as AssetUserValue type
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
