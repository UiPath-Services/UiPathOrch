using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the public surface of the Move-Orch{Asset,Bucket,Queue} trio.
//
// These cmdlets relocate a single shared entity from its source folder to a
// destination folder within the SAME tenant drive, via one atomic
// Share*ToFolders call (toAdd=[destination], toRemove=[source]). The move was
// verified end to end on a live tenant (asset value, queue items, and bucket
// Identifier all follow). The asserts here pin the invariants that make
//   Move-OrchXxx -Path Src -Name N -Destination Dst
//   Get-OrchXxx ... | Move-OrchXxx -Destination Dst
// safe and bindable, mirroring AssetLinkCmdletShapeTests for the link trio.
public class MoveOrchEntityCmdletShapeTests
{
    public static System.Collections.Generic.IEnumerable<object[]> MoveCmdletTypes()
    {
        yield return new object[] { typeof(MoveAssetCmdlet), "OrchAsset" };
        yield return new object[] { typeof(MoveBucketCmdlet), "OrchBucket" };
        yield return new object[] { typeof(MoveQueueCmdlet), "OrchQueue" };
    }

    [Theory]
    [MemberData(nameof(MoveCmdletTypes))]
    public void MoveCmdlet_DeclaresMoveVerbAndShouldProcess(System.Type cmdletType, string expectedNoun)
    {
        var attr = cmdletType.GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Move", attr!.VerbName);
        Assert.Equal(expectedNoun, attr.NounName);
        Assert.True(attr.SupportsShouldProcess,
            $"Move-{expectedNoun} relocates tenant state and must support -WhatIf / -Confirm.");
        // A move removes the entity from its source folder, so it should prompt
        // by default like the destructive Remove-Orch*Link cmdlets.
        Assert.Equal(ConfirmImpact.Medium, attr.ConfirmImpact);
    }

    [Theory]
    [InlineData(typeof(MoveAssetCmdlet), "Name")]
    [InlineData(typeof(MoveAssetCmdlet), "Destination")]
    [InlineData(typeof(MoveBucketCmdlet), "Name")]
    [InlineData(typeof(MoveBucketCmdlet), "Destination")]
    [InlineData(typeof(MoveQueueCmdlet), "Name")]
    [InlineData(typeof(MoveQueueCmdlet), "Destination")]
    public void MoveCmdlet_HasMandatoryNameAndDestination(System.Type cmdletType, string paramName)
    {
        // Without Mandatory on both, a missing -Destination could be misread as
        // a no-op or "move nowhere"; -Name is the entity to move. Both required.
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"{cmdletType.Name}.{paramName} must be Mandatory.");
    }

    [Theory]
    [InlineData(typeof(MoveAssetCmdlet), "Name")]
    [InlineData(typeof(MoveAssetCmdlet), "Destination")]
    [InlineData(typeof(MoveAssetCmdlet), "Path")]
    [InlineData(typeof(MoveBucketCmdlet), "Name")]
    [InlineData(typeof(MoveBucketCmdlet), "Destination")]
    [InlineData(typeof(MoveBucketCmdlet), "Path")]
    [InlineData(typeof(MoveQueueCmdlet), "Name")]
    [InlineData(typeof(MoveQueueCmdlet), "Destination")]
    [InlineData(typeof(MoveQueueCmdlet), "Path")]
    public void MoveCmdlet_PipelineProperties_BindByPropertyName(System.Type cmdletType, string paramName)
    {
        // Enables `Get-OrchAsset -Path Src | Move-OrchAsset -Destination Dst`:
        // Name/Path come from the piped entity, Destination from the command line.
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.{paramName} must accept ValueFromPipelineByPropertyName.");
    }

    [Theory]
    [InlineData(typeof(MoveAssetCmdlet), "Name")]
    [InlineData(typeof(MoveAssetCmdlet), "Destination")]
    [InlineData(typeof(MoveBucketCmdlet), "Name")]
    [InlineData(typeof(MoveBucketCmdlet), "Destination")]
    [InlineData(typeof(MoveQueueCmdlet), "Name")]
    [InlineData(typeof(MoveQueueCmdlet), "Destination")]
    public void MoveCmdlet_NameAndDestination_SupportWildcards(System.Type cmdletType, string paramName)
    {
        // -Name selects entities (wildcards like the link cmdlets); -Destination
        // is wildcard-resolved to a single folder (the base flags >1 as ambiguous).
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        Assert.NotNull(prop!.GetCustomAttribute<SupportsWildcardsAttribute>());
    }

    [Theory]
    [MemberData(nameof(MoveCmdletTypes))]
    public void MoveCmdlet_DerivesFromMoveBase(System.Type cmdletType, string _)
    {
        // Each concrete cmdlet must route through the shared base so the atomic
        // add-dst/remove-src relocation and the cross-drive / ambiguous guards
        // are inherited rather than re-implemented per entity.
        var baseType = cmdletType.BaseType;
        Assert.NotNull(baseType);
        Assert.True(baseType!.IsGenericType, $"{cmdletType.Name} should derive from MoveOrchEntityCmdletBase<T>.");
        Assert.Equal("MoveOrchEntityCmdletBase`1", baseType.GetGenericTypeDefinition().Name);
    }

    [Theory]
    [MemberData(nameof(MoveCmdletTypes))]
    public void MoveCmdlet_IsListedInModuleManifest(System.Type cmdletType, string noun)
    {
        // cmdletType is asserted indirectly: its CmdletAttribute noun must equal
        // the manifest entry, so the reflected verb-noun and the psd1 export agree.
        var attr = cmdletType.GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(noun, attr!.NounName);

        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains($"'Move-{noun}'", data);
        // The Copy- counterpart stays exported too: move and copy are distinct
        // operations (move relocates the one entity; copy makes a new one).
        Assert.Contains($"'Copy-{noun}'", data);
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
