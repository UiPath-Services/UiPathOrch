using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

// この cmdlet は動作しない。GetCredential で取得できる store には
// 作成時に必要となるデータが一部含まれていない。
// 一応、private として残しておく。

[Cmdlet(VerbsCommon.Copy, "OrchCredentialStore", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.CredentialStore))]
public class CopyCredentialStoreCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Path { get; set; }

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

    internal static void CopyCredentialStores(
        IWritableHost _this,
        OrchDriveInfo srcDrive, List<WildcardPattern>? wpName,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
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
            _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetCredentialStoreError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        using var reporter = new ProgressReporter(_this, 1, 100, "Copying credential stores");

        int index = 0;
        reporter.TotalNum = dstDrives.Count * stores.Count;

        foreach (var dstDrive in dstDrives)
        {
            foreach (var store in stores)
            {
                cancelToken.ThrowIfCancellationRequested();
                reporter.WriteProgress(++index, $"{store.GetPSPath()} to {dstDrive.NameColonSeparator}");

                // コピー元の store が "Orchestrator Database" で、かつコピー先に同名の store がある場合にはスキップ
                if (string.Compare(store.Name, "Orchestrator Database", true) == 0)
                {
                    var dstStores = dstDrive.CredentialStores.Get();
                    var existingStore = dstStores.FirstOrDefault(s => string.Compare(s.Name, "Orchestrator Database", true) == 0);
                    if (existingStore is not null) continue;
                }

                var target = $"Item: {store.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";

                if (shouldProcess || _this.ShouldProcess(store.GetPSPath(), "Copy CredentialStore"))
                {
                    CredentialStore postingStore = OrchCollectionExtensions.DeepCopy(store);
                    // postingStore.Path = null; // JsonIgnore 属性がついているので不要
                    postingStore.Id = null;

                    try
                    {
                        var createdStore = dstDrive.OrchAPISession.CreateCredentialStore(postingStore);
                        if (createdStore is not null)
                        {
                            dstDrive.CredentialStores.ClearCache();
                            //createdStore.Path = dstDrive.NameColonSeparator;
                            //WriteObject(createdStore);

                            var keys = FindKeysWithSearchText(createdStore.AdditionalConfiguration, "•");
                            string strKeys = string.Join(", ", keys);
                            if (!string.IsNullOrEmpty(strKeys))
                            {
                                createdStore.Path = dstDrive.NameColonSeparator;
                                _this.WriteWarning($"'{createdStore.GetPSPath()}': Please update '{strKeys}' manually.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(store.GetPSPath(), ex), "CreateCredentialStoreError", ErrorCategory.InvalidOperation, postingStore));
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetOrchDrive(Path);

        var dstDrives = SessionState.EnumDestinationDrives(Destination!);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyCredentialStores(this, srcDrive, wpName, dstDrives, false, cancelHandler.Token);
    }
}
