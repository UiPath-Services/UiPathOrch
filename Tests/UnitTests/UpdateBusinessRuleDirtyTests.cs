using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Per-field change-detection for Update-OrchBusinessRule: the pure ComputeBusinessRuleUpdate
// core must only report dirty (=> call the API) when a value actually changes. A supplied
// rule-definition file always writes (its content can't be diffed).
public class UpdateBusinessRuleDirtyTests
{
    private static Tag[] T(params (string n, string? v)[] items) =>
        items.Select(i => new Tag { Name = i.n, Value = i.v }).ToArray();

    private static BusinessRule Existing() => new()
    {
        Id = "rule-1",
        Name = "MyRule",
        Description = "current",
        Tags = T(("env", "prod")),
    };

    private static bool Run(BusinessRule existing, UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs input)
    {
        var payload = new BusinessRule { Name = existing.Name };
        return UpdateBusinessRuleCmdlet.ComputeBusinessRuleUpdate(payload, existing, input);
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        Assert.False(Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs()));
    }

    [Fact]
    public void Description_Unchanged_IsNoOp()
    {
        Assert.False(Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs { Description = "current" }));
    }

    [Fact]
    public void Description_Changed_Writes()
    {
        Assert.True(Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs { Description = "different" }));
    }

    [Fact]
    public void Tags_UnchangedSet_IsNoOp()
    {
        Assert.False(Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs { Tags = new[] { "env=prod" } }));
    }

    [Fact]
    public void Tags_DifferentSet_Writes()
    {
        Assert.True(Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs { Tags = new[] { "env=dev" } }));
    }

    [Fact]
    public void SourceFile_AlwaysWrites_EvenWhenMetadataUnchanged()
    {
        // File content can't be diffed, so -Source always triggers a write even when the
        // Description / Tags exactly match the current rule.
        bool dirty = Run(Existing(), new UpdateBusinessRuleCmdlet.BusinessRuleUpdateInputs
        {
            HasSourceFile = true,
            Description = "current",           // unchanged
            Tags = new[] { "env=prod" },        // unchanged
        });
        Assert.True(dirty);
    }
}
