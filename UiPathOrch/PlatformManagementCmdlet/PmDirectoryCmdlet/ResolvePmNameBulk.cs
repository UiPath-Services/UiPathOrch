using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Resolve, "PmDirectoryNameBulk")]
[OutputType(typeof(DirectoryUser))]
[OutputType(typeof(DirectoryGroup))]
[OutputType(typeof(DirectoryApplication))]
public class SearchPmDirectoryBulkCmdlet : OrchestratorPSCmdlet
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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        foreach (var drive in drives)
        {
            try
            {
                var directoryObjects = drive.PmBulkResolveByName(EntityType!, Name!, name => name);
                if (directoryObjects is null) continue;

                foreach (var directoryObject in directoryObjects.Values.Where(v => v is not null).OrderBy(v => v!.name))
                {
                    // PmGroupMember (DirectoryUser/Group/Application) is
                    // org-shared (PmBulkResolveByName cache). Emit a per-emit
                    // ShallowClone with the drive-local Path set on it — NOT a
                    // PSObject NoteProperty, which keys to base-object identity
                    // and would collapse to the last drive for same-org
                    // multi-drive (see LicenseInventory / PmAuthenticationRoot).
                    // The clone keeps its concrete runtime type
                    // (DirectoryUser/Group/Application via the polymorphic
                    // PmGroupMember converter), which drives the format view
                    // directly — no PSObject/TypeNames wrapper needed.
                    var copy = directoryObject!.ShallowClone();
                    copy.Path = drive.NameColonSeparator;
                    WriteObject(copy);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "ResolvePmDirectoryNameBulkError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
