using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Baseline tests for the Assign* extension helpers in OrchExtensions.cs.
// Pre-refactor: 22 method signatures, 231 callsites across 20 cmdlet files,
// zero unit-test coverage. These tests pin down the existing semantics of
// every overload — null vs empty handling, change detection, type-coercion
// quirks (e.g. AssignBoolIfNotNull(string?) parsing, AssignNumberIfNotNull
// from string with "" → null behaviour) — so the planned consolidation can
// be verified to preserve behaviour exactly.
//
// Each Assign* family gets its own test class for readability and so test
// runs report which family regressed if anything breaks.

public sealed class TestEntity
{
    public string? Str { get; set; }
    public int? Num { get; set; }
    public long? Big { get; set; }
    public bool? Flag { get; set; }
    public DateTime? When { get; set; }
    public Tag[]? Tags { get; set; }
    public long? Id { get; set; }
}

public class AssignStringIfNotNullTests
{
    [Fact]
    public void NullValue_DoesNotAssign()
    {
        var t = new TestEntity { Str = "original" };
        t.AssignStringIfNotNull(null, (e, v) => e.Str = v);
        Assert.Equal("original", t.Str);
    }

    [Fact]
    public void EmptyValue_AssignsEmpty()
    {
        // The bare AssignStringIfNotNull treats empty string as a valid value,
        // i.e. it WILL clear the existing value. Differs from AssignStringIfNotNullOrEmpty.
        var t = new TestEntity { Str = "original" };
        t.AssignStringIfNotNull("", (e, v) => e.Str = v);
        Assert.Equal("", t.Str);
    }

    [Fact]
    public void NonEmptyValue_Assigns()
    {
        var t = new TestEntity();
        t.AssignStringIfNotNull("new", (e, v) => e.Str = v);
        Assert.Equal("new", t.Str);
    }

    // --- Overload with target getter (change-detection / PUT semantics) ---

    [Fact]
    public void WithGetter_NullValue_ReturnsFalse()
    {
        var t = new TestEntity { Str = "original" };
        bool changed = t.AssignStringIfNotNull(null, e => e.Str, (e, v) => e.Str = v);
        Assert.False(changed);
        Assert.Equal("original", t.Str);
    }

    [Fact]
    public void WithGetter_EmptyValueAndCurrentNull_ReturnsFalse()
    {
        // Documented behaviour: null and "" are treated as equivalent, so an
        // empty value against a null current does NOT count as a change.
        var t = new TestEntity { Str = null };
        bool changed = t.AssignStringIfNotNull("", e => e.Str, (e, v) => e.Str = v);
        Assert.False(changed);
        Assert.Null(t.Str);
    }

    [Fact]
    public void WithGetter_EmptyValueAndCurrentEmpty_ReturnsFalse()
    {
        var t = new TestEntity { Str = "" };
        bool changed = t.AssignStringIfNotNull("", e => e.Str, (e, v) => e.Str = v);
        Assert.False(changed);
        Assert.Equal("", t.Str);
    }

    [Fact]
    public void WithGetter_EmptyValueAndCurrentNonEmpty_ReturnsTrue()
    {
        // Setting "" over an existing non-empty value IS a change.
        var t = new TestEntity { Str = "old" };
        bool changed = t.AssignStringIfNotNull("", e => e.Str, (e, v) => e.Str = v);
        Assert.True(changed);
        Assert.Equal("", t.Str);
    }

    [Fact]
    public void WithGetter_SameValue_ReturnsFalse()
    {
        var t = new TestEntity { Str = "x" };
        bool changed = t.AssignStringIfNotNull("x", e => e.Str, (e, v) => e.Str = v);
        Assert.False(changed);
    }

    [Fact]
    public void WithGetter_DifferentValue_ReturnsTrueAndAssigns()
    {
        var t = new TestEntity { Str = "x" };
        bool changed = t.AssignStringIfNotNull("y", e => e.Str, (e, v) => e.Str = v);
        Assert.True(changed);
        Assert.Equal("y", t.Str);
    }

