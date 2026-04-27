using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchDfEntity")]
[OutputType(typeof(DfEntity))]
public class GetDfEntityCommand : OrchestratorPSCmdlet
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

        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var entities = drive.DfEntities.Get(folder);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name),
                    enumerateCollection: true);
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetDfEntityError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
