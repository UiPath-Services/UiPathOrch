    using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchAsset")]
    [OutputType(typeof(Entities.Asset))]
    public class GetAssetCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(AssetNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(AssetValueTypeCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? ValueType { get; set; }

        [Parameter]
        public SwitchParameter ExpandUserValues { get; set; }

        [Parameter]
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

                if (asset.UserValues != null)
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
                ReadOnlyCollection<CredentialStore> credentialStores = null;

                string credentialStore = "";
                var psPath = SessionState.Path.GetResolvedPSPathFromPSPath(asset.Path).FirstOrDefault();
                if (psPath != null)
                {
                    OrchDriveInfo drive = psPath.Drive as OrchDriveInfo;
                    if (drive != null)
                    {
                        credentialStores = drive.GetCredentialStores();
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

                if (asset.UserValues != null)
                {
                    foreach (var userValue in asset.UserValues)
                    {
                        credentialStore = "";
                        if (credentialStores != null)
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

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            ExportCredentialCsv = GenerateCsvFilePath(ExportCredentialCsv, SessionState, DefaultCredentialCsvName);
            using var writerCredential = WriteCsvHeader(ExportCredentialCsv, CsvEncoding, CsvCredentialHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetAssets(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var assets = result.GetResult(cancelHandler.Token);
                    if (assets == null) continue;

                    var output = assets
                        .FilterByWildcards(a => a?.ValueType, wpValueType)
                        .FilterByWildcards(m => m?.Name, wpName)
                        //.OrderBy(m => m.ValueType)
                        .OrderBy(m => m.Name);

                    if (writer != null || writerCredential != null)
                    {
                        if (writer != null)
                        {
                            WriteCsvContent(writer, output);
                        }
                        if (writerCredential != null)
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

                            if (asset.UserValues != null && asset.UserValues.Count != 0)
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
                WriteCSVExportedMessage(this, ExportCsv);
            }
            if (!string.IsNullOrEmpty(ExportCredentialCsv))
            {
                WriteCSVExportedMessage(this, ExportCredentialCsv);
            }
        }
    }
}
