using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCredentialStore", SupportsShouldProcess = true)]
public class RemoveCredentialStoreCommand : RemoveDriveEntityCmdletBase<CredentialStore>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "CredentialStore";
    protected override Func<CredentialStore?, string?> GetName => c => c?.Name;
    protected override Func<CredentialStore, string> GetPSPath => c => c.GetPSPath();

    protected override IEnumerable<CredentialStore> GetEntities(OrchDriveInfo drive)
        => drive.CredentialStores.Get();

    protected override void Remove(OrchDriveInfo drive, CredentialStore cs)
    {
        drive.OrchAPISession.RemoveCredentialStore(cs.Id ?? 0);
        drive.CredentialStores.ClearCache();
    }
}
