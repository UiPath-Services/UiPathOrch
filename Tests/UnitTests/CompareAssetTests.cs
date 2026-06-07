using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for the Compare-Orch* diff engine (EntityComparison) and the Compare-OrchAsset
// pure diff surface (CompareAssetCmdlet.ComputeAssetDifferences + normalizers). The cmdlet's
// drive/folder resolution and "<= / => " enumeration need a live tenant and are covered by the
// Pester integration suite; everything below is deterministic and live-drive-free.

public class EntityComparisonValueEqualsTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData("", "", true)]
    [InlineData(null, "", true)]   // null and "" are equal for string-valued properties
    [InlineData("", null, true)]
    [InlineData("a", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("A", "a", false)]  // ordinal: case matters
    public void ValueEquals_Strings(string? a, string? b, bool expected)
    {
        Assert.Equal(expected, EntityComparison.ValueEquals(a, b));
    }

    [Fact]
    public void ValueEquals_NonStrings_UseObjectEquality()
    {
        Assert.True(EntityComparison.ValueEquals(true, true));
        Assert.False(EntityComparison.ValueEquals(true, false));
        Assert.False(EntityComparison.ValueEquals(true, null));   // bool? true vs null IS a difference
        Assert.True(EntityComparison.ValueEquals(1, 1));
        Assert.False(EntityComparison.ValueEquals(1, 2));
    }
}

public class EntityComparisonDiffPropertiesTests
{
    private static readonly (string Name, Func<Asset, object?> Get)[] Comparators =
    [
        ("Name", a => a.Name),
        ("Value", a => a.Value),
    ];

    [Fact]
    public void DiffProperties_NoDifference_ReturnsEmpty()
    {
        var a = new Asset { Name = "X", Value = "1" };
        var b = new Asset { Name = "X", Value = "1" };
        Assert.Empty(EntityComparison.DiffProperties(a, b, Comparators, null));
    }

    [Fact]
    public void DiffProperties_ReportsDifferingPropertyWithValues()
    {
        var a = new Asset { Name = "X", Value = "1" };
        var b = new Asset { Name = "X", Value = "2" };

        var diffs = EntityComparison.DiffProperties(a, b, Comparators, null);

        var d = Assert.Single(diffs);
        Assert.Equal("Value", d.Property);
        Assert.Equal("1", d.ReferenceValue);
        Assert.Equal("2", d.DifferenceValue);
    }

    [Fact]
    public void DiffProperties_OnlyFilter_RestrictsComparison()
    {
        var a = new Asset { Name = "X", Value = "1" };
        var b = new Asset { Name = "Y", Value = "2" };  // both differ

        var only = new HashSet<string>(["Value"], StringComparer.OrdinalIgnoreCase);
        var diffs = EntityComparison.DiffProperties(a, b, Comparators, only);

        Assert.Equal("Value", Assert.Single(diffs).Property);  // Name skipped
    }
}

public class CompareAssetDifferencesTests
{
    private static Asset Text(string name, string? value, string? description = null) =>
        new() { Name = name, ValueType = "Text", Value = value, Description = description };

    private static List<PropertyDifference> Diff(Asset a, Asset b,
        IReadOnlyCollection<string>? only = null, bool compareUserValues = true,
        Dictionary<string, string>? userMapping = null)
        => CompareAssetCmdlet.ComputeAssetDifferences(a, b, only, compareUserValues, userMapping);

    [Fact]
    public void Identical_ReturnsEmpty()
    {
        Assert.Empty(Diff(Text("A", "1"), Text("A", "1")));
    }

    [Fact]
    public void ValueDifference_IsReported()
    {
        Assert.Equal("Value", Assert.Single(Diff(Text("A", "1"), Text("A", "2"))).Property);
    }

