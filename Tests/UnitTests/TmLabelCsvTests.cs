using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// The Labels column of the Tm* CSV files: names joined by commas, a comma or backtick in a name
// escaped with a backtick, and -Labels splitting every element back on unescaped commas.
public class TmLabelCsvTests
{
    [Fact]
    public void Format_SortsAndEscapes()
        => Assert.Equal("alpha,be`,ta,x``y", TmLabelCsv.Format(["x`y", "be,ta", "alpha"]));

    [Fact]
    public void Format_NullOrEmpty_EmptyCell()
    {
        Assert.Equal("", TmLabelCsv.Format(null));
        Assert.Equal("", TmLabelCsv.Format([]));
    }

    [Fact]
    public void Parse_CsvCell_SplitsOnUnescapedCommas()
        => Assert.Equal(["alpha", "be,ta", "x`y"], TmLabelCsv.Parse(["alpha,be`,ta,x``y"])!);

    [Fact]
    public void Parse_NativeArray_KeepsEachElement()
        => Assert.Equal(["a", "b"], TmLabelCsv.Parse(["a", "b"])!);

    [Fact]
    public void Parse_EmptyCell_NoLabels()
        => Assert.Empty(TmLabelCsv.Parse([""])!);

    [Fact]
    public void Parse_Null_Null()
        => Assert.Null(TmLabelCsv.Parse(null));

    [Fact]
    public void RoundTrip()
    {
        string[] labels = ["a,b", "c`d", "e f", "日本語"];
        Assert.Equal(labels.Order(StringComparer.Ordinal), TmLabelCsv.Parse([TmLabelCsv.Format(labels)]));
    }
}
