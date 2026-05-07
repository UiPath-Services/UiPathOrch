using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Mirror of AssetLinkCmdletShapeTests / BucketLinkCmdletShapeTests for the
// queue-link trio. Locks Mandatory params, ShouldProcess on mutating cmdlets,
// pipeline-by-name binding, and psd1 export entries so future refactors
// can't quietly regress streaming, safety, or pipe-chaining.
public class QueueLinkCmdletShapeTests
{
    [Fact]
    public void AddQueueLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(AddQueueLinkCommand).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Add", cmdletAttr.VerbName);
        Assert.Equal("OrchQueueLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Add-OrchQueueLink modifies tenant state and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void RemoveQueueLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(RemoveQueueLinkCommand).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Remove", cmdletAttr.VerbName);
        Assert.Equal("OrchQueueLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Remove-OrchQueueLink is destructive and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void GetQueueLink_DoesNotDeclareShouldProcess()
    {
        var cmdletAttr = typeof(GetQueueLinkCommand).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.False(cmdletAttr.SupportsShouldProcess);
    }

    [Theory]
    [InlineData(typeof(AddQueueLinkCommand), "Name")]
    [InlineData(typeof(AddQueueLinkCommand), "Link")]
    [InlineData(typeof(RemoveQueueLinkCommand), "Name")]
    [InlineData(typeof(RemoveQueueLinkCommand), "Link")]
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
    [InlineData(typeof(GetQueueLinkCommand), "Name")]
    [InlineData(typeof(GetQueueLinkCommand), "Path")]
    [InlineData(typeof(AddQueueLinkCommand), "Name")]
    [InlineData(typeof(AddQueueLinkCommand), "Link")]
    [InlineData(typeof(AddQueueLinkCommand), "Path")]
    [InlineData(typeof(RemoveQueueLinkCommand), "Name")]
    [InlineData(typeof(RemoveQueueLinkCommand), "Link")]
    [InlineData(typeof(RemoveQueueLinkCommand), "Path")]
    public void PipelineProperties_AcceptValueFromPipelineByPropertyName(System.Type cmdletType, string paramName)
    {
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.{paramName} must accept ValueFromPipelineByPropertyName so the QueueLink output can pipe through.");
    }

    [Fact]
    public void QueueLink_HasPipelineProperties()
    {
        Assert.NotNull(typeof(QueueLink).GetProperty("Path"));
        Assert.NotNull(typeof(QueueLink).GetProperty("Name"));
        Assert.NotNull(typeof(QueueLink).GetProperty("Link"));
        Assert.NotNull(typeof(QueueLink).GetProperty("QueueId"));
        Assert.NotNull(typeof(QueueLink).GetProperty("FolderId"));
        Assert.NotNull(typeof(QueueLink).GetProperty("LinkFolderId"));
    }

    [Fact]
    public void GetQueueLink_DeclaresOutputType()
    {
        var outputAttr = typeof(GetQueueLinkCommand).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(QueueLink));
    }

    [Fact]
    public void QueueLinkCmdlets_AreListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'Get-OrchQueueLink'", data);
        Assert.Contains("'Add-OrchQueueLink'", data);
        Assert.Contains("'Remove-OrchQueueLink'", data);
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
