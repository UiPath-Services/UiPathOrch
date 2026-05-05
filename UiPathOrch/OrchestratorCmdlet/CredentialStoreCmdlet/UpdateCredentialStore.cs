using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchCredentialStore", SupportsShouldProcess = true)]
public class UpdateCredentialStoreCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? HostName { get; set; }

    // Provider-specific JSON config containing secrets (e.g., CyberArk URL/API key,
    // Azure Key Vault credentials). Set this to re-supply secrets after migration —
    // the GET API returns AdditionalConfiguration with secret fields masked, so
    // unless the user provides a fresh value here, the existing server-side
    // configuration is left untouched (we never echo the masked value back).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? AdditionalConfiguration { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            ICollection<CredentialStore>? stores;
            try
            {
                stores = drive.CredentialStores.Get();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCredentialStoreError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var store in stores
                .FilterByWildcards(s => s?.Name, wpName)
                .OrderBy(s => s.Name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string target = store.GetPSPath();

                // Fetch full details (the list endpoint omits Type and possibly other
                // fields needed for PUT). The single-store GET still masks secret
                // fields inside AdditionalConfiguration.
                CredentialStore? detailed;
                try
                {
                    detailed = drive.OrchAPISession.GetCredentialStore(store.Id!.Value);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "GetCredentialStoreError", ErrorCategory.InvalidOperation, store));
                    continue;
                }
                if (detailed is null) continue;

                // The AdditionalConfiguration returned from the server contains masked
                // secret values — set it to null on the deep copy so PUT does not echo
                // those masked values back. Only re-include it if the user provided one.
                detailed.AdditionalConfiguration = null;

                var posting = OrchCollectionExtensions.DeepCopy(detailed);
                posting.Id = store.Id;
                bool dirty = false;

                dirty |= posting.AssignStringIfNotNull(NewName, detailed, s => s.Name, (s, v) => s.Name = v);
                dirty |= posting.AssignStringIfNotNull(HostName, detailed, s => s.HostName, (s, v) => s.HostName = v);

                if (!string.IsNullOrEmpty(AdditionalConfiguration))
                {
                    posting.AdditionalConfiguration = AdditionalConfiguration;
                    dirty = true;
                }

                if (!dirty) continue;

                if (ShouldProcess(target, "Update CredentialStore"))
                {
                    try
                    {
                        drive.OrchAPISession.PutCredentialStore(posting);
                        drive.CredentialStores.ClearCache();
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateCredentialStoreError", ErrorCategory.InvalidOperation, store));
                    }
                }
            }
        }
    }
}
