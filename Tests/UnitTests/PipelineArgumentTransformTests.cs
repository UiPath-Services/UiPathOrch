using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// The -Tags / -Events parameters are string[] but their producers (Get-Orch* objects) carry Tag[] /
// WebhookEvent[]. Without an ArgumentTransformation, ValueFromPipelineByPropertyName coerced each
// element via its (default) ToString() to the type name, which the downstream resolver/ConvertToTags
// then turned into garbage -- so `Get-Orch* | New-/Update-Orch*` silently overwrote tags / wiped a
// webhook's event subscriptions. These tests lock the transforms: objects map to the CSV string form,
// plain strings (manual or CSV input) pass through unchanged.
public class PipelineArgumentTransformTests
{
    [Fact]
    public void TagTransform_ConvertsTagArrayToNameValueStrings()
    {
        var attr = new TagArgumentTransformationAttribute();
        var tags = new[]
        {
            new Tag { Name = "env", Value = "prod", DisplayName = "env", DisplayValue = "prod" },
            new Tag { Name = "team", Value = null, DisplayName = "team", DisplayValue = null },
        };
        var result = (string?[])attr.Transform(null!, tags);
        Assert.Equal(new[] { "env=prod", "team" }, result);
    }

    [Fact]
    public void TagTransform_PassesStringsThrough()
    {
        var attr = new TagArgumentTransformationAttribute();
        var result = (string?[])attr.Transform(null!, new[] { "env=prod", "team" });
        Assert.Equal(new[] { "env=prod", "team" }, result);
    }

    [Fact]
    public void WebhookEventTransform_ConvertsEventArrayToTypeNames()
    {
        var attr = new WebhookEventArgumentTransformationAttribute();
        var events = new[]
        {
            new WebhookEvent { EventType = "task.created" },
            new WebhookEvent { EventType = "job.completed" },
        };
        var result = (string?[])attr.Transform(null!, events);
        Assert.Equal(new[] { "task.created", "job.completed" }, result);
    }

    [Fact]
    public void WebhookEventTransform_PassesStringsThrough()
    {
        var attr = new WebhookEventArgumentTransformationAttribute();
        var result = (string?[])attr.Transform(null!, new[] { "task.created" });
        Assert.Equal(new[] { "task.created" }, result);
    }

    [Fact]
    public void RobotUserTransform_ConvertsToRobotId_includingModernRobotsWithNullUserName()
    {
        var attr = new RobotUserArgumentTransformationAttribute();
        var rus = new[]
        {
            new RobotUser { UserName = "domain\\alice", RobotId = 12345 },
            new RobotUser { UserName = null, RobotId = 67890 }, // modern robot: UserName null, Id present
        };
        var result = (string?[])attr.Transform(null!, rus);
        Assert.Equal(new[] { "12345", "67890" }, result);
    }

    [Fact]
    public void RobotUserTransform_PassesStringsThrough()
    {
        var attr = new RobotUserArgumentTransformationAttribute();
        var result = (string?[])attr.Transform(null!, new[] { "John Doe", "Jane Smith" });
        Assert.Equal(new[] { "John Doe", "Jane Smith" }, result);
    }

    [Fact]
    public void RobotExecutorTransform_ConvertsToName()
    {
        var attr = new RobotExecutorArgumentTransformationAttribute();
        var res = new[]
        {
            new RobotExecutor { Name = "robotA", Id = 1 },
            new RobotExecutor { Name = "robotB", Id = 2 },
        };
        var result = (string?[])attr.Transform(null!, res);
        Assert.Equal(new[] { "robotA", "robotB" }, result);
    }

    [Fact]
    public void RobotExecutorTransform_PassesStringsThrough()
    {
        var attr = new RobotExecutorArgumentTransformationAttribute();
        var result = (string?[])attr.Transform(null!, new[] { "robotA" });
        Assert.Equal(new[] { "robotA" }, result);
    }

