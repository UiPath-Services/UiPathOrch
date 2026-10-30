using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Update-TmRequirement sends a JSON Patch with one "replace" per field that was given and
// differs from the current value. A request that changes nothing must send no operation.
public class UpdateTmRequirementPatchTests
{
    private static TmRequirement Current() => new() { id = "r1", name = "Login", description = "desc" };

    [Fact]
    public void NothingGiven_NoOperation()
        => Assert.Empty(UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), null, null));

    [Fact]
    public void SameValues_NoOperation()
        => Assert.Empty(UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), "Login", "desc"));

    [Fact]
    public void NewName_ReplacesNameOnly()
    {
        var ops = UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), "Sign-in", null);
        var op = Assert.Single(ops);
        Assert.Equal(("replace", "/name", "Sign-in"), (op.op, op.path, op.value as string));
    }

    [Fact]
    public void Description_ReplacesDescriptionOnly()
    {
        var ops = UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), null, "new desc");
        var op = Assert.Single(ops);
        Assert.Equal(("replace", "/description", "new desc"), (op.op, op.path, op.value as string));
    }

    [Fact]
    public void EmptyDescription_ClearsIt()
    {
        var op = Assert.Single(UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), null, ""));
        Assert.Equal("", op.value);
    }

    [Fact]
    public void EmptyDescription_OnNullCurrent_NoOperation()
    {
        var current = Current();
        current.description = null;
        Assert.Empty(UpdateTmRequirementCmdlet.ComputeRequirementPatch(current, null, ""));
    }

    [Fact]
    public void BothChanged_TwoOperations_NameFirst()
    {
        var ops = UpdateTmRequirementCmdlet.ComputeRequirementPatch(Current(), "Sign-in", "new desc");
        Assert.Equal(["/name", "/description"], ops.Select(o => o.path));
    }
}
