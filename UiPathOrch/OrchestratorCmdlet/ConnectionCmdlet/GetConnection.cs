using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: the Connection Service requires the internal "ConnectionService" scope,
// which Identity Server does not expose to External Applications or PATs. Verified
// 2026-04-27 against scopes_supported (only IS.Triggers.Read/Write are public).
// The class is kept non-public and absent from psd1 CmdletsToExport so PowerShell
// cmdlet discovery skips it. Re-enable by switching to `public` and adding the
// name to UiPathOrch.psd1 once the scope ships.
[Cmdlet(VerbsCommon.Get, "OrchConnection")]
[OutputType(typeof(Connection))]
class GetConnectionCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Connections.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var connections = result.GetResult(cancelHandler.Token);
                if (connections is null) continue;

                WriteObject(connections
                    .FilterByWildcards(s => s?.name, wpName)
                    .OrderBy(s => s.name),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetConnectionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
