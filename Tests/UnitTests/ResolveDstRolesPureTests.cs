using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for ResolveDstRolesPure -- the multi-stage role resolver
// extracted from CopyItem.cs's FindDstRoles. Pins down the matching policy
// for assignment of folder-scope role lists during Copy-Item.
//
// Policy verified here:
//   1. Inherited roles are silently skipped.
//   2. Same-drive copy: match by Id (safer for renamed roles).
//   3. Cross-drive copy: match by Name, case-insensitive.
//   4. A matched dst role whose Type is "Tenant" is excluded from the
//      returned list. Reason: classic-folder user payloads include
//      tenant-scope roles in their role list, but only folder-scope
//      roles can legally be assigned via folder user APIs. The exclusion
//      is intentional -- not a bug.
public class ResolveDstRolesPureTests
{
    private static SimpleRole SR(long id, string name, string? origin = null) =>
        new() { Id = id, Name = name, Origin = origin };

    private static Role R(long id, string name, string type = "Folder") =>
        new() { Id = id, Name = name, Type = type };

    [Fact]
    public void SameDriveMatchByIdReturnsResolved()
    {
        var src = new[] { SR(10, "ignored-name") };
        var dst = new[] { R(10, "actual-name") };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: true);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.Resolved, entries[0].Result);
        Assert.Same(dst[0], entries[0].DstRole);
    }

    [Fact]
    public void CrossDriveMatchByName_CaseInsensitive()
    {
        var src = new[] { SR(10, "Auditor") };
        var dst = new[] { R(99, "AUDITOR") };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: false);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.Resolved, entries[0].Result);
        Assert.Equal(99, entries[0].DstRole!.Id);
    }

    [Fact]
    public void CrossDriveDoesNotMatchById()
    {
        // Same Id but different Name -- cross-drive only matches by Name, so this misses.
        var src = new[] { SR(10, "Auditor") };
        var dst = new[] { R(10, "DifferentName") };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: false);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.NotFoundInDstTenant, entries[0].Result);
    }

    [Fact]
    public void InheritedRolesAreSkippedSilently()
    {
        var src = new[] { SR(10, "Auditor", origin: "Inherited") };
        var dst = new[] { R(10, "Auditor") };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: true);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.SkippedAsInherited, entries[0].Result);
        // No matching attempt was made.
        Assert.Null(entries[0].DstRole);
    }

    [Fact]
    public void TenantTypeMatchedButExcluded()
    {
        var src = new[] { SR(10, "Administrator") };
        var dst = new[] { R(10, "Administrator", type: "Tenant") };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: true);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.SkippedAsTenantRole, entries[0].Result);
        // The matched role is still surfaced so the caller can introspect why.
        Assert.NotNull(entries[0].DstRole);
        Assert.Equal("Tenant", entries[0].DstRole!.Type);
    }

    [Fact]
    public void NotFoundWhenNoCandidateMatchesIdOnSameDrive()
    {
        var src = new[] { SR(10, "Auditor") };
        var dst = new[] { R(11, "Auditor") };  // Id mismatch
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: true);

        Assert.Single(entries);
        Assert.Equal(FindDstRoleResult.NotFoundInDstTenant, entries[0].Result);
    }

    [Fact]
    public void MixedInputProducesMixedResults()
    {
        var src = new[]
        {
            SR(10, "Auditor"),                             // Resolved
            SR(20, "Administrator"),                       // SkippedAsTenantRole
            SR(30, "BuiltIn", origin: "Inherited"),        // SkippedAsInherited
            SR(40, "Phantom"),                             // NotFoundInDstTenant
        };
        var dst = new[]
        {
            R(10, "Auditor", type: "Folder"),
            R(20, "Administrator", type: "Tenant"),
        };
        var entries = ResolveDstRolesPure(src, dst, isSameDrive: true);

        Assert.Equal(4, entries.Count);
        Assert.Equal(FindDstRoleResult.Resolved, entries[0].Result);
        Assert.Equal(FindDstRoleResult.SkippedAsTenantRole, entries[1].Result);
        Assert.Equal(FindDstRoleResult.SkippedAsInherited, entries[2].Result);
        Assert.Equal(FindDstRoleResult.NotFoundInDstTenant, entries[3].Result);
    }

    [Fact]
    public void EmptySrcRolesReturnsEmptyList()
    {
        var entries = ResolveDstRolesPure(Array.Empty<SimpleRole>(), Array.Empty<Role>(), isSameDrive: true);
        Assert.Empty(entries);
    }
}
