using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Get-OrchTestSetDetail -- the TestSet with its Packages[] and TestCases[]
// arrays populated, which the plain listing behind Get-OrchTestSet returns
// empty (it carries TestCaseCount and nothing else about them). That is fine
// for inventory and useless for any downstream pipeline that needs to
// recreate the TestSet elsewhere.
//
// One request per folder, from an expanded listing -- see
// OrchAPISession.GetTestSetsDetailed, which also records why GetForEdit, the
// endpoint this used to call once per test set, is not needed.
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

        // One expanded listing per folder answers everything, so phase 2 has nothing to
        // fetch and the pass is there only to batch the output a folder at a time -- the
        // table view sizes its columns from the first batch it receives, and row-by-row
        // emission let the first test set of a folder set them for the rest.
        FolderFanOut.Emit<TestSet, TestSet>(
            this, drivesFolders, "GetTestSetDetailError",
            listActivity: "Listing test sets",
            fetchActivity: "Getting test set details",
            list: (drive, folder) => drive.TestSetsDetailed.Get(folder)
                .FilterByWildcards(s => s?.Name, wpName)
                .OrderBy(s => s.Name),
            itemPath: testSet => testSet.GetPSPath(),
            fetch: (drive, folder, testSet) => testSet,
            emit: (drive, folder, rows) => WriteObject(rows, true));
    }
}
