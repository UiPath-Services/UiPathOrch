using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// The ability to add members is excluded from this cmdlet.
// ShouldProcess cannot be properly supported unless this cmdlet only creates empty groups.

[Cmdlet(VerbsCommon.New, "PmGroup", SupportsShouldProcess = true)]
public class AddPmGroupCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewPmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private class NewPmGroupNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);
            var results = ParallelResults.GroupBy(drives, drive => drive.PmGroups.Get());

            // Exclude Names already selected via parameters from candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewGroup", names, entities, e => e.name!));
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            var partitionGlobalId = drive.GetPartitionGlobalId();
            foreach (var groupName in GroupName!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(drive.NameColonSeparator, groupName);
                if (ShouldProcess(target, "New PmGroup"))
                {
                    try
                    {
                        var newGroup = drive.CreatePmGroup(WildcardPattern.Unescape(groupName));
                        if (newGroup is not null)
                        {
                            { var c = newGroup.ShallowClone(); c.Path = drive.NameColonSeparator; WriteObject(c); }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewPmGroupError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
