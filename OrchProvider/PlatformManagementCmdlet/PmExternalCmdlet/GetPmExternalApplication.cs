using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmExternalApplication")]
[OutputType(typeof(Entities.ExternalClient))]
public class GetPmExternalApplicationCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExternalApplicationNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmExternalClients.Get();
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(a => a?.name, wpName)
                    .OrderBy(a => a!.name),
                    true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmExternalApplicationError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
