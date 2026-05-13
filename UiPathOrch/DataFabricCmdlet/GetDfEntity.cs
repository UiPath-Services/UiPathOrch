using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: kept internal so PowerShell module loader does not register the cmdlet.
// The Data Fabric REST surface is preview / internal-only and we don't want to
// surface it to module users yet. Re-enable by switching `class` to `public class`
// and adding `Get-OrchDfEntity` to UiPathOrch.psd1 CmdletsToExport.
[Cmdlet(VerbsCommon.Get, "OrchDfEntity")]
[OutputType(typeof(DfEntity))]
class GetDfEntityCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DfEntityNameCompleter))]
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
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth, includeRoot: true);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.DfEntities.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name),
                    enumerateCollection: true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDfEntityError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