    [Fact]
    public void BoolValue_CasingDifference_IsNotADifference()
    {
        var a = new Asset { Name = "B", ValueType = "Bool", Value = "True" };
        var b = new Asset { Name = "B", ValueType = "Bool", Value = "true" };
        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void Description_NullVsEmpty_IsNotADifference()
    {
        Assert.Empty(Diff(Text("A", "1", null), Text("A", "1", "")));
    }

    [Fact]
    public void Tags_AreOrderIndependent()
    {
        var a = new Asset { Name = "A", ValueType = "Text", Value = "1", Tags = [new Tag { Name = "env", Value = "prod" }, new Tag { Name = "team", Value = "fin" }] };
        var b = new Asset { Name = "A", ValueType = "Text", Value = "1", Tags = [new Tag { Name = "team", Value = "fin" }, new Tag { Name = "env", Value = "prod" }] };
        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void Tags_DifferentValue_IsReported()
    {
        var a = new Asset { Name = "A", ValueType = "Text", Value = "1", Tags = [new Tag { Name = "env", Value = "prod" }] };
        var b = new Asset { Name = "A", ValueType = "Text", Value = "1", Tags = [new Tag { Name = "env", Value = "dev" }] };
        Assert.Equal("Tags", Assert.Single(Diff(a, b)).Property);
    }

    private static Asset WithUserValue(string user, string value)
        => new()
        {
            Name = "A",
            ValueType = "Text",
            Value = "1",
            UserValues = [new AssetUserValue { UserName = user, ValueType = "Text", Value = value }],
        };

    [Fact]
    public void UserValues_DifferentValue_IsReported()
    {
        Assert.Equal("UserValues", Assert.Single(Diff(WithUserValue("alice", "1"), WithUserValue("alice", "2"))).Property);
    }

    [Fact]
    public void UserValues_NotComparedWhenDisabled()
    {
        Assert.Empty(Diff(WithUserValue("alice", "1"), WithUserValue("alice", "2"), compareUserValues: false));
    }

    [Fact]
    public void UserValues_UserMapping_TranslatesReferenceName()
    {
        // Reference user "alice" maps to difference user "bob"; same value -> no difference.
        var mapping = new Dictionary<string, string> { ["alice"] = "bob" };
        Assert.Empty(Diff(WithUserValue("alice", "1"), WithUserValue("bob", "1"), userMapping: mapping));

        // Without the mapping the differing user names are a real difference.
        Assert.Equal("UserValues", Assert.Single(Diff(WithUserValue("alice", "1"), WithUserValue("bob", "1"))).Property);
    }

    [Fact]
    public void PropertyFilter_RestrictsToNamedProperty()
    {
        // Differs in both Value and Description; -Property Value reports only Value.
        var a = Text("A", "1", "desc-a");
        var b = Text("A", "2", "desc-b");
        var only = new HashSet<string>(["Value"], StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Value", Assert.Single(Diff(a, b, only)).Property);
    }

    [Fact]
    public void UnknownOnlyProperty_ComparesNothing()
    {
        // Documents the behavior the cmdlet warns about: an -Property name that isn't a
        // comparator leaves nothing to compare, so even differing assets read as equal.
        var only = new HashSet<string>(["Id"], StringComparer.OrdinalIgnoreCase);
        Assert.Empty(Diff(Text("A", "1"), Text("A", "999"), only, compareUserValues: false));
    }
}

public class CompareAssetMetadataTests
{
    [Fact]
    public void ValidPropertyNames_AreTheCuratedSet()
    {
        var expected = new[]
        {
            "ValueType", "ValueScope", "Value", "Description", "CredentialUsername",
            "ExternalName", "AllowDirectApiAccess", "Tags", "UserValues",
        };
        Assert.Equal(expected.Length, CompareAssetCmdlet.ValidPropertyNames.Count);
        foreach (var name in expected)
            Assert.Contains(name, CompareAssetCmdlet.ValidPropertyNames);
    }

    [Fact]
    public void ValidPropertyNames_AreCaseInsensitive()
    {
        Assert.Contains("value", CompareAssetCmdlet.ValidPropertyNames);
        Assert.Contains("USERVALUES", CompareAssetCmdlet.ValidPropertyNames);
    }
}
