using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using System.Text.Json.Nodes;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    // この cmdlet は動作しない。GetCredential で取得できる store には
    // 作成時に必要となるデータが一部含まれていない。
    // 一応、private として残しておく。

    [Cmdlet(VerbsCommon.Copy, "OrchCredentialStore", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.CredentialStore))]
    public class CopyCredentialStoreCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(CredentialStoreNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        [SupportsWildcards]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string? Path { get; set; }

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.Name_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.Name_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        static IEnumerable<string> FindKeysWithSearchText(string? jsonString, string searchText)
        {
            if (string.IsNullOrEmpty(jsonString)) yield break;

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                {
                    string value = property.Value.ToString();
                    if (value.Contains(searchText))
                    {
                        yield return property.Name;
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var srcDrive = OrchDriveInfo.GetOrchDrive(Path);

            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);
            var wpName = Name.ConvertToWildcardPatternList();

            srcDrive.CredentialStores.ClearCache();

            // この実装はこれで良い
            List<CredentialStore> stores = null;
            try
            {
                stores = srcDrive.CredentialStores.Get()
                    .FilterByWildcards(s => s?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetCredentialStoreError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }


            string msg = "Copying credential stores";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            int index = 0;
            reporter.TotalNum = dstDrives.Count * stores.Count;

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var dstDrive in dstDrives)
            {
                foreach (var store in stores)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var target = $"Item: {store.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";

                    reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {store.GetPSPath()} to {dstDrive.NameColonSeparator}");

                    if (ShouldProcess(store.GetPSPath(), "Copy CredentialStore"))
                    {
                        CredentialStore postingStore = OrchCollectionExtensions.DeepCopy(store);
                        // postingStore.Path = null; // JsonIgnore 属性がついているので不要
                        postingStore.Id = null;

                        try
                        {
                            var createdStore = dstDrive.OrchAPISession.CreateCredentialStore(postingStore);
                            if (createdStore != null)
                            {
                                dstDrive.CredentialStores.ClearCache();
                                //createdStore.Path = dstDrive.NameColonSeparator;
                                //WriteObject(createdStore);

                                var keys = FindKeysWithSearchText(createdStore.AdditionalConfiguration, "•");
                                string strKeys = string.Join(", ", keys);
                                if (!string.IsNullOrEmpty(strKeys))
                                {
                                    createdStore.Path = dstDrive.NameColonSeparator;
                                    WriteWarning($"'{createdStore.GetPSPath()}': Please update '{strKeys}' manually.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(store.GetPSPath(), ex), "CreateCredentialStoreError", ErrorCategory.InvalidOperation, postingStore));
                        }
                    }
                }
            }
        }
    }
}
