using System.Diagnostics;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    // Invoke-Item on a folder opens that folder in the Orchestrator web UI (default browser),
    // scoped to the drive's tenant and the folder id.
    protected override void InvokeDefaultAction(string path)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        string endpoint = drive.OrchAPISession._base_url;

        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        Folder folder = drive.GetFolder(orchPath);

        var (tenantId, _) = drive.GetTenantId();
        bool bQuery = false;
        //if (drive.OrchAPISession.ApiVersion < 12 && tenantId.HasValue)
        if (tenantId.HasValue)
        {
            endpoint += $"?tid={tenantId.Value}";
            bQuery = true;
        }

        if (folder is not null && folder.Id.HasValue && folder.Id! != 0)
        {
            endpoint += bQuery ? '&' : '?';
            endpoint += $"fid={folder.Id}";
        }

        Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
    }
}
