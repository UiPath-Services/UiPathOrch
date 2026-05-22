using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the public surface of the asset-link cmdlet trio in place. The
// regression that prompted these tests was Remove-OrchAssetLink shipping
// without an actual unshare API call AND without SupportsShouldProcess —
// it didn't surface for a release because the cmdlet wasn't yet listed in
// the psd1. The tests below codify the invariants that, taken together,
// make `Get | Remove` and `Get | Add` pipelines work and ensure each
// cmdlet declares the right safety semantics.
public class AssetLinkCmdletShapeTests
{
    [Fact]
    public void AddAssetLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(AddAssetLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Add", cmdletAttr.VerbName);
        Assert.Equal("OrchAssetLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Add-OrchAssetLink modifies tenant state and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void RemoveAssetLink_DeclaresShouldProcess()
    {
        var cmdletAttr = typeof(RemoveAssetLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Remove", cmdletAttr.VerbName);
        Assert.Equal("OrchAssetLink", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            "Remove-OrchAssetLink is destructive and must support -WhatIf / -Confirm.");
    }

    [Fact]
    public void GetAssetLink_DoesNotDeclareShouldProcess()
    {
        // Get is read-only; declaring SupportsShouldProcess would imply otherwise.
        var cmdletAttr = typeof(GetAssetLinkCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.False(cmdletAttr.SupportsShouldProcess);
    }

    [Theory]
    [InlineData(typeof(AddAssetLinkCmdlet), "Name")]
    [InlineData(typeof(AddAssetLinkCmdlet), "Link")]
    [InlineData(typeof(RemoveAssetLinkCmdlet), "Name")]
    [InlineData(typeof(RemoveAssetLinkCmdlet), "Link")]
    public void MutatingCmdlet_HasMandatoryNameAndLink(System.Type cmdletType, string paramName)
    {
        // Without Mandatory on both, Remove-OrchAssetLink without -Link would not
        // error and might be misread as "remove all links" — that's unsafe.
        // Add likewise: -Link is the whole point of the cmdlet, so it must be required.
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"{cmdletType.Name}.{paramName} must be Mandatory in at least one parameter set.");
    }

    [Theory]
    [InlineData(typeof(GetAssetLinkCmdlet), "Name")]
    [InlineData(typeof(GetAssetLinkCmdlet), "Path")]
    [InlineData(typeof(AddAssetLinkCmdlet), "Name")]
    [InlineData(typeof(AddAssetLinkCmdlet), "Link")]
    [InlineData(typeof(AddAssetLinkCmdlet), "Path")]
    [InlineData(typeof(RemoveAssetLinkCmdlet), "Name")]
    [InlineData(typeof(RemoveAssetLinkCmdlet), "Link")]
    [InlineData(typeof(RemoveAssetLinkCmdlet), "Path")]
    public void PipelineProperties_AcceptValueFromPipelineByPropertyName(System.Type cmdletType, string paramName)
    {
        // The trio is designed so `Get-OrchAssetLink | Remove-OrchAssetLink` and
        // `Get-OrchAssetLink -Path Source | Add-OrchAssetLink -Path Dest` work
        // with no -PipelineVariable plumbing. That requires Path / Name / Link
        // to bind from pipeline properties on every cmdlet they appear on.
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.{paramName} must accept ValueFromPipelineByPropertyName so the AssetLink output can pipe through.");
    }

    [Fact]
    public void AssetLink_HasPipelineProperties()
    {
        // The AssetLink output type carries Path / Name / Link to feed the
        // ValueFromPipelineByPropertyName bindings on Add and Remove. If any
        // of these properties get renamed or dropped, the pipeline silently
        // breaks at runtime — these reflection asserts surface that at CI time.
        Assert.NotNull(typeof(EntityLink).GetProperty("Path"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Name"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Link"));
        Assert.NotNull(typeof(EntityLink).GetProperty("Id"));
        Assert.NotNull(typeof(EntityLink).GetProperty("FolderId"));
        Assert.NotNull(typeof(EntityLink).GetProperty("LinkFolderId"));
    }

    [Fact]
    public void AssetLinkCmdlets_AllDeclareOutputType_OnGet()
    {
        // Get-OrchAssetLink advertises AssetLink as its output for Get-Help
        // -OutputType integration and for tooling that introspects pipelines.
        var outputAttr = typeof(GetAssetLinkCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(EntityLink));
    }

    [Fact]
    public void RemoveAssetLink_IsListedInModuleManifest()
    {
        // The whole reason the missing unshare implementation went undetected was
        // that Remove-OrchAssetLink wasn't in psd1 CmdletsToExport — Install-Module
        // didn't surface it, so nobody hit the bug. This test ensures the cmdlet
        // ships now that it actually works.
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'Remove-OrchAssetLink'", data);
        Assert.Contains("'Add-OrchAssetLink'", data);
        Assert.Contains("'Get-OrchAssetLink'", data);
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
