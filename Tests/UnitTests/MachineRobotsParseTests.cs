using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Regression guard for the malformed -MachineRobots handling.
//
// Before: a bad JSON value threw inside a broad try labelled "Failed to deserialize
// MachineRobots.", and because WriteError is non-terminating the cmdlet carried on and
// still issued the trigger PUT (with a null binding). Now the parse is isolated: a
// malformed value produces a clear, actionable error and sets parseFailed, which the
// trigger cmdlets use to skip that trigger instead of proceeding.
//
// The resolve phase needs a live drive (integration), but the parse phase runs first
// and never touches the drive/folder, so the parse contract is unit-testable directly
// with null drive/folder.
public class MachineRobotsParseTests
{
    // DeserializeMachineRobotSessions is an inherited instance method; any concrete
    // cmdlet instance can host the call, and it routes output through the passed
    // IWritableHost (not the cmdlet runtime), so a bare instance is enough.
    private static readonly UpdateTriggerCmdlet Cmd = new();

    [Fact]
    public void MalformedJson_SetsParseFailed_AndWritesActionableError()
    {
        var host = new CapturingHost();
        // Bin's exact value: the closing brace before ] is missing.
        var bad = new[] { "[{\"UserName\":\"orchtest\",\"MachineName\":\"orchestrator.local\"]" };

        var result = Cmd.DeserializeMachineRobotSessions(host, null!, null!, "trg", bad, out bool parseFailed);

        Assert.True(parseFailed);
        Assert.Null(result);

        var err = Assert.Single(host.Errors);
        Assert.Contains("MachineRobotsParseError", err.FullyQualifiedErrorId);
        Assert.Equal(ErrorCategory.InvalidArgument, err.CategoryInfo.Category);

        var msg = err.Exception.Message;
        Assert.Contains("not valid JSON", msg);   // says what's wrong
        Assert.Contains("UserName", msg);          // shows the expected shape
        Assert.Contains("orchtest", msg);          // echoes the offending value
    }

    [Fact]
    public void NullOrEmptyInput_IsNotAFailure_AndWritesNoError()
    {
        var host = new CapturingHost();

        Assert.Null(Cmd.DeserializeMachineRobotSessions(host, null!, null!, "trg", null, out bool f1));
        Assert.False(f1);

        Assert.Null(Cmd.DeserializeMachineRobotSessions(host, null!, null!, "trg", new[] { "" }, out bool f2));
        Assert.False(f2);

        Assert.Empty(host.Errors);
    }
}