    // --- Overload with source getter (PATCH / DeepCopy semantics) ---

    [Fact]
    public void WithSource_ComparesAgainstSourceNotTarget()
    {
        // PATCH pattern: target is the deep-copy being mutated, source is the
        // original we're comparing against. If source already has the value,
        // skip — even if target's local copy was mutated by something else.
        var source = new TestEntity { Str = "x" };
        var target = new TestEntity { Str = "z" };  // target's value is irrelevant
        bool changed = target.AssignStringIfNotNull("x", source, e => e.Str, (e, v) => e.Str = v);
        Assert.False(changed);
        Assert.Equal("z", target.Str);  // target left untouched
    }

    [Fact]
    public void WithSource_DifferentValue_AssignsToTarget()
    {
        var source = new TestEntity { Str = "x" };
        var target = new TestEntity { Str = "z" };
        bool changed = target.AssignStringIfNotNull("y", source, e => e.Str, (e, v) => e.Str = v);
        Assert.True(changed);
        Assert.Equal("y", target.Str);
        Assert.Equal("x", source.Str);  // source untouched
    }
}

public class AssignStringIfNotNullOrEmptyTests
{
    [Fact]
    public void NullValue_DoesNotAssign()
    {
        var t = new TestEntity { Str = "original" };
        t.AssignStringIfNotNullOrEmpty(null, (e, v) => e.Str = v);
        Assert.Equal("original", t.Str);
    }

    [Fact]
    public void EmptyValue_DoesNotAssign()
    {
        // Differs from AssignStringIfNotNull: empty string is treated as
        // "not specified" and skipped.
        var t = new TestEntity { Str = "original" };
        t.AssignStringIfNotNullOrEmpty("", (e, v) => e.Str = v);
        Assert.Equal("original", t.Str);
    }

    [Fact]
    public void NonEmptyValue_Assigns()
    {
        var t = new TestEntity();
        t.AssignStringIfNotNullOrEmpty("new", (e, v) => e.Str = v);
        Assert.Equal("new", t.Str);
    }
}

public class AssignNumberIfNotNullOrZeroTests
{
    [Fact]
    public void NullValue_DoesNotAssign()
    {
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNullOrZero<TestEntity, int>(null, (e, v) => e.Num = v);
        Assert.Equal(5, t.Num);
    }

    [Fact]
    public void ZeroValue_DoesNotAssign()
    {
        // Documented behaviour: zero is treated as "not specified" because
        // CSV empty columns coerce to 0 for int? parameters.
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNullOrZero<TestEntity, int>(0, (e, v) => e.Num = v);
        Assert.Equal(5, t.Num);
    }

    [Fact]
    public void NonZeroValue_Assigns()
    {
        var t = new TestEntity();
        t.AssignNumberIfNotNullOrZero<TestEntity, int>(7, (e, v) => e.Num = v);
        Assert.Equal(7, t.Num);
    }

    // --- Overload with target getter (change detection) ---

    [Fact]
    public void WithGetter_NullValue_ReturnsFalse()
    {
        var t = new TestEntity { Num = 5 };
        bool changed = t.AssignNumberIfNotNullOrZero<TestEntity, int>(null, e => e.Num, (e, v) => e.Num = v);
        Assert.False(changed);
    }

    [Fact]
    public void WithGetter_ZeroValue_ReturnsFalse()
    {
        var t = new TestEntity { Num = 5 };
        bool changed = t.AssignNumberIfNotNullOrZero<TestEntity, int>(0, e => e.Num, (e, v) => e.Num = v);
        Assert.False(changed);
        Assert.Equal(5, t.Num);
    }

    [Fact]
    public void WithGetter_SameValue_ReturnsFalse()
    {
        var t = new TestEntity { Num = 7 };
        bool changed = t.AssignNumberIfNotNullOrZero<TestEntity, int>(7, e => e.Num, (e, v) => e.Num = v);
        Assert.False(changed);
    }

