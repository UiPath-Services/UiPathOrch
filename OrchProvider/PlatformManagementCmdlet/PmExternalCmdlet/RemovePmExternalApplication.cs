using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmExternalApplication", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ExternalClient))]
public class RemovePmExternalApplicationCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExternalApplicationNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmExternalClients.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                foreach (var app in entities
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.name))
                {
                    string target = app.GetPSPath();
                    var drive = result.Source;

                    if (!Force)
                    {
                        var myAppId = drive._psDrive.AppId;
                        if (app.id == myAppId) continue;
                    }

                    if (ShouldProcess(target, "Remove PmExternalApplication"))
                    {
                        try
                        {
                            drive.OrchAPISession.DeletePmExternalClient(drive.GetPartitionGlobalId() ?? "", app.id ?? "");
                            drive.PmExternalClients.ClearCache();
                            drive.PmGroups.ClearCache();
                            drive._dicSearchPmDirectory = null;
                            drive._dicSearchDirectory = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "GetPmExternalApplicationError", ErrorCategory.InvalidOperation, drive));
                        }
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmExternalApplicationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
