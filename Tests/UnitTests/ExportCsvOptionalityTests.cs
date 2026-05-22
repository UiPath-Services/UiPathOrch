using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Backward-compatibility guard for the cmdlets that GAINED -ExportCsv in
// v1.5.1 / v1.5.3. Adding a parameter must not change behaviour for
// callers who don't pass it: -ExportCsv and -CsvEncoding must be
// non-mandatory (the object-output path stays the default), and -ExportCsv
// must not be positional (so existing positional `Get-OrchX SomeName`
// invocations still bind SomeName to -Name, not -ExportCsv).
public class ExportCsvOptionalityTests
{
    public static System.Collections.Generic.IEnumerable<object[]> CmdletsWithExportCsv()
    {
        yield return new object[] { typeof(GetApiTriggerCmdlet) };
        yield return new object[] { typeof(GetTestDataQueueCmdlet) };
        yield return new object[] { typeof(GetActionCatalogCmdlet) };
        yield return new object[] { typeof(GetTestSetScheduleCmdlet) };
        yield return new object[] { typeof(GetWebhookCmdlet) };
        yield return new object[] { typeof(GetAssetLinkCmdlet) };
        yield return new object[] { typeof(GetBucketLinkCmdlet) };
        yield return new object[] { typeof(GetQueueLinkCmdlet) };
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void ExportCsv_IsOptionalAndNotPositional(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("ExportCsv");
        Assert.True(prop is not null, $"{cmdletType.Name} lost its -ExportCsv parameter.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a => Assert.False(a.Mandatory,
            $"{cmdletType.Name}.-ExportCsv must NOT be Mandatory — existing callers omit it."));
        Assert.All(attrs, a => Assert.True(a.Position == int.MinValue,
            $"{cmdletType.Name}.-ExportCsv must be a Named parameter (not positional) so " +
            "positional `Get-OrchX SomeName` still binds to -Name."));
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void CsvEncoding_IsOptional(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("CsvEncoding");
        Assert.True(prop is not null, $"{cmdletType.Name} lost its -CsvEncoding parameter.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a => Assert.False(a.Mandatory,
            $"{cmdletType.Name}.-CsvEncoding must NOT be Mandatory."));
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void OutputTypeUnchanged_StillDeclaresEntityOutput(System.Type cmdletType)
    {
        // The object-output path (no -ExportCsv) must still advertise the
        // entity OutputType so Get-Help / pipelines keep working.
        var outputAttr = cmdletType.GetCustomAttribute<OutputTypeAttribute>();
        Assert.True(outputAttr is not null,
            $"{cmdletType.Name} must still declare [OutputType] for the object-output path.");
        Assert.NotEmpty(outputAttr!.Type);
    }
}
