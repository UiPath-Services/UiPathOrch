using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Add-/Remove-TmTestSetTestCase and -TmRequirementTestCase resolve names: a name with no wildcard
// must match exactly one item, since Test Manager allows duplicate names; a wildcard selects all.
public class TmTestCaseAssignmentTests
{
    private static readonly string[] Names = ["Login", "Logout", "Dup", "dup", "Login [v2]"];

    private static List<string> Resolve(string pattern, out string? error)
        => TmTestCaseAssignment.Resolve(Names, n => n, pattern, "test case", out error);

    [Fact]
    public void Name_MatchesExactlyOne_IgnoringCase()
    {
        Assert.Equal(["Login"], Resolve("login", out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Name_NoMatch_Error()
    {
        Assert.Empty(Resolve("Nope", out var error));
        Assert.Equal("There is no test case named 'Nope'.", error);
    }

    [Fact]
    public void Name_TwoMatches_ErrorAndNothingSelected()
    {
        Assert.Empty(Resolve("Dup", out var error));
        Assert.StartsWith("There are 2 test cases named 'Dup'", error);
    }

    [Fact]
    public void EscapedName_IsLiteral()
    {
        Assert.Equal(["Login [v2]"], Resolve("Login `[v2`]", out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Wildcard_SelectsEveryMatch_NoErrorWhenNone()
    {
        Assert.Equal(["Login", "Logout", "Login [v2]"], Resolve("Log*", out var error));
        Assert.Null(error);
        Assert.Empty(Resolve("Z*", out error));
        Assert.Null(error);
    }

    [Fact]
    public void DuplicateNames_IgnoringCase()
        => Assert.Equal(["Dup"], TmTestCaseAssignment.DuplicateNames(Names, n => n), StringComparer.OrdinalIgnoreCase);
}
