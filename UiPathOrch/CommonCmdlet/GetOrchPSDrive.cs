using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPSDrive")]
[OutputType(typeof(Entities.OrchPSDrive))]
public class GetOrchPSDriveCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    // If we could create an OrchDriveInfoBase class, this could be implemented more cleanly.
    //private void EnumDrives<T>() where T : OrchDriveInfoBase
    //{
    //    IEnumerable<T> drives = null;
    //    if (Path is null)
    //    {
    //        drives = T.EnumAllOrchDrives();
    //    }
    //    else
    //    {
    //        drives = T.EnumOrchDrives(Path);
    //    }
    //}

    private void ConnectToOrchDrive(OrchDriveInfo drive)
    {
        if (Force.IsPresent)
        {
            try
            {
                drive.OrchAPISession.EnsureAuthenticated();
                drive.GetPartitionGlobalId();
                // Confidential apps skip GetCurrentUser(), so _tenantId / _tenantKey
                // are not populated as a side effect of authentication. Trigger the
                // /odata/Users + /odata/Users(id) fallback explicitly so -Force returns
                // a fully populated OrchPSDrive for both Conf and Non-Conf apps.
                drive.GetTenantId();
                // Populate the product-version cache so the ProductVersion column is
                // filled under -Force. The OrchPSDrive ctor reads it passively (CachedValue).
                drive.ProductVersion.Get();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetOrchPSDriveError", ErrorCategory.InvalidOperation, drive));
            }
            return;
        }

        // No -Force: never trigger auth on a cold drive (that would pop PKCE for a drive
        // the user merely listed). But a drive that is ALREADY authenticated can fetch the
        // org-global /api/Status/Version with one cheap call on the existing token — do it,
        // otherwise ProductVersion stays blank for connected drives because the OrchPSDrive
        // ctor only reads the cache passively. IsAuthenticated is a side-effect-free
        // token-presence check (no PKCE) — the same gate Clear-OrchCache uses.
        if (drive.IsAuthenticated)
        {
            try
            {
                drive.ProductVersion.Get();
            }
            catch (Exception ex)
            {
                // Best-effort enrichment: leave ProductVersion blank for this drive
                // rather than failing the whole listing over a transient version fetch.
                WriteVerbose($"{drive.NameColon}: could not fetch ProductVersion: {ex.Message}");
            }
        }
    }

    private void WriteOrchProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllOrchDrives();
        else drives = SessionState.EnumOrchDrives(effectivePath);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteDuProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchDuDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllDuDrives();
        else drives = SessionState.EnumDuDrives(effectivePath);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive.ParentDrive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    private void WriteTmProviderInfo()
    {
        var effectivePath = EffectivePath(Path, LiteralPath);
        IEnumerable<OrchTmDriveInfo> drives;
        if (effectivePath is null) drives = SessionState.EnumAllTmDrives();
        else drives = SessionState.EnumTmDrives(effectivePath);

        foreach (var drive in drives)
        {
            ConnectToOrchDrive(drive.ParentDrive);
            WriteObject(new OrchPSDrive(drive));
        }
    }

    protected override void ProcessRecord()
    {
        WriteOrchProviderInfo();
        WriteDuProviderInfo();
        WriteTmProviderInfo();
    }
}
