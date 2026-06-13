using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Pattern A/B base classes (RemoveFolderEntityCmdletBase / RemoveDriveEntityCmdletBase)
// and the OrchArgumentCompleter family lean heavily on these wildcard / value-set
// extension helpers. The classes themselves are PSCmdlet derivatives that need a
// PowerShell Runspace to host, so direct unit testing is impractical; covering the
// pure-function utilities they call gives us the same regression-detection value
// for far less infrastructure.

public class ConvertToWildcardPatternListTests
{
    [Fact]
    public void NullInput_ReturnsNull()
    {
        IEnumerable<string?>? input = null;
        Assert.Null(input.ConvertToWildcardPatternList());
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyList()
    {
        var result = Array.Empty<string?>().ConvertToWildcardPatternList();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void LiteralString_BuildsPatternMatchingItself()
    {
        var patterns = new[] { "Apple" }.ConvertToWildcardPatternList();
        Assert.NotNull(patterns);
        Assert.Single(patterns);
        Assert.True(patterns[0].IsMatch("Apple"));
        Assert.True(patterns[0].IsMatch("apple")); // case-insensitive
        Assert.False(patterns[0].IsMatch("Apples"));
    }

    [Fact]
    public void WildcardString_BuildsPatternHonoringWildcards()
    {
        var patterns = new[] { "App*" }.ConvertToWildcardPatternList();
        Assert.NotNull(patterns);
        Assert.True(patterns[0].IsMatch("Apple"));
        Assert.True(patterns[0].IsMatch("Application"));
        Assert.False(patterns[0].IsMatch("Banana"));
    }

    [Fact]
    public void MultipleStrings_BuildOnePatternEach()
    {
        var patterns = new[] { "Apple", "Banana" }.ConvertToWildcardPatternList();
        Assert.NotNull(patterns);
        Assert.Equal(2, patterns.Count);
    }
}

public class FilterByWildcardsTests
{
    private static readonly string[] Fruits = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry"];

    [Fact]
    public void NullPatternList_ReturnsSourceUnchanged()
    {
        List<WildcardPattern>? patterns = null;
        var result = Fruits.FilterByWildcards(s => s, patterns).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void EmptyPatternList_ReturnsSourceUnchanged()
    {
        var result = Fruits.FilterByWildcards(s => s, new List<WildcardPattern>()).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void NullStringArray_ReturnsSourceUnchanged()
    {
        string[]? patterns = null;
        var result = Fruits.FilterByWildcards(s => s, patterns).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void EmptyStringArray_ReturnsSourceUnchanged()
    {
        var result = Fruits.FilterByWildcards(s => s, Array.Empty<string>()).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void SinglePrefixWildcard_KeepsMatchingItems()
    {
        var result = Fruits.FilterByWildcards(s => s, new[] { "B*" }).ToArray();
        Assert.Equal(new[] { "Banana", "Blueberry" }, result);
    }

    [Fact]
    public void MultiplePatterns_OrLogic()
    {
        var result = Fruits.FilterByWildcards(s => s, new[] { "A*", "C*" }).ToArray();
        Assert.Equal(new[] { "Apple", "Apricot", "Cherry" }, result);
    }

    [Fact]
    public void PatternMatchingNothing_ReturnsEmpty()
    {
        var result = Fruits.FilterByWildcards(s => s, new[] { "Z*" }).ToArray();
        Assert.Empty(result);
    }

    [Fact]
    public void CaseInsensitiveMatching()
    {
        var result = Fruits.FilterByWildcards(s => s, new[] { "apple" }).ToArray();
        Assert.Equal(new[] { "Apple" }, result);
    }

    [Fact]
    public void SelectorReturningNull_FiltersOutThoseEntries()
    {
        // Pattern A's Name selectors are typically `e => e?.Name` and entities can have null Name.
        var input = new[] { "Apple", null!, "Banana" };
        var result = input.FilterByWildcards(s => s, new[] { "*" }).ToArray();
        // WildcardPattern.IsMatch(null) is false, so null-named entries drop out.
        Assert.Equal(new[] { "Apple", "Banana" }, result);
    }
}

// FilterByNames is the literal-first matcher behind the destructive cmdlets
// (Remove-*/Move-*/*-Link). It keys on Name (cross-tenant-stable) and treats a
// requested name as literal when an entity is named exactly that, falling back to
// wildcard only when there is no literal hit. These cover the gap it closes: a piped
// entity whose Name contains wildcard metacharacters ("Sales [EU]") must match itself,
// not be re-interpreted as a wildcard (which over-matched or threw before).
public class FilterByNamesTests
{
    private static readonly string[] Fruits = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry"];

    [Fact]
    public void NullNames_ReturnsSourceUnchanged()
    {
        string[]? names = null;
        Assert.Equal(Fruits, Fruits.FilterByNames(s => s, names).ToArray());
    }

    [Fact]
    public void EmptyNames_ReturnsSourceUnchanged()
    {
        Assert.Equal(Fruits, Fruits.FilterByNames(s => s, Array.Empty<string>()).ToArray());
    }

    [Fact]
    public void LiteralName_MatchesExactlyNotPrefix()
    {
        var result = Fruits.FilterByNames(s => s, new[] { "Apple" }).ToArray();
        Assert.Equal(new[] { "Apple" }, result); // not Apricot
    }

    [Fact]
    public void CaseInsensitiveLiteral()
    {
        Assert.Equal(new[] { "Apple" }, Fruits.FilterByNames(s => s, new[] { "apple" }).ToArray());
    }

    [Fact]
    public void NameWithMetacharacters_MatchedLiterallyWhenItExists()
    {
        // The core fix: a piped entity literally named with wildcard metacharacters
        // must match itself, not be re-interpreted as a wildcard.
        var source = new[] { "Sales E", "Sales U", "Sales [EU]" };
        var result = source.FilterByNames(s => s, new[] { "Sales [EU]" }).ToArray();
        Assert.Equal(new[] { "Sales [EU]" }, result); // not "Sales E" / "Sales U"
    }

    [Fact]
    public void WildcardIntent_ExpandsWhenNoLiteralMatch()
    {
        var result = Fruits.FilterByNames(s => s, new[] { "B*" }).ToArray();
        Assert.Equal(new[] { "Banana", "Blueberry" }, result);
    }

    [Fact]
    public void InvalidPatternWithNoLiteralMatch_NoThrowNoMatch()
    {
        // "Z[" is an invalid wildcard pattern AND matches no entity literally. It must
        // not throw (that would abort the whole removal); it yields nothing.
        var result = Fruits.FilterByNames(s => s, new[] { "Z[" }).ToArray();
        Assert.Empty(result);
    }

    [Fact]
    public void LiteralMetacharName_ShadowsWildcardIntent_LiteralWins()
    {
        // Pathological case: an entity literally named "B*" exists while other entities
        // would also match "B*" as a wildcard. Literal wins — the narrower, safer choice
        // for a destructive op. (Only addressable with an explicit literal/wildcard split.)
        var source = new[] { "B*", "Banana", "Blueberry" };
        var result = source.FilterByNames(s => s, new[] { "B*" }).ToArray();
        Assert.Equal(new[] { "B*" }, result);
    }

    [Fact]
    public void MixedLiteralAndWildcard_NoDuplicates_SourceOrder()
    {
        // "A*" (no literal "A*") expands to Apple/Apricot; "Apple" literal also selects
        // Apple. Apple appears once and results stay in source order.
        var result = Fruits.FilterByNames(s => s, new[] { "A*", "Apple" }).ToArray();
        Assert.Equal(new[] { "Apple", "Apricot" }, result);
    }

    [Fact]
    public void NullSelectorEntries_FilteredOut()
    {
        var input = new[] { "Apple", null!, "Banana" };
        var result = input.FilterByNames(s => s, new[] { "*" }).ToArray();
        Assert.Equal(new[] { "Apple", "Banana" }, result);
    }
}

// The *Any (multi-selector) and Select* (empty => none) members of the FilterByNames family,
// used by dual-identity cmdlets (User: UserName OR EmailAddress) and action cmdlets respectively.
public class FilterByNamesFamilyTests
{
    private sealed record U(string? UserName, string? Email);

    private static readonly U[] Users =
    [
        new("alice", "alice@corp.com"),
        new("bob#ext#@t.onmicrosoft.com", "bob@corp.com"),
        new("carol [x]", "carol@corp.com"),
    ];

    private static readonly Func<U?, string?>[] UserSelectors =
        [u => u?.UserName, u => u?.Email];

    [Fact]
    public void FilterByNamesAny_MatchesViaEitherSelector()
    {
        // Matched by EmailAddress even though UserName is the mangled B2B form.
        var result = Users.FilterByNamesAny(UserSelectors, new[] { "bob@corp.com" }).ToArray();
        Assert.Single(result);
        Assert.Equal("bob#ext#@t.onmicrosoft.com", result[0].UserName);
    }

    [Fact]
    public void FilterByNamesAny_LiteralBracketUserName_MatchedLiterally()
    {
        var result = Users.FilterByNamesAny(UserSelectors, new[] { "carol [x]" }).ToArray();
        Assert.Single(result);
        Assert.Equal("carol [x]", result[0].UserName);
    }

    [Fact]
    public void FilterByNamesAny_NullNames_ReturnsAll()
    {
        Assert.Equal(Users, Users.FilterByNamesAny(UserSelectors, null).ToArray());
    }

    [Fact]
    public void SelectByNames_EmptyNames_ReturnsEmpty()
    {
        // Action-cmdlet semantics: no -Name => no-op (unlike FilterByNames which returns all).
        Assert.Empty(new[] { "Apple", "Banana" }.SelectByNames(s => s, Array.Empty<string>()));
    }

    [Fact]
    public void SelectByNames_LiteralBracketName_MatchedLiterally()
    {
        var source = new[] { "Sales E", "Sales [EU]" };
        Assert.Equal(new[] { "Sales [EU]" }, source.SelectByNames(s => s, new[] { "Sales [EU]" }).ToArray());
    }

    [Fact]
    public void SelectByNamesAny_NullNames_ReturnsEmpty()
    {
        Assert.Empty(Users.SelectByNamesAny(UserSelectors, null));
    }
}

public class SelectByWildcardsTests
{
    private static readonly string[] Fruits = ["Apple", "Apricot", "Banana", "Cherry"];

    [Fact]
    public void NullPatternList_ReturnsEmpty()
    {
        // Different from FilterByWildcards: SelectByWildcards returns empty when patterns are absent.
        List<WildcardPattern>? patterns = null;
        Assert.Empty(Fruits.SelectByWildcards(s => s, patterns));
    }

    [Fact]
    public void EmptyPatternList_ReturnsEmpty()
    {
        Assert.Empty(Fruits.SelectByWildcards(s => s, new List<WildcardPattern>()));
    }

    [Fact]
    public void NullStringArray_ReturnsEmpty()
    {
        string[]? patterns = null;
        Assert.Empty(Fruits.SelectByWildcards(s => s, patterns));
    }

    [Fact]
    public void EmptyStringArray_ReturnsEmpty()
    {
        Assert.Empty(Fruits.SelectByWildcards(s => s, Array.Empty<string>()));
    }

    [Fact]
    public void WithPattern_BehavesLikeFilter()
    {
        var result = Fruits.SelectByWildcards(s => s, new[] { "A*" }).ToArray();
        Assert.Equal(new[] { "Apple", "Apricot" }, result);
    }
}

public class ExcludeByWildcardsTests
{
    private static readonly string[] Fruits = ["Apple", "Apricot", "Banana", "Cherry"];

    [Fact]
    public void NullPatternList_ReturnsSourceUnchanged()
    {
        List<WildcardPattern>? patterns = null;
        var result = Fruits.ExcludeByWildcards(s => s, patterns).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void EmptyPatternList_ReturnsSourceUnchanged()
    {
        var result = Fruits.ExcludeByWildcards(s => s, new List<WildcardPattern>()).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void SinglePattern_RemovesMatchingItems()
    {
        var patterns = new[] { "A*" }.ConvertToWildcardPatternList();
        var result = Fruits.ExcludeByWildcards(s => s, patterns).ToArray();
        Assert.Equal(new[] { "Banana", "Cherry" }, result);
    }

    [Fact]
    public void MultiplePatterns_AnyMatchExcludes()
    {
        var patterns = new[] { "A*", "B*" }.ConvertToWildcardPatternList();
        var result = Fruits.ExcludeByWildcards(s => s, patterns).ToArray();
        Assert.Equal(new[] { "Cherry" }, result);
    }

    [Fact]
    public void PatternMatchingNothing_KeepsAll()
    {
        var patterns = new[] { "Z*" }.ConvertToWildcardPatternList();
        var result = Fruits.ExcludeByWildcards(s => s, patterns).ToArray();
        Assert.Equal(Fruits, result);
    }
}

public class ExcludeByClassValuesTests
{
    private static readonly string[] Fruits = ["Apple", "Banana", "Cherry"];

    [Fact]
    public void NullValues_ReturnsSourceUnchanged()
    {
        IEnumerable<string?>? values = null;
        var result = Fruits.ExcludeByClassValues<string, string>(s => s, values).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void EmptyValues_ReturnsSourceUnchanged()
    {
        var result = Fruits.ExcludeByClassValues<string, string>(s => s, Array.Empty<string?>()).ToArray();
        Assert.Equal(Fruits, result);
    }

    [Fact]
    public void SingleValue_RemovesIt()
    {
        var result = Fruits.ExcludeByClassValues<string, string>(s => s, new string?[] { "Banana" }).ToArray();
        Assert.Equal(new[] { "Apple", "Cherry" }, result);
    }

    [Fact]
    public void MultipleValues_RemoveAllMatching()
    {
        var result = Fruits.ExcludeByClassValues<string, string>(s => s, new string?[] { "Apple", "Cherry" }).ToArray();
        Assert.Equal(new[] { "Banana" }, result);
    }

    [Fact]
    public void ValueNotInSource_NoOp()
    {
        var result = Fruits.ExcludeByClassValues<string, string>(s => s, new string?[] { "Durian" }).ToArray();
        Assert.Equal(Fruits, result);
    }
}

public class ExcludeByStructValuesTests
{
    private static readonly int[] Numbers = [1, 2, 3, 4, 5];

    [Fact]
    public void NullValues_ReturnsSourceUnchanged()
    {
        IEnumerable<int>? values = null;
        var result = Numbers.ExcludeByStructValues(n => n, values).ToArray();
        Assert.Equal(Numbers, result);
    }

    [Fact]
    public void EmptyValues_ReturnsSourceUnchanged()
    {
        var result = Numbers.ExcludeByStructValues(n => n, Array.Empty<int>()).ToArray();
        Assert.Equal(Numbers, result);
    }

    [Fact]
    public void SingleValue_RemovesIt()
    {
        var result = Numbers.ExcludeByStructValues(n => n, new[] { 3 }).ToArray();
        Assert.Equal(new[] { 1, 2, 4, 5 }, result);
    }

    [Fact]
    public void MultipleValues_RemoveAllMatching()
    {
        var result = Numbers.ExcludeByStructValues(n => n, new[] { 2, 4 }).ToArray();
        Assert.Equal(new[] { 1, 3, 5 }, result);
    }

    [Fact]
    public void ValueNotInSource_NoOp()
    {
        var result = Numbers.ExcludeByStructValues(n => n, new[] { 99 }).ToArray();
        Assert.Equal(Numbers, result);
    }
}
