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
}
