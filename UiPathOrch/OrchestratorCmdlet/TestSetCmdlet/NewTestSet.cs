using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// New-OrchTestSet -- wraps POST /odata/TestSets.
//
// Minimum-viable surface: Name + Description + Enabled + Path. The
// wrapped server endpoint is POST-only; there is no Set-/Update- yet.
[Cmdlet(VerbsCommon.New, "OrchTestSet", SupportsShouldProcess = true)]
[OutputType(typeof(TestSet))]
public class NewTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                var newTestSet = new TestSet
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newTestSet.AssignStringIfNotNullOrEmpty(Description, (t, v) => t.Description = v);
                newTestSet.AssignBoolIfNotNull(Enabled, (t, v) => t.Enabled = v);

                if (ShouldProcess(target, "New TestSet"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateTestSet(folder.Id!.Value, newTestSet);
                        drive.TestSets.ClearCache(folder);
                        if (created is not null)
                        {
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewTestSetError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
