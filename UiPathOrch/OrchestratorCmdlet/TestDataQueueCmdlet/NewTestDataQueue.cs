using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// New-OrchTestDataQueue -- wraps POST /odata/TestDataQueues.
//
// Minimum-viable surface: Name + Description + ContentJsonSchema + Path.
// The wrapped server endpoint is POST-only; there is no Set-/Update- yet.
[Cmdlet(VerbsCommon.New, "OrchTestDataQueue", SupportsShouldProcess = true)]
[OutputType(typeof(TestDataQueue))]
public class NewTestDataQueueCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ContentJsonSchema { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                var newQueue = new TestDataQueue
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newQueue.AssignStringIfNotNullOrEmpty(Description, (q, v) => q.Description = v);
                newQueue.AssignStringIfNotNullOrEmpty(ContentJsonSchema, (q, v) => q.ContentJsonSchema = v);
                // Server requires ContentJsonSchema; the empty JSON-schema
                // "{}" is "any value", the right default for a queue that
                // shouldn't constrain item shape. Live verification
                // 2026-05-21: omitting the field returns 400 "The
                // ContentJsonSchema field is required.".
                newQueue.ContentJsonSchema ??= "{}";

                if (ShouldProcess(target, "New TestDataQueue"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateTestDataQueue(folder.Id!.Value, newQueue);
                        drive.TestDataQueues.ClearCache(folder);
                        if (created is not null)
                        {
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewTestDataQueueError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
