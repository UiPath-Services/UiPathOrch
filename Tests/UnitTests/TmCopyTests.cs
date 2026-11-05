using UiPath.PowerShell.Commands;
using Xunit;
using static UiPath.PowerShell.Commands.TmCopy;

namespace UnitTests;

// The rules Copy-Item / Copy-Tm* match items across Test Manager projects by (see TmCopy.cs and
// scratch/design-copy-tm.md, tables K and M).
public class TmCopyTests
{
    // Table M: how many items of one key the source and the destination have, and -Force.
    [Theory]
    // (The expected value is the name: Pairing is internal, and a public test method cannot take it.)
    [InlineData(1, 0, false, "Create")]   // M1
    [InlineData(1, 0, true, "Create")]    // M2
    [InlineData(1, 1, false, "Paired")]   // M3
    [InlineData(1, 1, true, "Paired")]    // M4: -Force still reuses a 1:1 pair
    [InlineData(1, 2, false, "Skip")]     // M5
    [InlineData(1, 2, true, "Create")]    // M6
    [InlineData(2, 0, false, "Create")]   // M7: the first copy of duplicates
    [InlineData(2, 0, true, "Create")]    // M8
    [InlineData(2, 1, false, "Skip")]     // M9
    [InlineData(2, 1, true, "Create")]    // M10
    [InlineData(2, 2, false, "Skip")]     // M11: equal counts are not paired by position
    [InlineData(2, 2, true, "Create")]    // M12
    public void Decide_Table(int srcCount, int dstCount, bool force, string expected)
        => Assert.Equal(expected, Decide(srcCount, dstCount, force).ToString());

    // Table K: which (name, foreign reference) pairs are the same key.
    [Theory]
    [InlineData("Login", null, "Login", null, true)]        // K1
    [InlineData("Login", null, "login", null, false)]       // K2: the name is exact, case included
    [InlineData("Login", "J-1", "Login", "J-1", true)]      // K3
    [InlineData("Login", "J-1", "Login", "j-1", false)]     // K4: the reference is exact
    [InlineData("Login", null, "Login", "", true)]          // K5: null and "" are no reference
    [InlineData("Login", "J-1", "Login", null, false)]      // K6
    [InlineData("Login", "J-1", "Logout", "J-1", false)]    // K7
    [InlineData(" Login ", " J-1 ", "Login", "J-1", true)]  // K8: trimmed, as the server stores them
    [InlineData("Login", "   ", "Login", null, true)]       // K8: whitespace only is no reference
    public void Key_Equality(string? name1, string? ref1, string? name2, string? ref2, bool same)
    {
        var a = TmCopyKey.Of(name1, ref1);
        var b = TmCopyKey.Of(name2, ref2);
        Assert.Equal(same, a == b);
        if (same) Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