    [Fact]
    public void WithGetter_DifferentValue_ReturnsTrueAndAssigns()
    {
        var t = new TestEntity { Num = 7 };
        bool changed = t.AssignNumberIfNotNullOrZero<TestEntity, int>(9, e => e.Num, (e, v) => e.Num = v);
        Assert.True(changed);
        Assert.Equal(9, t.Num);
    }

    // --- Overload with source getter (PATCH semantics) ---

    [Fact]
    public void WithSource_ComparesAgainstSource()
    {
        var source = new TestEntity { Num = 7 };
        var target = new TestEntity { Num = 99 };
        bool changed = target.AssignNumberIfNotNullOrZero<TestEntity, int>(7, source, e => e.Num, (e, v) => e.Num = v);
        Assert.False(changed);
        Assert.Equal(99, target.Num);
    }
}

public class AssignNumberIfNotNullTests
{
    [Fact]
    public void NullValue_DoesNotAssign()
    {
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNull<TestEntity, int>(null, (e, v) => e.Num = v);
        Assert.Equal(5, t.Num);
    }

    [Fact]
    public void ZeroValue_AssignsZero()
    {
        // Differs from AssignNumberIfNotNullOrZero: 0 is a real value here.
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNull<TestEntity, int>(0, (e, v) => e.Num = v);
        Assert.Equal(0, t.Num);
    }

    [Fact]
    public void NonZeroValue_Assigns()
    {
        var t = new TestEntity();
        t.AssignNumberIfNotNull<TestEntity, int>(42, (e, v) => e.Num = v);
        Assert.Equal(42, t.Num);
    }

    // --- String-coercion overload ---

    [Fact]
    public void FromString_NullValue_DoesNotAssign()
    {
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNull((string?)null, (e, v) => e.Num = v);
        Assert.Equal(5, t.Num);
    }

    [Fact]
    public void FromString_EmptyValue_AssignsNull()
    {
        // Documented behaviour: empty string clears the value (assigns null).
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNull("", (e, v) => e.Num = v);
        Assert.Null(t.Num);
    }

    [Fact]
    public void FromString_ParseableValue_Assigns()
    {
        var t = new TestEntity();
        t.AssignNumberIfNotNull("42", (e, v) => e.Num = v);
        Assert.Equal(42, t.Num);
    }

    [Fact]
    public void FromString_UnparseableNonEmptyValue_DoesNotAssign()
    {
        // "abc" is non-empty but not parseable: silently skipped, does NOT clear.
        var t = new TestEntity { Num = 5 };
        t.AssignNumberIfNotNull("abc", (e, v) => e.Num = v);
        Assert.Equal(5, t.Num);
    }

    // --- Overload with source (PATCH semantics) ---

    [Fact]
    public void WithSource_SameAsSource_ReturnsFalse()
    {
        var source = new TestEntity { Num = 7 };
        var target = new TestEntity { Num = 99 };
        bool changed = target.AssignNumberIfNotNull<TestEntity, int>(7, source, e => e.Num, (e, v) => e.Num = v);
        Assert.False(changed);
        Assert.Equal(99, target.Num);
    }

    [Fact]
    public void WithSource_DifferentFromSource_AssignsToTarget()
    {
        var source = new TestEntity { Num = 7 };
        var target = new TestEntity { Num = 99 };
        bool changed = target.AssignNumberIfNotNull<TestEntity, int>(11, source, e => e.Num, (e, v) => e.Num = v);
        Assert.True(changed);
        Assert.Equal(11, target.Num);
        Assert.Equal(7, source.Num);
    }

    [Fact]
    public void WithSource_ZeroValue_AssignsZeroIfDifferent()
    {
        // Distinct from AssignNumberIfNotNullOrZero: zero is a real value here,
        // so 0-against-non-zero-source is a change.
        var source = new TestEntity { Num = 5 };
        var target = new TestEntity { Num = 99 };
        bool changed = target.AssignNumberIfNotNull<TestEntity, int>(0, source, e => e.Num, (e, v) => e.Num = v);
        Assert.True(changed);
        Assert.Equal(0, target.Num);
    }
}

