using System.Reflection;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Regression test for the pre-1.4.3 ClearAllCache bug.
//
// The legacy OrchDuDriveInfo.ClearAllCache was a hand-maintained list of
// `_dicXxx = null` assignments. `_dicDuExtractors` was missing from the list,
// so `Clear-OrchCache` silently left stale extractor data behind. The
// migration to registry-driven ClearAllCache (iterating `_allTenantCache`)
// makes that class of bug structurally impossible — every cache class
// registers itself in its constructor via `_drive._allTenantCache.Add(this)`
// and is cleared uniformly by the registry loop.
//
// This test locks in the *structural* property the registry pattern depends
// on: every DU entity has a corresponding cache field on OrchDuDriveInfo, of
// the right concrete cache type. A future refactor that drops or renames a
// cache field will fail this test rather than silently regress to the bug
// shape (entity exists, cache class exists, but it's not wired into the
// drive's registry → Clear-OrchCache silently misses it).
public class OrchDuDriveInfoCacheRegistrationTests
{
    [Theory]
    [InlineData("DuRoles", typeof(DuListCachePerOrganization<DuRole>))]
    [InlineData("DuProjects", typeof(DuListCachePerTenant<DuProject>))]
    [InlineData("DuUsers", typeof(DuKeyedListCachePerOrganization<(string TenantKey, string ProjectId), DuUser>))]
    [InlineData("DuDocumentTypes", typeof(DuListCachePerProject<DuDocumentType>))]
    [InlineData("DuClassifiers", typeof(DuListCachePerProject<DuClassifier>))]
    // DuExtractors was the missing entry in the pre-1.4.3 manual clear loop —
    // keep it explicitly named here.
    [InlineData("DuExtractors", typeof(DuListCachePerProject<DuExtractor>))]
    public void EveryDuEntityHasACacheFieldOfTheRightType(string fieldName, Type expectedType)
    {
        var field = typeof(OrchDuDriveInfo).GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(field);
        Assert.Equal(expectedType, field!.FieldType);
        // The cache type must implement ITenantCacheClearable so the
        // registry-driven ClearAllCache will iterate and clear it.
        Assert.True(typeof(ITenantCacheClearable).IsAssignableFrom(field.FieldType),
            $"Field '{fieldName}' must be ITenantCacheClearable so ClearAllCache covers it.");
    }

    [Fact]
    public void OrchDuDriveInfo_HasTenantAndFolderCacheRegistries()
    {
        // ClearAllCache iterates these — if either disappears, the migration
        // is incomplete.
        var tenantReg = typeof(OrchDuDriveInfo).GetField(
            "_allTenantCache",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var folderReg = typeof(OrchDuDriveInfo).GetField(
            "_allFolderCache",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(tenantReg);
        Assert.NotNull(folderReg);
        Assert.Equal(typeof(List<ITenantCacheClearable>), tenantReg!.FieldType);
        Assert.Equal(typeof(List<IFolderCacheClearable>), folderReg!.FieldType);
    }
}
