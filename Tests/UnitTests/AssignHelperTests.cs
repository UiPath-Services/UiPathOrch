using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Commands.CsvHelper;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

internal sealed class CapturingHost : IWritableHost
{
    public List<ErrorRecord> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<ProgressRecord> Progress { get; } = new();
    public void WriteError(ErrorRecord errorRecord) => Errors.Add(errorRecord);
    public void WriteWarning(string text) => Warnings.Add(text);
    public void WriteProgress(ProgressRecord progressRecord) => Progress.Add(progressRecord);
    public bool ShouldProcess(string target, string action) => true;
}

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

    // --- Overload with source getter (PATCH / DeepCopy semantics) ---
    // Note: a target-only-getter PUT overload existed historically but had
    // zero callsites in the codebase, so it was removed. Update cmdlets all
    // use the source-getter form for PATCH semantics. The empty-vs-null
    // equivalence is exercised through the WithSource_* tests below — the
    // PATCH form's body is structurally identical to the deleted PUT form
    // (just compares against source instead of target).

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

    // --- Overload with source getter (PATCH semantics) ---
    // (PUT overload with target-only getter was removed — zero callsites.)

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

    // --- string? + source + getter (PATCH semantics) ---
    // (PUT overload with target-only getter was removed — zero callsites.)

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

public class MergeNonEmptyValueTests
{
    // Pinned semantics for the multi-row CSV-aggregation merge rule.
    // Priority: non-empty > "" > null. Among non-empty values, last-writer-wins.
    // Originally duplicated as MergeDescription in SetAsset / SetCredentialAsset /
    // SetSecretAsset; extracted to a shared helper in OrchExtensions.cs.

    [Fact]
    public void NullValue_DoesNothing()
    {
        var d = new Dictionary<string, string> { ["k"] = "old" };
        d.MergeNonEmptyValue("k", null);
        Assert.Equal("old", d["k"]);
    }

    [Fact]
    public void NullValueOnEmptyDictionary_DoesNotInsert()
    {
        var d = new Dictionary<string, string>();
        d.MergeNonEmptyValue("k", null);
        Assert.False(d.ContainsKey("k"));
    }

    [Fact]
    public void EmptyValueOnEmptyDictionary_InsertsEmpty()
    {
        // First write: existing absent → matches "!TryGetValue" branch and inserts "".
        // Reflects current behaviour: "" is recorded so a later read sees it.
        var d = new Dictionary<string, string>();
        d.MergeNonEmptyValue("k", "");
        Assert.True(d.ContainsKey("k"));
        Assert.Equal("", d["k"]);
    }

    [Fact]
    public void EmptyValueOverExisting_DoesNotOverwrite()
    {
        // Priority rule: empty cannot beat existing non-empty.
        var d = new Dictionary<string, string> { ["k"] = "OLD" };
        d.MergeNonEmptyValue("k", "");
        Assert.Equal("OLD", d["k"]);
    }

    [Fact]
    public void EmptyValueOverExistingEmpty_DoesNotOverwrite()
    {
        var d = new Dictionary<string, string> { ["k"] = "" };
        d.MergeNonEmptyValue("k", "");
        Assert.Equal("", d["k"]);
    }

    [Fact]
    public void NonEmptyValueOnEmptyDictionary_Inserts()
    {
        var d = new Dictionary<string, string>();
        d.MergeNonEmptyValue("k", "NEW");
        Assert.Equal("NEW", d["k"]);
    }

    [Fact]
    public void NonEmptyOverEmpty_Overwrites()
    {
        // Priority: non-empty beats "".
        var d = new Dictionary<string, string> { ["k"] = "" };
        d.MergeNonEmptyValue("k", "NEW");
        Assert.Equal("NEW", d["k"]);
    }

    [Fact]
    public void NonEmptyOverNonEmpty_LastWriterWins()
    {
        // Among non-empty values, last writer wins.
        var d = new Dictionary<string, string> { ["k"] = "OLD" };
        d.MergeNonEmptyValue("k", "NEW");
        Assert.Equal("NEW", d["k"]);
    }

