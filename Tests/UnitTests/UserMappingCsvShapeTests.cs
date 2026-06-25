using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins the public surface and the CSV contract of the tenant-migration
// user-mapping workflow (New-/Test-OrchUserMappingCsv plus the -UserMappingCsv
// switch on the copy cmdlets).
//
// Why this exists: the -UserMappingCsv path has had limited real-world
// validation (see docs/50-MigrationGuide.md "Maturity note"). These shape
// tests are a cheap regression floor — they don't exercise a live migration,
// but they lock the wiring and the column contract the migration guide and the
// LoadUserMappingCsv reader both depend on, so a rename/refactor can't silently
// break the documented round-trip.
public class UserMappingCsvShapeTests
{
    // Every copy cmdlet the migration guide tells the user to pass
    // -UserMappingCsv to must actually expose it. (Copy-Item is the navigation
    // provider's copy verb — it carries -UserMappingCsv as a dynamic parameter,
    // not a [Cmdlet] property, so it is covered by the guide, not by reflection
    // here.)
    public static System.Collections.Generic.IEnumerable<object[]> CopyCmdletsWithMapping()
    {
        yield return new object[] { typeof(CopyUserCmdlet) };
        yield return new object[] { typeof(CopyFolderUserCmdlet) };
        yield return new object[] { typeof(CopyAssetCmdlet) };
        yield return new object[] { typeof(CopyPmUserCmdlet) };
    }

    [Theory]
    [MemberData(nameof(CopyCmdletsWithMapping))]
    public void CopyCmdlet_ExposesUserMappingCsv_AsOptionalStringParameter(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("UserMappingCsv");
        Assert.NotNull(prop);
        // A single .csv path, not an array — one mapping file per migration run.
        Assert.Equal(typeof(string), prop!.PropertyType);

        var param = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(param);
        // It augments a copy that already works without it, so it must stay
        // optional: making it Mandatory would break the Case A simple migration.
        Assert.True(param.All(a => !a.Mandatory),
            $"{cmdletType.Name}.UserMappingCsv must be optional (Case A copies without it).");
    }

    // The exact CSV header set, in order. New-OrchUserMappingCsv emits it,
    // LoadUserMappingCsv reads SourceUserName/DestinationUserName back, and
    // docs/50-MigrationGuide.md (B-1) lists these columns verbatim. If any of
    // the three drifts, the documented workflow silently breaks — so the column
    // set is pinned here as the single source of truth.
    private static readonly string[] ExpectedHeaders =
    {
        "SourceUserName", "SourceEmail", "SourceDisplayName", "SourceSource",
        "DestinationUserName", "Name", "SurName", "DisplayName"
    };

    [Fact]
    public void NewUserMappingCsv_HeaderColumns_MatchTheDocumentedContract()
    {
        var headers = GetCsvHeaders(typeof(NewUserMappingCsvCmdlet));
        Assert.Equal(ExpectedHeaders, headers);
    }

    [Fact]
    public void NewUserMappingCsv_HasBothJoinKeyColumns()
    {
        // The mapping is keyed source->destination by these two columns; the
        // reader (LoadUserMappingCsv) and the human who fills blanks both rely
        // on them existing under exactly these names.
        var headers = GetCsvHeaders(typeof(NewUserMappingCsvCmdlet));
        Assert.Contains("SourceUserName", headers);
        Assert.Contains("DestinationUserName", headers);
    }

    [Theory]
    [InlineData("SourceTenant")]
    [InlineData("DestinationTenant")]
    [InlineData("ExportCsv")]
    public void NewUserMappingCsv_CoreParameters_AreMandatory(string paramName)
    {
        // Generating a mapping needs both endpoints and an output path; none has
        // a sensible default, so all three are Mandatory.
        var prop = typeof(NewUserMappingCsvCmdlet).GetProperty(paramName);
        Assert.NotNull(prop);
        var param = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(param);
        Assert.True(param.Any(a => a.Mandatory), $"NewUserMappingCsv.{paramName} must be Mandatory.");
    }

    [Fact]
    public void TestUserMappingCsv_IsADiagnosticTestCmdlet()
    {
        // Test-OrchUserMappingCsv validates an edited CSV before it is trusted
        // in a copy run; it must stay the Test verb so the guide's B-3 step
        // ("validate before migrating") resolves.
        var attr = typeof(TestUserMappingCsvCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Test", attr!.VerbName);
        Assert.Equal("OrchUserMappingCsv", attr.NounName);
    }

    // Reflect the private static CsvHeaders array, same approach as
    // ExportCsvHeaderParityTests.
    private static string[] GetCsvHeaders(System.Type cmdletType)
    {
        var field = cmdletType.GetField("CsvHeaders", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var value = (string[]?)field!.GetValue(null);
        Assert.NotNull(value);
        return value!;
    }
}
