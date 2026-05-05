using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

// This cmdlet does not work. The store retrieved by GetCredential does not
// contain some data required for creation.
// Keeping it as private for now.

[Cmdlet(VerbsCommon.Copy, "OrchCredentialStore", SupportsShouldProcess = true)]
public class CopyCredentialStoreCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
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

        // This implementation is fine as is
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

                // Skip if the source store is "Orchestrator Database" and a store with the same name exists at the destination
                if (string.Compare(store.Name, "Orchestrator Database", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var dstStores = dstDrive.CredentialStores.Get();
                    var existingStore = dstStores.FirstOrDefault(s => string.Compare(s.Name, "Orchestrator Database", StringComparison.OrdinalIgnoreCase) == 0);
                    if (existingStore is not null) continue;
                }

                var target = $"Item: {store.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";

                if (shouldProcess || _this.ShouldProcess(store.GetPSPath(), "Copy CredentialStore"))
                {
                    CredentialStore postingStore = OrchCollectionExtensions.DeepCopy(store);
                    // postingStore.Path = null; // Not needed because it has the JsonIgnore attribute
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
                                _this.WriteWarning($"'{createdStore.GetPSPath()}': Please update '{strKeys}' in AdditionalConfiguration with Update-OrchCredentialStore cmdlet.");
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
