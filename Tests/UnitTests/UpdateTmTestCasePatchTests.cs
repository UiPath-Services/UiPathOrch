using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Update-TmTestCase sends a JSON Patch with one "replace" per field that was given and differs
// from the current value. A request that changes nothing must send no operation.
public class UpdateTmTestCasePatchTests
{
    private static TmTestCase Current() => new()
    {
        id = "t1",
        name = "Login",
        version = "1.0.0",
        description = "desc",
        preCondition = "user exists",
        postCondition = null,
    };

    [Fact]
    public void NothingGiven_NoOperation()
        => Assert.Empty(UpdateTmTestCaseCmdlet.ComputeTestCasePatch(Current(), null, null, null, null, null));

    [Fact]
    public void SameValues_NoOperation()
        => Assert.Empty(UpdateTmTestCaseCmdlet.ComputeTestCasePatch(Current(), "Login", "1.0.0", "desc", "user exists", ""));

    [Theory]
    [InlineData("/name")]
    [InlineData("/version")]
    [InlineData("/description")]
    [InlineData("/preCondition")]
    [InlineData("/postCondition")]
    public void OneFieldChanged_OneReplace(string path)
    {
        string? V(string p) => p == path ? "changed" : null;
        var ops = UpdateTmTestCaseCmdlet.ComputeTestCasePatch(Current(), V("/name"), V("/version"), V("/description"), V("/preCondition"), V("/postCondition"));
        var op = Assert.Single(ops);
        Assert.Equal(("replace", path, "changed"), (op.op, op.path, op.value as string));
    }

    [Fact]
    public void AllChanged_FiveOperations_InFieldOrder()
    {
        var ops = UpdateTmTestCaseCmdlet.ComputeTestCasePatch(Current(), "a", "b", "c", "d", "e");
        Assert.Equal(["/name", "/version", "/description", "/preCondition", "/postCondition"], ops.Select(o => o.path));
    }
}
