using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Mirror of AssetLinkCmdletShapeTests for the bucket-link trio. Keeps the
// public surface — Mandatory parameters, ShouldProcess on mutating cmdlets,
// pipeline-by-name binding, exported in psd1 — locked in so a future
// refactor cannot quietly regress streaming, safety, or pipe-chaining.
public class BucketLinkCmdletShapeTests
{
    [Fact]
    public void AddBucketLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(AddBucketLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Add", cmdletAttr.VerbName);
        Assert.Equal("OrchBucketLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Add-OrchBucketLink modifies tenant state and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void RemoveBucketLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(RemoveBucketLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Remove", cmdletAttr.VerbName);
        Assert.Equal("OrchBucketLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Remove-OrchBucketLink is destructive and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void GetBucketLink_DoesNotDeclareShouldProcess()
    {
        var cmdletAttr = typeof(GetBucketLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.False(cmdletAttr.SupportsShouldProcess);
    }

    [Theory]
    [InlineData(typeof(AddBucketLinkCmdlet), "Name")]
    [InlineData(typeof(AddBucketLinkCmdlet), "Link")]
    [InlineData(typeof(RemoveBucketLinkCmdlet), "Name")]
    [InlineData(typeof(RemoveBucketLinkCmdlet), "Link")]
    public void MutatingCmdlet_HasMandatoryNameAndLink(System.Type cmdletType, string paramName)
    {
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"{cmdletType.Name}.{paramName} must be Mandatory in at least one parameter set.");
    }

    [Theory]
    [InlineData(typeof(GetBucketLinkCmdlet), "Name")]
    [InlineData(typeof(GetBucketLinkCmdlet), "Path")]
    [InlineData(typeof(AddBucketLinkCmdlet), "Name")]
    [InlineData(typeof(AddBucketLinkCmdlet), "Link")]
    [InlineData(typeof(AddBucketLinkCmdlet), "Path")]
    [InlineData(typeof(RemoveBucketLinkCmdlet), "Name")]
    [InlineData(typeof(RemoveBucketLinkCmdlet), "Link")]
    [InlineData(typeof(RemoveBucketLinkCmdlet), "Path")]
    public void PipelineProperties_AcceptValueFromPipelineByPropertyName(System.Type cmdletType, string paramName)
    {
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.{paramName} must accept ValueFromPipelineByPropertyName so the BucketLink output can pipe through.");
    }

    [Fact]
    public void BucketLink_HasPipelineProperties()
    {
        Assert.NotNull(typeof(EntityLink).GetProperty("Path"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Name"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Link"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Id"));
        Assert.NotNull(typeof(EntityLink).GetProperty("FolderId"));
        Assert.NotNull(typeof(EntityLink).GetProperty("LinkFolderId"));
    }

    [Fact]
    public void GetBucketLink_DeclaresOutputType()
    {
        var outputAttr = typeof(GetBucketLinkCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(EntityLink));
    }

    [Fact]
    public void BucketLinkCmdlets_AreListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'Get-OrchBucketLink'", data);
        Assert.Contains("'Add-OrchBucketLink'", data);
        Assert.Contains("'Remove-OrchBucketLink'", data);
    }

    private static string LocateModuleManifest()
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "Staging", "UiPathOrch.psd1");
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException(
            "Staging/UiPathOrch.psd1 not found above " + System.AppContext.BaseDirectory);
    }
}
