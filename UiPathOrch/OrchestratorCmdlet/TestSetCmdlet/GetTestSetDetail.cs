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
// Expanding the Packages / TestCases navigation properties on the listing
// does not avoid the per-set call either -- see OrchDriveInfo.TestSetsDetailed
// for what the expanded shape leaves out.
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
        WarnTestingModuleDeprecated(drivesFolders.Select(df => df.drive));
        var wpName = Name.ConvertToWildcardPatternList();

        // GetForEdit used to run here, in the consumer loop, one test set at a time on the
        // pipeline thread, while the pool fetched only the per-folder listings. FolderFanOut
        // moves it into a pool of its own and emits a folder's rows together, so the table
        // view sizes its columns from the whole folder rather than from its first row.
        FolderFanOut.Emit<TestSet, TestSet>(
            this, drivesFolders, "GetTestSetDetailError",
            listActivity: "Listing test sets",
            fetchActivity: "Getting test set details",
            list: (drive, folder) => drive.TestSets.Get(folder)
                .FilterByWildcards(s => s?.Name, wpName)
                .OrderBy(s => s.Name),
            itemPath: testSet => testSet.GetPSPath(),
            fetch: (drive, folder, testSet) => testSet.Id is null
                ? null
                : drive.TestSetsDetailed.Get(folder, testSet.Id.Value),
            emit: (drive, folder, rows) => WriteObject(rows, true));
    }
}