public class AssignBoolIfNotNullTests
{
    // --- string? value overload (parses the string) ---

    [Fact]
    public void FromString_Null_DoesNotAssign()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull((string?)null, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FromString_Empty_DoesNotAssign()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull("", (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FromString_True_AssignsTrue()
    {
        var t = new TestEntity { Flag = false };
        t.AssignBoolIfNotNull("true", (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FromString_False_AssignsFalse()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull("false", (e, v) => e.Flag = v);
        Assert.False(t.Flag);
    }

    [Fact]
    public void FromString_UnparseableValue_AssignsNull()
    {
        // Documented behaviour: anything non-bool but non-empty clears to null.
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull("abc", (e, v) => e.Flag = v);
        Assert.Null(t.Flag);
    }

    // --- bool? value overload (direct, no parsing) ---

    [Fact]
    public void FromBool_Null_DoesNotAssign()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull((bool?)null, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FromBool_True_Assigns()
    {
        var t = new TestEntity { Flag = false };
        t.AssignBoolIfNotNull((bool?)true, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FromBool_False_Assigns()
    {
        // bool? false IS a real value, gets assigned.
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotNull((bool?)false, (e, v) => e.Flag = v);
        Assert.False(t.Flag);
    }

    // --- string? + getter (change detection) ---

    [Fact]
    public void WithGetter_FromStringSameValue_ReturnsFalse()
    {
        var t = new TestEntity { Flag = true };
        bool changed = t.AssignBoolIfNotNull("true", e => e.Flag, (e, v) => e.Flag = v);
        Assert.False(changed);
    }

    [Fact]
    public void WithGetter_FromStringDifferentValue_ReturnsTrueAndAssigns()
    {
        var t = new TestEntity { Flag = false };
        bool changed = t.AssignBoolIfNotNull("true", e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(changed);
        Assert.True(t.Flag);
    }

    [Fact]
    public void WithGetter_UnparseableSetsNullIfCurrentNotNull()
    {
        var t = new TestEntity { Flag = true };
        bool changed = t.AssignBoolIfNotNull("abc", e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(changed);
        Assert.Null(t.Flag);
    }

    // --- string? + source + getter (PATCH semantics) ---

    [Fact]
    public void WithSource_FromStringSameAsSource_ReturnsFalse()
    {
        var source = new TestEntity { Flag = true };
        var target = new TestEntity { Flag = false };
        bool changed = target.AssignBoolIfNotNull("true", source, e => e.Flag, (e, v) => e.Flag = v);
        Assert.False(changed);
        Assert.False(target.Flag);  // target left untouched
    }

    [Fact]
    public void WithSource_FromStringDifferentFromSource_AssignsToTarget()
    {
        var source = new TestEntity { Flag = false };
        var target = new TestEntity { Flag = true };
        bool changed = target.AssignBoolIfNotNull("true", source, e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(changed);
        Assert.True(target.Flag);
        Assert.False(source.Flag);
    }
}

public class AssignBoolIfNotFalseSourceTests
{
    [Fact]
    public void WithSource_SameAsSource_ReturnsFalse()
    {
        var source = new TestEntity { Flag = true };
        var target = new TestEntity { Flag = true };
        bool changed = target.AssignBoolIfNotFalse("true", source, e => e.Flag, (e, v) => e.Flag = v);
        Assert.False(changed);
    }

    [Fact]
    public void WithSource_DifferentFromSource_AssignsTrue()
    {
        var source = new TestEntity { Flag = false };
        var target = new TestEntity { Flag = true };
        bool changed = target.AssignBoolIfNotFalse("true", source, e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(changed);
        Assert.True(target.Flag);
    }

    [Fact]
    public void WithSource_FalseAgainstNullTargetCurrent_DoesNotAssign()
    {
        // The not-false guard checks target's current value (not source's).
        // Even if source has true, if target's mutated state is null, false is skipped.
        var source = new TestEntity { Flag = true };
        var target = new TestEntity { Flag = null };
        bool changed = target.AssignBoolIfNotFalse("false", source, e => e.Flag, (e, v) => e.Flag = v);
        Assert.False(changed);
        Assert.Null(target.Flag);
    }
}

public class AssignBoolIfNotFalseTests
{
    [Fact]
    public void Null_DoesNotAssign()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotFalse(null, e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void Empty_DoesNotAssign()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotFalse("", e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }

    [Fact]
    public void FalseAgainstNullCurrent_DoesNotAssign()
    {
        // The signature guard: don't assign false when the current value is null.
        // Used to avoid flipping null → false in PATCH payloads where null means
        // "server keeps default".
        var t = new TestEntity { Flag = null };
        t.AssignBoolIfNotFalse("false", e => e.Flag, (e, v) => e.Flag = v);
        Assert.Null(t.Flag);
    }

    [Fact]
    public void FalseAgainstTrueCurrent_AssignsFalse()
    {
        var t = new TestEntity { Flag = true };
        t.AssignBoolIfNotFalse("false", e => e.Flag, (e, v) => e.Flag = v);
        Assert.False(t.Flag);
    }

    [Fact]
    public void TrueAgainstNullCurrent_AssignsTrue()
    {
        var t = new TestEntity { Flag = null };
        t.AssignBoolIfNotFalse("true", e => e.Flag, (e, v) => e.Flag = v);
        Assert.True(t.Flag);
    }
}

public class AssignDateTimeIfNotNullTests
{
    [Fact]
    public void Null_DoesNotAssign()
    {
        var t = new TestEntity { When = DateTime.UnixEpoch };
        t.AssignDateTimeIfNotNull(null, (e, v) => e.When = v);
        Assert.Equal(DateTime.UnixEpoch, t.When);
    }

    [Fact]
    public void NonNullWithUtcConversion_AssignsAsUtc()
    {
        // Default convertToUniversalTime: true
        var local = new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Local);
        var t = new TestEntity();
        t.AssignDateTimeIfNotNull(local, (e, v) => e.When = v);
        Assert.NotNull(t.When);
        Assert.Equal(DateTimeKind.Utc, t.When!.Value.Kind);
    }

    [Fact]
    public void NonNullWithUtcConversionDisabled_AssignsAsIs()
    {
        var local = new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Local);
        var t = new TestEntity();
        t.AssignDateTimeIfNotNull(local, (e, v) => e.When = v, convertToUniversalTime: false);
        Assert.Equal(local, t.When);
    }
}

public class AssignTagsTests
{
    [Fact]
    public void NullArray_DoesNotAssign()
    {
        var t = new TestEntity { Tags = [new Tag { Name = "old" }] };
        t.AssignTags(null, (e, v) => e.Tags = v);
        Assert.NotNull(t.Tags);
        Assert.Single(t.Tags);
    }

    [Fact]
    public void EmptyArray_DoesNotAssign()
    {
        var t = new TestEntity { Tags = [new Tag { Name = "old" }] };
        t.AssignTags([], (e, v) => e.Tags = v);
        Assert.NotNull(t.Tags);
        Assert.Single(t.Tags);
    }

    [Fact]
    public void KeyOnly_AssignsTagWithNullValue()
    {
        var t = new TestEntity();
        t.AssignTags(["env"], (e, v) => e.Tags = v);
        Assert.NotNull(t.Tags);
        Assert.Single(t.Tags);
        Assert.Equal("env", t.Tags[0].Name);
        Assert.Null(t.Tags[0].Value);
    }

    [Fact]
    public void KeyEqualsValue_AssignsTagWithValue()
    {
        var t = new TestEntity();
        t.AssignTags(["env=prod"], (e, v) => e.Tags = v);
        Assert.NotNull(t.Tags);
        Assert.Single(t.Tags);
        Assert.Equal("env", t.Tags[0].Name);
        Assert.Equal("prod", t.Tags[0].Value);
    }

    [Fact]
    public void CommaSeparated_SplitsIntoMultipleTags()
    {
        // ConvertToTags treats both commas and the input array as separators.
        var t = new TestEntity();
        t.AssignTags(["a,b=c"], (e, v) => e.Tags = v);
        Assert.NotNull(t.Tags);
        Assert.Equal(2, t.Tags.Length);
        Assert.Equal("a", t.Tags[0].Name);
        Assert.Null(t.Tags[0].Value);
        Assert.Equal("b", t.Tags[1].Name);
        Assert.Equal("c", t.Tags[1].Value);
    }
}

public class AssignIdFromNameTests
{
    private sealed class TestEntry
    {
        public string Name { get; init; } = "";
        public long Id { get; init; }
    }

    private sealed class CapturingHost : IWritableHost
    {
        public List<ErrorRecord> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public List<ProgressRecord> Progress { get; } = new();
        public void WriteError(ErrorRecord errorRecord) => Errors.Add(errorRecord);
        public void WriteWarning(string text) => Warnings.Add(text);
        public void WriteProgress(ProgressRecord progressRecord) => Progress.Add(progressRecord);
        public bool ShouldProcess(string target, string action) => true;
    }

    [Fact]
    public void NullName_ReturnsTrueAndDoesNotAssign()
    {
        var host = new CapturingHost();
        var t = new TestEntity { Id = 99 };
        bool ok = t.AssignIdFromName(
            null,
            () => new[] { new TestEntry { Name = "a", Id = 1 } },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.True(ok);
        Assert.Equal(99, t.Id);
        Assert.Empty(host.Errors);
    }

    [Fact]
    public void EmptyName_ClearsIdAndReturnsTrue()
    {
        // Documented behaviour: empty string explicitly clears the id (sets to default).
        var host = new CapturingHost();
        var t = new TestEntity { Id = 99 };
        bool ok = t.AssignIdFromName(
            "",
            () => new[] { new TestEntry { Name = "a", Id = 1 } },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.True(ok);
        Assert.Equal(0, t.Id);
        Assert.Empty(host.Errors);
    }

    [Fact]
    public void ExactMatch_AssignsAndReturnsTrue()
    {
        var host = new CapturingHost();
        var t = new TestEntity();
        bool ok = t.AssignIdFromName(
            "alpha",
            () => new[] { new TestEntry { Name = "alpha", Id = 7 }, new TestEntry { Name = "beta", Id = 8 } },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.True(ok);
        Assert.Equal(7, t.Id);
        Assert.Empty(host.Errors);
    }

    [Fact]
    public void WildcardMatch_AssignsAndReturnsTrue()
    {
        var host = new CapturingHost();
        var t = new TestEntity();
        bool ok = t.AssignIdFromName(
            "alp*",
            () => new[] { new TestEntry { Name = "alpha", Id = 7 } },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.True(ok);
        Assert.Equal(7, t.Id);
    }

    [Fact]
    public void NoMatch_WritesErrorAndReturnsFalse()
    {
        var host = new CapturingHost();
        var t = new TestEntity();
        bool ok = t.AssignIdFromName(
            "missing",
            () => new[] { new TestEntry { Name = "alpha", Id = 7 } },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.False(ok);
        Assert.Single(host.Errors);
        Assert.Contains("No Thing found", host.Errors[0].Exception.Message);
    }

    [Fact]
    public void MultipleMatches_WritesErrorAndReturnsFalse()
    {
        var host = new CapturingHost();
        var t = new TestEntity();
        bool ok = t.AssignIdFromName(
            "*",
            () => new[]
            {
                new TestEntry { Name = "alpha", Id = 7 },
                new TestEntry { Name = "beta", Id = 8 },
            },
            e => e.Name, e => e.Id,
            (s, v) => s.Id = v,
            host, "target", "Thing");
        Assert.False(ok);
        Assert.Single(host.Errors);
        Assert.Contains("Multiple Thing found", host.Errors[0].Exception.Message);
    }
}
