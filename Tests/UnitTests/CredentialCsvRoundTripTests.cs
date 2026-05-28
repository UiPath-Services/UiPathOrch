using UiPath.PowerShell.Commands;
using Xunit;
using CsvLine = UiPath.PowerShell.Commands.CsvHelper.CsvLine;

namespace UnitTests;

// Regression: Get-OrchAsset -ExportCredentialCsv must escape every field. An
// unescaped comma/quote in Description, CredentialUsername or ExternalName shifts
// all later columns when the CSV is read back (the documented round trip is
// `Import-Csv x.csv | Set-OrchCredentialAsset`), corrupting CredentialStore /
// UserName and leaking the username into the CredentialPassword column.
//
// These build the row exactly as the exporter does (BuildCredentialCsvRow, which
// WriteCredentialCsvContent now delegates to) and round-trip it through the
// project's RFC-4180 splitter, asserting each field survives byte-for-byte.
public class CredentialCsvRoundTripTests
{
    // Indices in CsvCredentialHeaders order.
    private const int Description = 2, CredentialStore = 3, UserName = 4,
        CredentialUsername = 6, CredentialPassword = 7, ExternalName = 8;

    private static List<string> RoundTrip(string?[] row)
        => CsvLine.Split(string.Join(",", row));

    [Fact]
    public void Description_WithComma_RoundTripsIntact_NoColumnShift()
    {
        var f = RoundTrip(GetAssetCmdlet.BuildCredentialCsvRow(
            path: @"Orch1:\Shared", name: "SapCred", description: "Prod, EMEA",
            credentialStore: "AzureKV", userName: null, machineName: null,
            credentialUsername: "svc_sap", externalName: null));

        Assert.Equal(9, f.Count);
        Assert.Equal("Prod, EMEA", f[Description]);
        Assert.Equal("AzureKV", f[CredentialStore]);
        Assert.Equal("svc_sap", f[CredentialUsername]);
        Assert.Equal("", f[CredentialPassword]); // username must NOT leak into password
    }

    [Fact]
    public void Description_StartingWithQuoteAndComma_RoundTripsIntact()
    {
        var f = RoundTrip(GetAssetCmdlet.BuildCredentialCsvRow(
            @"Orch1:\Shared", "C", description: "\"VIP\", primary", credentialStore: "KV",
            userName: null, machineName: null, credentialUsername: "u", externalName: null));

        Assert.Equal(9, f.Count);
        Assert.Equal("\"VIP\", primary", f[Description]);
        Assert.Equal("KV", f[CredentialStore]);
    }

    [Fact]
    public void CredentialUsername_WithComma_RoundTripsIntact()
    {
        var f = RoundTrip(GetAssetCmdlet.BuildCredentialCsvRow(
            @"Orch1:\Shared", "C", description: "d", credentialStore: "KV",
            userName: "dom\\user", machineName: "M", credentialUsername: "a,b", externalName: "ext"));

        Assert.Equal(9, f.Count);
        Assert.Equal("a,b", f[CredentialUsername]);
        Assert.Equal("ext", f[ExternalName]);
        Assert.Equal("dom\\user", f[UserName]);
    }

    [Fact]
    public void ExternalName_WithComma_RoundTripsIntact()
    {
        var f = RoundTrip(GetAssetCmdlet.BuildCredentialCsvRow(
            @"Orch1:\Shared", "C", description: "d", credentialStore: "KV",
            userName: null, machineName: null, credentialUsername: "u", externalName: "ext,name"));

        Assert.Equal(9, f.Count);
        Assert.Equal("ext,name", f[ExternalName]);
        Assert.Equal("KV", f[CredentialStore]);
    }

    [Fact]
    public void PlainValues_RoundTripUnchanged_AndProduceNineColumns()
    {
        var f = RoundTrip(GetAssetCmdlet.BuildCredentialCsvRow(
            @"Orch1:\Shared", "Plain", description: "simple", credentialStore: "KV",
            userName: null, machineName: null, credentialUsername: "user1", externalName: "ext1"));

        Assert.Equal(9, f.Count);
        Assert.Equal("simple", f[Description]);
        Assert.Equal("user1", f[CredentialUsername]);
        Assert.Equal("ext1", f[ExternalName]);
    }
}
