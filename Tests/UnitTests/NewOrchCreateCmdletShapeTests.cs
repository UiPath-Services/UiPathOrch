using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the surface of the five "had Copy/Get, was missing New" cmdlets
// shipped 2026-05-21:
//   New-OrchTestSet / New-OrchTestSetSchedule / New-OrchTestDataQueue /
//   New-OrchActionCatalog
// (plus New-OrchApiTrigger which has its own dedicated test class).
//
// Same playbook as AssetLinkCmdletShapeTests:
//   - Cmdlet attribute (verb/noun spelling, SupportsShouldProcess).
//   - OutputType points at the right entity.
//   - Mandatory `Name` parameter accepts pipeline binding so CSV import works.
//   - Manifest export presence — without this, Install-Module wouldn't surface
//     the cmdlet even though the DLL has it (the bug class that prompted
//     RemoveAssetLink_IsListedInModuleManifest in the asset-link tests).
public class NewOrchCreateCmdletShapeTests
{
    [Theory]
    [InlineData(typeof(NewTestSetCmdlet), "OrchTestSet", typeof(TestSet))]
    [InlineData(typeof(NewTestSetScheduleCmdlet), "OrchTestSetSchedule", typeof(TestSetSchedule))]
    [InlineData(typeof(NewTestDataQueueCmdlet), "OrchTestDataQueue", typeof(TestDataQueue))]
    [InlineData(typeof(NewActionCatalogCmdlet), "OrchActionCatalog", typeof(TaskCatalog))]
    public void Cmdlet_DeclaresExpectedAttributes(System.Type cmdletType, string expectedNoun, System.Type expectedOutput)
    {
        var cmdletAttr = cmdletType.GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("New", cmdletAttr.VerbName);
        Assert.Equal(expectedNoun, cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess,
            $"{cmdletType.Name} creates server-side state and must support -WhatIf / -Confirm.");

        var outputAttr = cmdletType.GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == expectedOutput);
    }

    [Theory]
    [InlineData(typeof(NewTestSetCmdlet))]
    [InlineData(typeof(NewTestSetScheduleCmdlet))]
    [InlineData(typeof(NewTestDataQueueCmdlet))]
    [InlineData(typeof(NewActionCatalogCmdlet))]
    public void Cmdlet_HasMandatoryNameAcceptingPipeline(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("Name");
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"{cmdletType.Name}.Name must be Mandatory.");
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.Name must accept ValueFromPipelineByPropertyName so CSV import works.");
    }

    [Fact]
    public void NewTestSetSchedule_HasMandatoryTestSetName()
    {
        // CSV-driven create requires the TestSet linkage. The cmdlet resolves
        // TestSetName -> TestSetId server-side, so this column is the only
        // way to bind a schedule to its set.
        var prop = typeof(NewTestSetScheduleCmdlet).GetProperty("TestSetName");
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.Mandatory),
            "NewTestSetScheduleCmdlet.TestSetName must be Mandatory — schedule has no usable identity without it.");
    }

    [Theory]
    [InlineData("'New-OrchTestSet'")]
    [InlineData("'New-OrchTestSetSchedule'")]
    [InlineData("'New-OrchTestDataQueue'")]
    [InlineData("'New-OrchActionCatalog'")]
    public void Cmdlet_IsListedInModuleManifest(string expectedEntry)
    {
        // Without this, Install-Module wouldn't surface the cmdlet — the same
        // class of bug as the unshipped Remove-OrchAssetLink that prompted
        // the original manifest-presence guard.
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains(expectedEntry, data);
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
