using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins FolderFanOut.EmitListed, the one-phase shape behind Get-OrchTriggerDetail and
// Get-OrchTestSetDetail. Its point is WHEN rows come out, which nothing tested before: 1.19.0
// shipped both cmdlets on the two-phase Emit with a fetch that returned its argument, so every
// folder was listed before the first row was printed, and no test noticed because none looked at
// timing. The first test was checked against that shape -- the same setup run through Emit with a
// fetch returning its argument leaves the flag false. Driven with a fake ICommandRuntime (no
// runspace); the drive is never touched, so it is passed as null.
public class FolderFanOutEmitListedTests
{
    private sealed class TestCmdlet : OrchestratorPSCmdlet
    {
    }

    private static (TestCmdlet cmdlet, RecordingCommandRuntime runtime) NewCmdlet()
    {
        var runtime = new RecordingCommandRuntime();
        return (new TestCmdlet { CommandRuntime = runtime }, runtime);
    }

    private static List<(OrchDriveInfo drive, Folder folder)> Folders(params string[] names)
        => names.Select(n => ((OrchDriveInfo)null!, new Folder { FullName = $"Test:\\{n}" })).ToList();

    private static string NameOf(Folder folder) => folder.FullName!["Test:\\".Length..];

    [Fact]
    public void Emits_a_folder_before_a_later_folders_listing_returns()
    {
        var (cmdlet, _) = NewCmdlet();
        var aEmitted = new ManualResetEventSlim(false);
        bool cWaitedForA = false;

        FolderFanOut.EmitListed<string>(cmdlet, Folders("A", "B", "C"), "TestError",
            activity: "Listing",
            list: (_, folder) =>
            {
                // C's listing does not return until A has been printed. Listing every folder
                // before emitting any -- the 1.19.0 behaviour -- leaves this waiting the full
                // timeout and the flag false.
                if (NameOf(folder) == "C") cWaitedForA = aEmitted.Wait(TimeSpan.FromSeconds(10));
                return [NameOf(folder) + "1"];
            },
            emit: (_, folder, rows) =>
            {
                if (NameOf(folder) == "A") aEmitted.Set();
            });

        Assert.True(cWaitedForA, "folder A should be emitted while folder C is still listing");
    }

    [Fact]
    public void Emits_folders_in_the_order_given_even_when_later_ones_finish_first()
    {
        var (cmdlet, _) = NewCmdlet();
        var emitted = new List<string>();

        FolderFanOut.EmitListed<string>(cmdlet, Folders("A", "B", "C"), "TestError",
            activity: "Listing",
            list: (_, folder) =>
            {
                // A is the slowest, so B and C are back first; output must still start with A.
                if (NameOf(folder) == "A") Thread.Sleep(300);
                return [NameOf(folder) + "1", NameOf(folder) + "2"];
            },
            emit: (_, folder, rows) => emitted.Add($"{NameOf(folder)}:{string.Join(",", rows)}"));

        Assert.Equal(new[] { "A:A1,A2", "B:B1,B2", "C:C1,C2" }, emitted);
    }

    [Fact]
    public void Skips_empty_folders_and_reports_a_failing_one_without_stopping()
    {
        var (cmdlet, runtime) = NewCmdlet();
        var emitted = new List<string>();

        FolderFanOut.EmitListed<string>(cmdlet, Folders("A", "Empty", "Bad", "D"), "TestError",
            activity: "Listing",
            list: (_, folder) => NameOf(folder) switch
            {
                "Empty" => [],
                "Bad" => throw new OrchException(folder.FullName, "listing failed"),
                var n => [n + "1"],
            },
            emit: (_, folder, rows) => emitted.Add(NameOf(folder)));

        Assert.Equal(new[] { "A", "D" }, emitted);
        var error = Assert.Single(runtime.Errors);
        Assert.Equal("TestError", error.FullyQualifiedErrorId);
    }
}
