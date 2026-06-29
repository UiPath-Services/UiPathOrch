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

    // Decision rule (factored out for unit testing) for whether Get-OrchPSDrive
    // should actively fetch the org-global ProductVersion (/api/Status/Version):
    //   -Force                            -> yes (the full connect authenticates the drive).
    //   no -Force, already authenticated  -> yes (one cheap call on the existing token;
    //                                         fills ProductVersion for connected drives).
    //   no -Force, cold drive             -> NO (fetching would trigger auth/PKCE for a
    //                                         drive the user merely listed).
    internal static bool ShouldFetchProductVersion(bool force, bool isAuthenticated)
        => force || isAuthenticated;

    private void ConnectToOrchDrive(OrchDriveInfo drive)
    {
        bool force = Force.IsPresent;

        // -Force performs a full connect first (auth + identity) so the drive ends up
        // authenticated and every column is populated. Without -Force we never touch a
        // cold drive's network — that would pop PKCE for a drive the user merely listed.
        if (force)
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
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetOrchPSDriveError", ErrorCategory.InvalidOperation, drive));
                return;
            }
        }

        // IsAuthenticated is a side-effect-free token-presence check (no PKCE) — the same
        // gate Clear-OrchCache uses. The OrchPSDrive ctor only reads ProductVersion
        // passively (CachedValue), so this Get() is what fills it for connected drives.
        if (!ShouldFetchProductVersion(force, drive.IsAuthenticated)) return;

        try
        {
            drive.ProductVersion.Get();
        }
        catch (Exception ex)
        {
            // Under -Force a failure is a real error; without it the fetch is best-effort
            // enrichment, so leave ProductVersion blank rather than failing the listing.
            if (force)
                WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetOrchPSDriveError", ErrorCategory.InvalidOperation, drive));
            else
                WriteVerbose($"{drive.NameColon}: could not fetch ProductVersion: {ex.Message}");
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
