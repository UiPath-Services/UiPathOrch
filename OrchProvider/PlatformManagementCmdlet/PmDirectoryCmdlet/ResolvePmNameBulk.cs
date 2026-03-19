using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.EntityType_Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Resolve, "PmDirectoryNameBulk")]
[OutputType(typeof(DirectoryUser))]
[OutputType(typeof(DirectoryGroup))]
[OutputType(typeof(DirectoryApplication))]
public class SearchPmDirectoryBulkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<User_Group_Application>))]
    public string? EntityType { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmDirectoryNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        string viewName = EntityType!.ToLower() switch
        {
            "user" => "UiPath.PowerShell.Entities.DirectoryUser",
            "group" => "UiPath.PowerShell.Entities.DirectoryGroup",
            "application" => "UiPath.PowerShell.Entities.DirectoryApplication",
            _ => null
        };

        foreach (var drive in drives)
        {
            try
            {
                var directoryObjects = drive.PmBulkResolveByName(EntityType!, Name!, name => name);
                if (directoryObjects is null) continue;

                foreach (var directoryObject in directoryObjects.Values.Where(v => v is not null).OrderBy(v => v!.name))
                {
                    var psObject = new PSObject(directoryObject);
                    psObject.TypeNames.Clear();
                    psObject.TypeNames.Add(viewName);
                    WriteObject(psObject);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "ResolvePmDirectoryNameBulkError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
