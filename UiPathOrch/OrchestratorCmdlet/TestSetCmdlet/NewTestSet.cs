using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// New-OrchTestSet -- wraps POST /odata/TestSets.
//
// Surface: Name + Description + Enabled + Packages + TestCases + Path. The
// wrapped server endpoint is POST-only; there is no Set-/Update- yet.
//
// The server rejects creation with errorCode 3204 ("Test Set is empty. It
// should have at least one package and one test case.") unless both
// Packages[] and TestCases[] are populated. Supply them explicitly via
// the -Packages and -TestCases parameters (the typical fixture-seeding
// path; live-verified against PAT:\Shared 2026-05-21).
//
// Pipeline-from-Get does NOT work: Get-OrchTestSet uses the LIST endpoint
// /odata/TestSets which returns each row with TestCaseCount populated but
// Packages and TestCases arrays empty. Cloning across folders should use
// Copy-OrchTestSet, which calls the per-item GetForEdit endpoint and
// carries the full payload server-side.
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

    // Test automation packages bound to this set. Each TestSetPackage
    // references a package by PackageIdentifier + VersionMask. Pipeline-
    // bound from Copy-OrchTestSet (whose output has Packages populated).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public TestSetPackage[]? Packages { get; set; }

    // Test cases included in this set. Each TestCase references a
    // TestCaseDefinition by DefinitionId.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public TestCase[]? TestCases { get; set; }

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

                var newTestSet = new TestSet
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newTestSet.AssignStringIfNotNullOrEmpty(Description, (t, v) => t.Description = v);
                newTestSet.AssignBoolIfNotNull(Enabled, (t, v) => t.Enabled = v);
                if (Packages is not null) newTestSet.Packages = Packages;
                if (TestCases is not null) newTestSet.TestCases = TestCases;

                if (ShouldProcess(target, "New TestSet"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.CreateTestSet(folder.Id!.Value, newTestSet);
                        drive.TestSets.ClearCache(folder);
                        // Both the POST response and the LIST GET return
                        // the entity with Packages / TestCases collections
                        // empty — only the per-item GetForEdit endpoint
                        // populates them. Call it so the cmdlet output
                        // shows the actual arrays the server stored.
                        TestSet? toEmit = created;
                        if (created?.Id is not null)
                        {
                            try
                            {
                                toEmit = drive.OrchAPISession.GetTestSetForEdit(folder.Id!.Value, created.Id.Value) ?? created;
                            }
                            catch
                            {
                                // GetForEdit failed (older OC, perms gap, etc.) — fall back to the POST response.
                            }
                        }
                        if (toEmit is not null)
                        {
                            toEmit.Path = folder.GetPSPath();
                            WriteObject(toEmit);
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
