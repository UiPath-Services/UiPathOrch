using System;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the Get-PmUser -ExportCsv row shape so the export round-trips to New-PmUser.
// The property that matters after adding the UserName column (so a userName that
// differs from the email survives on Automation Suite / on-premises): every row has
// exactly one cell per CsvHeaders entry, and the userName lands in its own column
// instead of being smuggled into the Name column and lost on re-import.
public class PmUserCsvTests
{
    private static string[] Headers() =>
        (string[])typeof(GetPmUserCmdlet)
            .GetField("CsvHeaders", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    [Fact]
    public void BuildUserCsvRow_ColumnCount_MatchesHeaders()
    {
        var row = GetPmUserCmdlet.BuildUserCsvRow(
            "Orch1:", new PmUser { email = "a@x.com", userName = "alice" }, new[] { "G1" });
        Assert.Equal(Headers().Length, row.Length);
    }

    [Fact]
    public void BuildUserCsvRow_PreservesUserNameDistinctFromEmail()
    {
        // Columns: Path, Email, UserName, Name, SurName, DisplayName, Type, ...
        var row = GetPmUserCmdlet.BuildUserCsvRow(
            "Orch1:",
            new PmUser { email = "a@x.com", userName = "alice", name = "Alice", surname = "Smith", displayName = "Alice S" },
            Array.Empty<string>());
        Assert.Equal("a@x.com", row[1]);   // Email
        Assert.Equal("alice", row[2]);     // UserName -- its own column now
        Assert.Equal("Alice", row[3]);     // Name
        Assert.Equal("Smith", row[4]);     // SurName
        Assert.Equal("Alice S", row[5]);   // DisplayName
    }

    [Fact]
    public void BuildUserCsvRow_NoEmail_LeavesEmailBlank_KeepsUserName()
    {
        var row = GetPmUserCmdlet.BuildUserCsvRow(
            "Orch1:", new PmUser { email = null, userName = "localbob" }, Array.Empty<string>());
        Assert.Equal("", row[1]);          // Email blank
        Assert.Equal("localbob", row[2]);  // UserName preserved
    }

    [Fact]
    public void ExportHeaders_LineUpWith_NewPmUser_ImportColumns()
    {
        // The export headers must include the parameters New-PmUser binds by property
        // name, or the round-trip silently drops fields.
        var headers = Headers();
        foreach (var col in new[] { "Email", "UserName", "Name", "SurName", "DisplayName", "Type", "GroupName" })
            Assert.Contains(col, headers);
    }
}
