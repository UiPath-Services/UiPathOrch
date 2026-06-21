using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Get-OrchTestSetDetail -- one GetTestSetForEdit call per matched
// TestSet, so the Packages[] and TestCases[] arrays come back populated.
// Get-OrchTestSet uses the LIST endpoint, which returns TestCaseCount
// but empty arrays; that's fine for inventory but useless for any
// downstream pipeline that needs to recreate the TestSet elsewhere.
//
// Documented clone path:
//     Get-OrchTestSetDetail SrcSet | New-OrchTestSet -Path Dst -Name DstSet
// works because New-OrchTestSet accepts Packages and TestCases via
// ValueFromPipelineByPropertyName.
[Cmdlet(VerbsCommon.Get, "OrchTestSetDetail")]
[OutputType(typeof(TestSet))]
public class GetTestSetDetailCmdlet : OrchestratorPSCmdlet
{
    // -Name is Mandatory by design — the detail path makes one extra API
    // call per matched TestSet, so accidental fan-out from a default
    // "all TestSets" would be expensive on large folders. Wildcards
    // (including "*") still work; the user just has to type the
    // selector explicitly.
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestSetNameCompleter))]
    [SupportsWildcards]
    public string[] Name { get; set; } = default!;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.TestSets.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting test set details");
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entities = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entities is null) continue;

                var (drive, folder) = result.Source;
                var targetEntities = entities
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name);

                foreach (var entity in targetEntities)
                {
                    if (entity?.Id is null) continue;
                    try
                    {
                        var detailed = drive.OrchAPISession.GetTestSetForEdit(folder.Id!.Value, entity.Id.Value);
                        if (detailed is null) continue;
                        detailed.Path = folder.GetPSPath();
                        WriteObject(detailed);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(entity.GetPSPath(), ex), "GetTestSetDetailError", ErrorCategory.InvalidOperation, entity));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestSetDetailError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
