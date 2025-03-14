using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.EntityType_Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Resolve, "OrchPmDirectoryNameBulk")]
[OutputType(typeof(DirectoryUser))]
[OutputType(typeof(DirectoryGroup))]
[OutputType(typeof(DirectoryApplication))]
public class SearchPmDirectoryBulkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<User_Group_Application>))]
    public string? EntityType { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmDirectoryNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmBulkResolveByName(EntityType!, Name!, name => name));

        string viewName = EntityType!.ToLower() switch
        {
            "user" => "UiPath.PowerShell.Entities.DirectoryUser",
            "group" => "UiPath.PowerShell.Entities.DirectoryGroup",
            "application" => "UiPath.PowerShell.Entities.DirectoryApplication",
            _ => null
        };

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var directoryObjects = result.GetResult(cancelHandler.Token);
                if (directoryObjects is null) continue;

                foreach (var directoryObject in directoryObjects.Values.Where(v => v is not null).OrderBy(v => v!.name))
                {
                    var psObject = new PSObject(directoryObject);
                    psObject.TypeNames.Clear();
                    psObject.TypeNames.Add(viewName);
                    WriteObject(psObject);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "ResolvePmDirectoryNameBulkError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