    // ---------------- shared unwrapping / shaping contract ----------------
    //
    // The cases above only exercise "typed array in, strings out" and "strings pass through".
    // The transforms do three more things that the tests never pinned, each of which silently
    // reintroduces the original data-destroying bug if it regresses -- and all three are shared
    // plumbing, so a regression would hit every one of these parameters at once:
    //
    //   * PSObject unwrapping on the INPUT and, separately, on each ELEMENT. Pipeline binding
    //     hands elements over wrapped; if the element is not unwrapped, `is Tag` fails and the
    //     element falls back to ToString() -- i.e. the type name, which is exactly what these
    //     attributes exist to prevent.
    //   * a bare string must count as ONE item, never as its IEnumerable<char>.
    //   * a bare (non-array) entity must still be projected.
    //
    // Tag stands in for all four where the behaviour is type-independent; the element-unwrap
    // case is repeated per attribute because that is the one that destroyed real data.

    [Fact]
    public void Transform_UnwrapsAPSObjectAroundTheWholeInput()
    {
        var attr = new TagArgumentTransformationAttribute();
        var tags = new[] { new Tag { Name = "env", Value = "prod", DisplayName = "env", DisplayValue = "prod" } };

        var result = (string?[])attr.Transform(null!, PSObject.AsPSObject(tags));

        Assert.Equal(new[] { "env=prod" }, result);
    }

    [Theory]
    [MemberData(nameof(ElementUnwrapCases))]
    public void Transform_UnwrapsAPSObjectAroundEachElement(
        ArgumentTransformationAttribute attr, object entity, string expected)
    {
        // The shape ValueFromPipelineByPropertyName actually delivers.
        var wrapped = new object[] { PSObject.AsPSObject(entity) };

        var result = (string?[])attr.Transform(null!, wrapped);

        Assert.Equal(new[] { expected }, result);
    }

    public static TheoryData<ArgumentTransformationAttribute, object, string> ElementUnwrapCases() => new()
    {
        { new TagArgumentTransformationAttribute(), new Tag { Name = "env", Value = "prod", DisplayName = "env", DisplayValue = "prod" }, "env=prod" },
        { new WebhookEventArgumentTransformationAttribute(), new WebhookEvent { EventType = "task.created" }, "task.created" },
        { new RobotUserArgumentTransformationAttribute(), new RobotUser { RobotId = 12345 }, "12345" },
        { new RobotExecutorArgumentTransformationAttribute(), new RobotExecutor { Name = "robotA" }, "robotA" },
    };

    // A string is IEnumerable<char>. Without the explicit `is not string` guard, "env=prod"
    // would be exploded into one item per character.
    [Fact]
    public void Transform_TreatsABareStringAsASingleItem()
    {
        var attr = new TagArgumentTransformationAttribute();

        var result = (string?[])attr.Transform(null!, "env=prod");

        Assert.Equal(new[] { "env=prod" }, result);
    }

    [Fact]
    public void Transform_ProjectsABareNonArrayEntity()
    {
        var attr = new TagArgumentTransformationAttribute();

        var result = (string?[])attr.Transform(null!, new Tag { Name = "env", Value = "prod", DisplayName = "env", DisplayValue = "prod" });

        Assert.Equal(new[] { "env=prod" }, result);
    }

    [Fact]
    public void Transform_PassesNullThroughInsteadOfThrowing()
    {
        var attr = new TagArgumentTransformationAttribute();

        Assert.Null(attr.Transform(null!, null!));
    }

    // An element of an unrelated type has no projection, so it falls back to ToString(). Pinned
    // so the fallback stays a deliberate branch rather than becoming an exception.
    [Fact]
    public void Transform_FallsBackToToStringForUnrelatedTypes()
    {
        var attr = new TagArgumentTransformationAttribute();

        var result = (string?[])attr.Transform(null!, new object[] { 42 });

        Assert.Equal(new[] { "42" }, result);
    }
}