    [Fact]
    public void TupleKeys_WorkLikeAnyOther()
    {
        // Real-world callsite uses (name, path) tuple keys.
        var d = new Dictionary<(string name, string path), string>();
        d.MergeNonEmptyValue(("asset1", "Orch1:\\Folder"), "first");
        d.MergeNonEmptyValue(("asset1", "Orch1:\\Folder"), "");      // empty: skip
        d.MergeNonEmptyValue(("asset1", "Orch1:\\Folder"), "second"); // non-empty: overwrite
        d.MergeNonEmptyValue(("asset2", "Orch1:\\Folder"), "other");  // different key: inserts
        Assert.Equal("second", d[("asset1", "Orch1:\\Folder")]);
        Assert.Equal("other", d[("asset2", "Orch1:\\Folder")]);
    }
}

public class AssignIdFromNameTests
{
    private sealed class TestEntry
    {
        public string Name { get; init; } = "";
        public long Id { get; init; }
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

// CsvLineBase.AssignStringValue / AssignIntValue / AssignBoolValue are the multi-
// row CSV aggregation helpers used by Add-OrchUser. They detect collisions across
// rows that share an identity (e.g. one user appearing in multiple CSV rows for
// different roles): the first row populates the CsvLine, subsequent rows pass
// through Update() which calls these helpers to either no-op (matching value),
// warn (different value), or skip (current row didn't specify the field).
//
// Pre-fix bug: the string and int variants used to fall through to setter(newValue)
// when newValue was null/empty (string) or null/0 (int), silently clobbering a
// value set by an earlier row with whatever the later row left blank. The bool
// variant always had the correct early-return; the string and int variants were
// brought into line.

public class AssignStringValueTests
{
    [Fact]
    public void NullNewValue_DoesNotInvokeSetterOrWarn()
    {
        // Pre-fix: setter(null) was called, clobbering the earlier row's value.
        // Post-fix: early return preserves the earlier row's value.
        var host = new CapturingHost();
        string? captured = "untouched";
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", "previousValue", null, v => captured = v);
        Assert.Equal("untouched", captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void EmptyNewValue_DoesNotInvokeSetterOrWarn()
    {
        // Pre-fix: setter("") was called, clobbering the earlier row's value with empty.
        // Post-fix: empty CSV cell on a later row leaves the earlier row's value alone.
        var host = new CapturingHost();
        string? captured = "untouched";
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", "previousValue", "", v => captured = v);
        Assert.Equal("untouched", captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueMatchingCurrent_InvokesSetter_NoWarning()
    {
        // Same value across rows: no collision, setter fires (effectively a no-op
        // since current already equals new). Behaviour is unchanged from pre-fix.
        var host = new CapturingHost();
        string? captured = null;
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", "alice@example", "alice@example", v => captured = v);
        Assert.Equal("alice@example", captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueDifferingFromCurrent_DoesNotSet_EmitsWarning()
    {
        // Collision across rows: warning emitted, current row's value NOT applied.
        // The warning text quotes the previously-specified value for diagnostics.
        var host = new CapturingHost();
        string? captured = "previous";
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", "previous", "different", v => captured = v);
        Assert.Equal("previous", captured); // setter not invoked
        Assert.Single(host.Warnings);
        Assert.Contains("OrchTest", host.Warnings[0]);
        Assert.Contains("alice", host.Warnings[0]);
        Assert.Contains("'previous'", host.Warnings[0]);
    }

    [Fact]
    public void NewValueSpecifiedWhenCurrentNull_AssignsValue_NoWarning()
    {
        // Row 1 didn't specify this field (currentValue is null) and Row 2 does.
        // This is "first real value", not a collision — assign and stay silent.
        // Pre-fix the helper warned "specified multiple times" with a `''`
        // previously-specified value, AND silently rejected Row 2's value.
        var host = new CapturingHost();
        string? captured = null;
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", null, "first-real-value", v => captured = v);
        Assert.Equal("first-real-value", captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueSpecifiedWhenCurrentEmpty_AssignsValue_NoWarning()
    {
        // Row 1 had an empty cell for this field (currentValue = ""); Row 2
        // specifies a real value. Same logic as the null case — assign, no warn.
        var host = new CapturingHost();
        string? captured = "";
        CsvLineBase.AssignStringValue(host, "OrchTest", "alice", "", "first-real-value", v => captured = v);
        Assert.Equal("first-real-value", captured);
        Assert.Empty(host.Warnings);
    }
}

public class AssignIntValueTests
{
    [Fact]
    public void NullNewValue_DoesNotInvokeSetterOrWarn()
    {
        // Pre-fix: setter(null) was called, clobbering the earlier row.
        // Post-fix: early return preserves the earlier row's value.
        var host = new CapturingHost();
        int? captured = 99;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", 1024, null, v => captured = v);
        Assert.Equal(99, captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void ZeroNewValue_DoesNotInvokeSetterOrWarn()
    {
        // Pre-fix: setter(0) was called when CSV empty cell coerced to 0.
        // Post-fix: 0 is treated as unspecified (CSV empty-cell sentinel) and skipped.
        var host = new CapturingHost();
        int? captured = 99;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", 1024, 0, v => captured = v);
        Assert.Equal(99, captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueMatchingCurrent_InvokesSetter_NoWarning()
    {
        var host = new CapturingHost();
        int? captured = null;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", 1024, 1024, v => captured = v);
        Assert.Equal(1024, captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueDifferingFromCurrent_DoesNotSet_EmitsWarning()
    {
        var host = new CapturingHost();
        int? captured = 1024;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", 1024, 768, v => captured = v);
        Assert.Equal(1024, captured);
        Assert.Single(host.Warnings);
        Assert.Contains("1024", host.Warnings[0]);
    }

    [Fact]
    public void NewValueSpecifiedWhenCurrentNull_AssignsValue_NoWarning()
    {
        // Row 1 didn't specify (currentValue=null); Row 2 specifies a real value.
        // First-real-value, not a collision — assign and stay silent.
        var host = new CapturingHost();
        int? captured = null;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", null, 1024, v => captured = v);
        Assert.Equal(1024, captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void NewValueSpecifiedWhenCurrentZero_AssignsValue_NoWarning()
    {
        // Row 1's empty CSV cell coerced to 0 (currentValue=0); Row 2 specifies.
        // Same logic as the null case — 0 is the CSV-empty sentinel for int?.
        var host = new CapturingHost();
        int? captured = 0;
        CsvLineBase.AssignIntValue(host, "OrchTest", "alice", 0, 1024, v => captured = v);
        Assert.Equal(1024, captured);
        Assert.Empty(host.Warnings);
    }
}

public class AssignBoolValueTests
{
    [Fact]
    public void NullNewValue_DoesNotInvokeSetterOrWarn()
    {
        var host = new CapturingHost();
        bool? captured = true;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", true, null, v => captured = v);
        Assert.True(captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void EmptyNewValue_DoesNotInvokeSetterOrWarn()
    {
        var host = new CapturingHost();
        bool? captured = true;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", true, "", v => captured = v);
        Assert.True(captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void UnparseableNewValue_DoesNotInvokeSetterOrWarn()
    {
        // bool.TryParse on garbage → silently skip (NOT clear or warn).
        var host = new CapturingHost();
        bool? captured = true;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", true, "yes-please", v => captured = v);
        Assert.True(captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void ParseableSameAsCurrent_InvokesSetter_NoWarning()
    {
        var host = new CapturingHost();
        bool? captured = null;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", true, "true", v => captured = v);
        Assert.True(captured);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void ParseableDifferentFromCurrent_DoesNotSet_EmitsWarning()
    {
        var host = new CapturingHost();
        bool? captured = true;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", true, "false", v => captured = v);
        Assert.True(captured); // setter not invoked
        Assert.Single(host.Warnings);
    }

    [Fact]
    public void ParseableNewValueWhenCurrentNull_AssignsValue_NoWarning()
    {
        // Row 1 didn't specify (currentValue=null); Row 2 parses to a real bool.
        // First-real-value, not a collision. (Less impactful than the string/int
        // cases because AddUser's CsvLine constructor defaults bool fields to
        // false via `?? false`, but the helper should still behave consistently.)
        var host = new CapturingHost();
        bool? captured = null;
        CsvLineBase.AssignBoolValue(host, "OrchTest", "alice", null, "true", v => captured = v);
        Assert.True(captured);
        Assert.Empty(host.Warnings);
    }
}

// Scenario tests that simulate AddUser.cs CsvLine.Update calls in sequence,
// modelling the per-(user, role) CSV import pattern: row 1 populates all
// scalar fields via the constructor, rows 2..N typically only specify a
// different Role and leave the scalar cells blank. These tests pin the
// post-fix behaviour of the multi-row aggregation as a whole, complementing
// the per-helper unit tests above.
//
// FakeCsvLine mirrors the shape of AddUser.cs's private CsvLine class for
// the fields actually exercised by the scenarios (UR_*, ES_*, MayHave*).
// CsvLine itself is a private nested type and not directly testable; this
// fake stands in for the same orchestration of helper calls.

public class MultiRowCsvScenarioTests
{
    private sealed class FakeCsvLine
    {
        // String scalars
        public string? UR_UserName { get; set; }
        public string? UR_CredentialType { get; set; }
        public string? ES_TracingLevel { get; set; }

        // Int scalars
        public int? ES_ResolutionWidth { get; set; }
        public int? ES_ResolutionHeight { get; set; }

        // Bool scalars (parsed from string at the parameter binder)
        public bool? IsExternalLicensed { get; set; }
        public bool? MayHaveRobotSession { get; set; }

        // Roles HashSet — accumulated, not collision-detected
        public HashSet<string> Roles { get; } = new();
    }

    // Helper: simulate one row's worth of Update() calls against an existing FakeCsvLine.
    // Mirrors the structure of AddUser.cs CsvLine.Update.
    private static void SimulateRowUpdate(
        IWritableHost host,
        FakeCsvLine line,
        string userName,
        string? ur_userName, string? ur_credentialType,
        string? es_tracingLevel,
        int? es_resolutionWidth, int? es_resolutionHeight,
        string? isExternalLicensed, string? mayHaveRobotSession,
        IEnumerable<string>? roles)
    {
        const string drive = "OrchTest";
        CsvLineBase.AssignStringValue(host, drive, userName, line.UR_UserName, ur_userName, v => line.UR_UserName = v);
        CsvLineBase.AssignStringValue(host, drive, userName, line.UR_CredentialType, ur_credentialType, v => line.UR_CredentialType = v);
        CsvLineBase.AssignStringValue(host, drive, userName, line.ES_TracingLevel, es_tracingLevel, v => line.ES_TracingLevel = v);
        CsvLineBase.AssignIntValue(host, drive, userName, line.ES_ResolutionWidth, es_resolutionWidth, v => line.ES_ResolutionWidth = v);
        CsvLineBase.AssignIntValue(host, drive, userName, line.ES_ResolutionHeight, es_resolutionHeight, v => line.ES_ResolutionHeight = v);
        CsvLineBase.AssignBoolValue(host, drive, userName, line.IsExternalLicensed, isExternalLicensed, v => line.IsExternalLicensed = v);
        CsvLineBase.AssignBoolValue(host, drive, userName, line.MayHaveRobotSession, mayHaveRobotSession, v => line.MayHaveRobotSession = v);
        if (roles is not null)
        {
            line.Roles.UnionWith(roles.Where(r => !string.IsNullOrEmpty(r)));
        }
    }

    [Fact]
    public void TypicalCsv_TwoRowsSameUserDifferentRoles_PreservesRow1ScalarsAndAccumulatesRoles()
    {
        // Realistic CSV pattern (per-(user, role) export):
        //   UserName,Role,IsExternalLicensed,UR_UserName,UR_CredentialType,ES_TracingLevel,ES_ResolutionWidth
        //   alice,RoleA,true,domain\alice,UsernamePassword,Verbose,1024
        //   alice,RoleB,,,,,
        //
        // Row 1 populates everything via ctor (here: direct field init).
        // Row 2 carries only the additional Role; all other cells are blank.
        // Expected: scalar fields stay at row-1 values, Roles accumulates {A, B}.
        // Pre-fix: row 2's blank string/int cells would clobber row 1 → POST-time
        // EndProcessing checks would skip the UnattendedRobot block entirely.

        var host = new CapturingHost();
        var line = new FakeCsvLine
        {
            // Simulate ctor from row 1
            UR_UserName = @"domain\alice",
            UR_CredentialType = "UsernamePassword",
            ES_TracingLevel = "Verbose",
            ES_ResolutionWidth = 1024,
            IsExternalLicensed = true,
            MayHaveRobotSession = false,
        };
        line.Roles.Add("RoleA");

        // Simulate row 2: only Role specified, all other cells blank
        SimulateRowUpdate(host, line, "alice",
            ur_userName: "", ur_credentialType: "",
            es_tracingLevel: "",
            es_resolutionWidth: 0, es_resolutionHeight: 0,
            isExternalLicensed: "", mayHaveRobotSession: "",
            roles: new[] { "RoleB" });

        Assert.Equal(@"domain\alice", line.UR_UserName);
        Assert.Equal("UsernamePassword", line.UR_CredentialType);
        Assert.Equal("Verbose", line.ES_TracingLevel);
        Assert.Equal(1024, line.ES_ResolutionWidth);
        Assert.True(line.IsExternalLicensed);
        Assert.False(line.MayHaveRobotSession);

        Assert.Equal(2, line.Roles.Count);
        Assert.Contains("RoleA", line.Roles);
        Assert.Contains("RoleB", line.Roles);

        // No collision: every blank cell was unspecified, not conflicting.
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void ConflictingScalarOnRow2_TriggersWarning_KeepsRow1Value()
    {
        // Row 2 explicitly disagrees on a scalar — should warn and keep row 1's value.
        var host = new CapturingHost();
        var line = new FakeCsvLine { UR_UserName = @"domain\alice", IsExternalLicensed = true };
        line.Roles.Add("RoleA");

        SimulateRowUpdate(host, line, "alice",
            ur_userName: @"domain\bob",      // conflict with row 1's "alice"
            ur_credentialType: null,
            es_tracingLevel: null,
            es_resolutionWidth: null, es_resolutionHeight: null,
            isExternalLicensed: "false",     // conflict with row 1's true
            mayHaveRobotSession: null,
            roles: new[] { "RoleB" });

        // First-wins: row 1's values survive
        Assert.Equal(@"domain\alice", line.UR_UserName);
        Assert.True(line.IsExternalLicensed);
        // Two warnings: one per conflicting scalar
        Assert.Equal(2, host.Warnings.Count);
        // Roles still accumulate normally
        Assert.Contains("RoleA", line.Roles);
        Assert.Contains("RoleB", line.Roles);
    }

    [Fact]
    public void Row2RepeatsSameScalars_NoWarning_ValuesUnchanged()
    {
        // Row 2 carries the same scalar values as row 1 (e.g. duplicated CSV).
        // Should be silent — no warning, no harm.
        var host = new CapturingHost();
        var line = new FakeCsvLine
        {
            UR_UserName = @"domain\alice",
            IsExternalLicensed = true,
            ES_ResolutionWidth = 1024,
        };

        SimulateRowUpdate(host, line, "alice",
            ur_userName: @"domain\alice", ur_credentialType: null,
            es_tracingLevel: null,
            es_resolutionWidth: 1024, es_resolutionHeight: null,
            isExternalLicensed: "true", mayHaveRobotSession: null,
            roles: null);

        Assert.Equal(@"domain\alice", line.UR_UserName);
        Assert.True(line.IsExternalLicensed);
        Assert.Equal(1024, line.ES_ResolutionWidth);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void ManyBlankRowsAfterRow1_ScalarsRemainStable()
    {
        // Stress: simulate a CSV where one user has 5 roles split across 5 rows.
        // Rows 2..5 all leave scalar cells blank. Scalars must not drift.
        var host = new CapturingHost();
        var line = new FakeCsvLine
        {
            UR_UserName = @"domain\alice",
            IsExternalLicensed = true,
            ES_ResolutionWidth = 1024,
        };
        line.Roles.Add("RoleA");

        for (int i = 2; i <= 5; i++)
        {
            SimulateRowUpdate(host, line, "alice",
                ur_userName: "", ur_credentialType: "",
                es_tracingLevel: "",
                es_resolutionWidth: 0, es_resolutionHeight: 0,
                isExternalLicensed: "", mayHaveRobotSession: "",
                roles: new[] { $"Role{(char)('A' + i - 1)}" });
        }

        Assert.Equal(@"domain\alice", line.UR_UserName);
        Assert.True(line.IsExternalLicensed);
        Assert.Equal(1024, line.ES_ResolutionWidth);
        Assert.Empty(host.Warnings);
        Assert.Equal(5, line.Roles.Count); // RoleA..RoleE
    }

    [Fact]
    public void Row1FieldUnsetRow2SetsIt_AssignsRow2Value_NoWarning()
    {
        // The complementary case to TypicalCsv_TwoRowsSameUserDifferentRoles_*:
        // row 1 leaves a scalar field blank (e.g. user identifies but doesn't
        // configure UR), row 2 then provides the value. Should be treated as
        // first-real-value, not a "specified multiple times" collision.
        //
        //   UserName,Type,Role,UR_UserName,ES_TracingLevel
        //   alice,DirectoryUser,RoleA,,Verbose
        //   alice,DirectoryUser,RoleB,domain\alice,
        //
        // Pre-fix: row 2's UR_UserName was rejected with a confusing
        //   "specified multiple times. Using the previously specified value ''"
        // warning, and the user got POSTed with no UnattendedRobot block.

        var host = new CapturingHost();
        var line = new FakeCsvLine
        {
            UR_UserName = null,                  // row 1 didn't specify
            ES_TracingLevel = "Verbose",
            ES_ResolutionWidth = null,
            IsExternalLicensed = null,
        };
        line.Roles.Add("RoleA");

        SimulateRowUpdate(host, line, "alice",
            ur_userName: @"domain\alice",        // row 2 specifies for the first time
            ur_credentialType: null,
            es_tracingLevel: "",                 // already on row 1
            es_resolutionWidth: 1024,            // also a first-time set
            es_resolutionHeight: null,
            isExternalLicensed: "true",          // first-time set
            mayHaveRobotSession: null,
            roles: new[] { "RoleB" });

        Assert.Equal(@"domain\alice", line.UR_UserName);
        Assert.Equal("Verbose", line.ES_TracingLevel);   // preserved from row 1
        Assert.Equal(1024, line.ES_ResolutionWidth);     // first-time set from row 2
        Assert.True(line.IsExternalLicensed);            // first-time set from row 2
        Assert.Equal(2, line.Roles.Count);
        Assert.Empty(host.Warnings);                     // no spurious collisions
    }

    [Fact]
    public void Row2EmptyCellOnFieldRow1DidNotSet_LeavesFieldUnset()
    {
        // Row 1 didn't specify ES_ResolutionWidth (column missing or empty).
        // Row 2 also doesn't specify it. The field stays null, no warning.
        var host = new CapturingHost();
        var line = new FakeCsvLine(); // ES_ResolutionWidth = null

        SimulateRowUpdate(host, line, "alice",
            ur_userName: null, ur_credentialType: null,
            es_tracingLevel: null,
            es_resolutionWidth: 0, es_resolutionHeight: 0,
            isExternalLicensed: null, mayHaveRobotSession: null,
            roles: null);

        Assert.Null(line.ES_ResolutionWidth);
        Assert.Null(line.ES_ResolutionHeight);
        Assert.Empty(host.Warnings);
    }
}
